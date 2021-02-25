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
                    _log.InvokeExceptionLog(this, e);
                }
            }
            
            lock (_lockObject)
            {
                
                if (!Connected)
                    return;

                Connected = false;

            }
            _log.InvokeInfoLog(this, $"Socket {ContextName} is Disconnected with Ip:{TcpClient.Client.RemoteEndPoint}. Id=" + Id);

            try
            {
                _cancellationToken.Cancel(true);
            }
            catch (Exception e)
            {
                _log.InvokeExceptionLog(this, e);
            }
            
            try
            {

                SocketStatistic.WeHaveDisconnect();
            }
            catch (Exception e)
            {
                _log.InvokeExceptionLog(this, e);
            }
            
            try
            {
                SocketStream.Close();
            }
            catch (Exception e)
            {
                _log.InvokeExceptionLog(this, e);
            }     
            
            try
            {
                TcpClient.Close();
            }
            catch (Exception e)
            {
                _log.InvokeExceptionLog(this, e);
            }
            
            try
            {
                _deliveryPublisherSubscriber.Stop();
            }
            catch (Exception e)
            {
                _log.InvokeExceptionLog(this, e);
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
                _log.InvokeExceptionLog(this, e);
            }
        }
        #endregion


        #region Read
        private async Task PublishDataToTrafficReaderAsync(TcpDataReader trafficReader)
        {

            try
            {
#if NETSTANDARD2_1
                var buffer = await trafficReader.AllocateBufferToWriteAsync(_cancellationToken.Token);

                var readSize =
                    await SocketStream.ReadAsync(buffer, _cancellationToken.Token);
                

                while (readSize > 0)
                {
                    SocketStatistic.WeHaveReceiveEvent(readSize);

                    trafficReader.CommitWrittenData(readSize);

                    buffer = await trafficReader.AllocateBufferToWriteAsync(_cancellationToken.Token);

                    readSize =
                        await SocketStream.ReadAsync(buffer, _cancellationToken.Token);
                }
#else


                var buffer = await trafficReader.AllocateBufferToWriteLegacyAsync(_cancellationToken.Token);
                
                var readSize =
                    await SocketStream.ReadAsync(buffer.buffer, buffer.start, buffer.len, _cancellationToken.Token);

                while (readSize > 0)
                {
                    SocketStatistic.WeHaveReceiveEvent(readSize);

                    trafficReader.CommitWrittenData(readSize);

                    buffer = await trafficReader.AllocateBufferToWriteLegacyAsync(_cancellationToken.Token);

                    readSize =
                        await SocketStream.ReadAsync(buffer.buffer, buffer.start, buffer.len, _cancellationToken.Token);
                }           
#endif
            }
            catch (Exception e)
            {
                _log.InvokeExceptionLog(this, e);
            }
            finally
            {
                _log.InvokeInfoLog(this, "Disconnected from Traffic Reader Loop");
                trafficReader.Stop();
            }

        }

        private async Task ReadLoopAsync(int bufferSize)
        {
            try
            {
                var trafficReader = new TcpDataReader(bufferSize);

                var trafficWriterTask = PublishDataToTrafficReaderAsync(trafficReader);


                while (TcpClient.Connected)
                {
                    var incomingDataPacket =
                        await TcpSerializer.DeserializeAsync(trafficReader, _cancellationToken.Token);
                    await HandleIncomingDataAsync(incomingDataPacket);
                }

                await trafficWriterTask;
            }
            catch (Exception e)
            {
                _log.InvokeExceptionLog(this, e);
            }
            finally
            {
                _log.InvokeInfoLog(this, "Disconnected from ReadLoopAsync");
                Disconnect();
            }
        }

        private Task _readLoopTask;
        private Task _sendTrafficTask;
        internal void StartReadThread(int bufferSize)
        {
            _readLoopTask = ReadLoopAsync(bufferSize);
            _sendTrafficTask = StartSendDeliveryTaskAsync();
        }
        #endregion

        
        #region Write

        private byte[] _deliveryBuffer;

        private TcpSocketPublisherSubscriber _deliveryPublisherSubscriber;
        
        private async Task StartSendDeliveryTaskAsync()
        {

            while (true)
            {
                var dataToSend = await _deliveryPublisherSubscriber.DequeueAsync(_deliveryBuffer);

                if (dataToSend.Length == 0)
                {
                    Console.WriteLine("Leaving Send Delivery Task async");
                    return;
                }
                
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
                    _log.InvokeExceptionLog(this, e);
                    Disconnect();
                    break;
                }

            }

       
        }

        public void SendDataToSocket(TSocketData data)
        {
            if (!Connected)
                return;

            var dataToSend = TcpSerializer.Serialize(data);
            
            _deliveryPublisherSubscriber.Publish(dataToSend);
        }
        
        #endregion


        public string ContextName { get; private set; }

        protected void SetContextName(string contextName)
        {
            ContextName = contextName;
            Inited = true;
            _log.InvokeInfoLog(this, $"Changed context name to: {contextName} for socket id: {Id} with ip: {TcpClient?.Client.RemoteEndPoint}");
        }

        public SocketStatistic SocketStatistic { get; private set; }

        public bool Connected { get; private set; }
        public bool Inited { get; private set; }

        private ISocketLogInvoker _log;

        internal ValueTask StartAsync(TcpClient tcpClient, ITcpSerializer<TSocketData> tcpSerializer, object lockObject, ISocketLogInvoker log, 
            Action<ITcpContext> disconnectedCallback, byte[] deliveryBuffer)
        {
            _disconnectedCallback = disconnectedCallback;
            _lockObject = lockObject;
            _deliveryBuffer = deliveryBuffer;
            _deliveryPublisherSubscriber = new TcpSocketPublisherSubscriber(_lockObject);
            TcpClient = tcpClient;
            SocketStream = TcpClient.GetStream();
            TcpSerializer = tcpSerializer;
            SetContextName(TcpClient.Client.RemoteEndPoint.ToString());
            _log = log;
            Connected = true;
            SocketStatistic = new SocketStatistic();
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