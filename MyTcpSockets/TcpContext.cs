using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;
using MyTcpSockets.DataSender;

namespace MyTcpSockets
{

    public interface ITcpContext
    {
        Stream SocketStream { get; }
        
        SendDataQueue DataToSend { get; }

        ValueTask DisconnectAsync();
        
        long Id { get; }
        
        bool Connected { get; } 
    }
    
    
    public abstract class TcpContext<TSocketData> : ITcpContext
    {

        private OutDataSender _outDataSender;

        private object _lockObject;
        
        public TcpClient TcpClient { get; protected set; }
        public Stream SocketStream { get; private set; }
        
        public long Id { get; internal set; }

        public SendDataQueue DataToSend { get; } = new SendDataQueue();

        public ValueTask DisconnectAsync()
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
                    return new ValueTask();

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
                DataToSend.Clear();
                SocketStatistic.WeHaveDisconnect();
            }
            catch (Exception e)
            {
                WriteLog("SocketStatistic Disconnect: "+e);
            }
            
            var result = ProcessOnDisconnectAsync();
            
            try
            {
                SocketStream.Close();
            }
            catch (Exception e)
            {
                WriteLog("SocketStream.Close(). "+e);
            }     
            
            try
            {
                TcpClient.Close();
            }
            catch (Exception e)
            {
                WriteLog("TcpClient.Close(). "+e);
            }

            return result;

        }
        
        protected ITcpSerializer<TSocketData> TcpSerializer { get; private set; }
        protected abstract ValueTask OnConnectAsync();
        protected abstract ValueTask OnDisconnectAsync();
        protected abstract ValueTask HandleIncomingDataAsync(TSocketData data);
        
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();


        private async Task PublishDataToTrafficReaderAsync(TcpDataReader trafficReader)
        {

            try
            {
                var socketBuffer = new byte[TcpSerializer.BufferSize];

                var readSize =
                    await SocketStream.ReadAsync(socketBuffer, 0, socketBuffer.Length, _cancellationToken.Token);

                while (readSize > 0)
                {
                    SocketStatistic.WeHaveReceiveEvent(readSize);

                    trafficReader.NewPackage(socketBuffer, readSize);

                    socketBuffer = new byte[TcpSerializer.BufferSize];

                    readSize =
                        await SocketStream.ReadAsync(socketBuffer, 0, socketBuffer.Length, _cancellationToken.Token);
                }
            }
            catch (Exception e)
            {
                WriteLog(e);
            }
            finally
            {
                WriteLog("Disconnected from Traffic Reader Loop");
                await trafficReader.StopAsync();
            }

        }
        
        
        

        internal async Task ReadLoopAsync()
        {
            try
            {
                var trafficReader = new TcpDataReader();

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
                await DisconnectAsync();
            }
        }



        internal Task ReadLoopTask;
        internal void StartReadThread()
        {
            ReadLoopTask = ReadLoopAsync();
        }
        
        public void SendPacket(TSocketData data)
        {
            if (!Connected)
                return;
            
            var dataToSend = TcpSerializer.Serialize(data);
            DataToSend.Enqueue(dataToSend);
            _outDataSender.PushData(this);
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

        public string ContextName { get; private set; }

        protected void SetContextName(string contextName)
        {
            ContextName = contextName;
            WriteLog($"Changed context name to: {contextName} for socket id: {Id} with ip: {TcpClient.Client.RemoteEndPoint}");
        }

        public SocketStatistic SocketStatistic { get; private set; }

        public bool Connected { get; private set; }


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

        private Action<ITcpContext> _disconnectedCallback;

        internal ValueTask StartAsync(TcpClient tcpClient, ITcpSerializer<TSocketData> tcpSerializer, OutDataSender outDataSender, object lockObject, Action<ITcpContext, object> log, Action<ITcpContext> disconnectedCallback)
        {
            _disconnectedCallback = disconnectedCallback;
            _lockObject = lockObject;
            _outDataSender = outDataSender;
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