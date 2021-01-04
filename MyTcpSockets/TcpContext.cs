using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;

namespace MyTcpSockets
{

   public interface ITcpContext
    {
        SocketStatistic SocketStatistic { get; }
        
        long Id { get; }
        
        
        bool Inited { get; }

    }
    
    
    public abstract class TcpContext<TSocketData> : ITcpContext
    {

        private object _lockObject;
        
        public TcpClient TcpClient { get; protected set; }
        public Stream SocketStream { get; private set; }
        
        public long Id { get; internal set; }
        
        protected ITcpSerializer<TSocketData> TcpSerializer { get; private set; }
        protected abstract ValueTask OnConnectAsync();
        protected abstract ValueTask OnDisconnectAsync();
        protected abstract ValueTask HandleIncomingDataAsync(TSocketData data);
        
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        #region Disconnect
        
        private Action<ITcpContext> _disconnectedCallback;
        
        public void Disconnect()
        {
            if (_disconnectedCallback != null)
            {
                try
                {
                    _disconnectedCallback(this);
                }
                catch (Exception e)
                {
                    WriteLog("SocketStream._disconnectedCallback() "+e);
                }
            }
            
            lock (_lockObject)
            {
                
                if (!Connected)
                    return;

                Connected = false;

            }
            WriteLog($"Socket {ContextName} is Disconnected with Ip:{TcpClient.Client.RemoteEndPoint}. Id=" + Id);

            try
            {
                _cancellationToken.Cancel(true);
            }
            catch (Exception e)
            {
                WriteLog(" _cancellationToken.Cancel(): "+e);
            }
            
            try
            {

                SocketStatistic.WeHaveDisconnect();
            }
            catch (Exception e)
            {
                WriteLog("SocketStatistic Disconnect: "+e);
            }
            
            try
            {
                SocketStream.Close();
            }
            catch (Exception e)
            {
                WriteLog("SocketStream.Close(): "+e);
            }     
            
            try
            {
                TcpClient.Close();
            }
            catch (Exception e)
            {
                WriteLog("TcpClient.Close(): "+e);
            }
            
            try
            {
                _asyncLock.Dispose();
            }
            catch (Exception e)
            {
                WriteLog("_asyncLock.Dispose(): "+e);
            }
            
            
            Task.Run(ProcessOnDisconnectAsync);
        }
        
        private async ValueTask ProcessOnDisconnectAsync()
        {
            try
            {
                await OnDisconnectAsync();
            }
            catch (Exception e)
            {
                WriteLog(e);
            }
        }
        #endregion


        #region Read
        private async Task PublishDataToTrafficReaderAsync(TcpDataReader trafficReader)
        {


            try
            {
#if NETSTANDARD2_1
                var buffer = trafficReader.AllocateBufferToWrite();

                var readSize =
                    await SocketStream.ReadAsync(buffer, _cancellationToken.Token);

                while (readSize > 0)
                {
                    SocketStatistic.WeHaveReceiveEvent(readSize);

                    trafficReader.CommitWrittenData(readSize);

                    buffer = trafficReader.AllocateBufferToWrite();

                    readSize =
                        await SocketStream.ReadAsync(buffer, _cancellationToken.Token);
                }
#else


                var buffer = trafficReader.AllocateBufferToWriteLegacy();
                
                var readSize =
                    await SocketStream.ReadAsync(buffer.buffer, buffer.start, buffer.len, _cancellationToken.Token);

                while (readSize > 0)
                {
                    SocketStatistic.WeHaveReceiveEvent(readSize);

                    trafficReader.CommitWrittenData(readSize);

                    buffer = trafficReader.AllocateBufferToWriteLegacy();

                    readSize =
                        await SocketStream.ReadAsync(buffer.buffer, buffer.start, buffer.len, _cancellationToken.Token);
                }           
#endif
            }
            catch (Exception e)
            {
                WriteLog(e);
            }
            finally
            {
                WriteLog("Disconnected from Traffic Reader Loop");
                trafficReader.Stop();
            }

        }

        private async Task ReadLoopAsync(int bufferSize, int minAllocationSize)
        {
            try
            {
                var trafficReader = new TcpDataReader(bufferSize, minAllocationSize);

                var trafficWriterTask = PublishDataToTrafficReaderAsync(trafficReader);
                
                while (TcpClient.Connected)
                {
#if NETSTANDARD2_1
                    await foreach (var incomingDataPacket in TcpSerializer.DeserializeAsync(trafficReader, _cancellationToken.Token))
                    {
                        await HandleIncomingDataAsync(incomingDataPacket);
                    }
#else
                    var incomingDataPacket = await TcpSerializer.DeserializeAsync(trafficReader, _cancellationToken.Token);
                    await HandleIncomingDataAsync(incomingDataPacket);
#endif
      
                }
                
                await trafficWriterTask;
            }
            finally
            {
                WriteLog("Disconnected from ReadLoopAsync");
                Disconnect();
            }
        }

        protected Task ReadLoopTask { get; private set; }
        internal void StartReadThread(int bufferSize, int minAllocationSize)
        {
            ReadLoopTask = ReadLoopAsync(bufferSize, minAllocationSize);
        }
        #endregion

        
        #region Write
        private readonly SendDataQueue _sendDataQueue;

        private byte[] _deliveryBuffer;
        
        private readonly AsyncLock _asyncLock;

        private async ValueTask DeliverPacketAsync()
        {
            await _asyncLock.LockAsync();
            try
            {
                var dataToSend = _sendDataQueue.Dequeue(_deliveryBuffer);
            
                while (dataToSend.Length >0)
                {
                    try
                    {
                        var dt = DateTime.UtcNow;
                        await SocketStream.WriteAsync(dataToSend);
                        SocketStatistic.WeHaveSendEvent(dataToSend.Length);
                        SocketStatistic.LastSendToSocketDuration = DateTime.UtcNow - dt;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        _log?.Invoke(this, e);

                        Disconnect();
                        break;
                    }
                    dataToSend = _sendDataQueue.Dequeue(_deliveryBuffer);
                }
            }
            finally
            {
                _asyncLock.Unlock();
            }
       
        }

        public void SendDataToSocket(TSocketData data)
        {
            if (!Connected)
                return;

            var dataToSend = TcpSerializer.Serialize(data);
            
            _sendDataQueue.Enqueue(dataToSend);
            Task.Run(DeliverPacketAsync);
        }
        
        #endregion


        public string ContextName { get; private set; }

        protected void SetContextName(string contextName)
        {
            ContextName = contextName;
            Inited = true;
            WriteLog($"Changed context name to: {contextName} for socket id: {Id} with ip: {TcpClient.Client.RemoteEndPoint}");
        }

        public SocketStatistic SocketStatistic { get; private set; }

        public bool Connected { get; private set; }
        public bool Inited { get; private set; }


        private Action<ITcpContext, object> _log;

        protected void WriteLog(object data)
        {
            try
            {
                _log?.Invoke(this, data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        
        }


        public TcpContext()
        {
            _asyncLock = new AsyncLock(_lockObject);
            _sendDataQueue = new SendDataQueue(_lockObject);
        }

        internal ValueTask StartAsync(TcpClient tcpClient, ITcpSerializer<TSocketData> tcpSerializer, object lockObject, Action<ITcpContext, object> log, 
            Action<ITcpContext> disconnectedCallback, byte[] deliveryBuffer)
        {
            _disconnectedCallback = disconnectedCallback;
            _lockObject = lockObject;
            TcpClient = tcpClient;
            SocketStream = TcpClient.GetStream();
            TcpSerializer = tcpSerializer;
            SetContextName(TcpClient.Client.RemoteEndPoint.ToString());
            _log = log;
            Connected = true;
            SocketStatistic = new SocketStatistic();

            _deliveryBuffer = deliveryBuffer;

            
            return OnConnectAsync();
        }
    }

    public abstract class ClientTcpContext<TSocketData> : TcpContext<TSocketData>
    {
        protected abstract TSocketData GetPingPacket();

        public async ValueTask SendPingAsync()
        {
            var pingPacket = GetPingPacket();
            var packageToSend = TcpSerializer.Serialize(pingPacket);
            await SocketStream.WriteAsync(packageToSend);
        }
    }
}