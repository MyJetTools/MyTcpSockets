using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;

namespace MyTcpSockets
{
    public abstract class TcpContext<TSocketData>
    {
        public TcpClient TcpClient { get; private set; }
        protected Stream SocketStream { get; private set; }
        protected ITcpSerializer<TSocketData> TcpSerializer { get; private set; }
        protected abstract ValueTask OnConnectAsync();
        protected abstract ValueTask OnDisconnectAsync();
        protected abstract ValueTask HandleIncomingDataAsync(TSocketData data);

        internal async Task ReadLoopAsync()
        {

            try
            {
                var trafficReader = new TcpDataReader();


                var trafficWriterTask = Task.Run(async () =>
                {
                    var socketBuffer = new byte[TcpSerializer.BufferSize];
            
                    var readSize =
                        await SocketStream.ReadAsync(socketBuffer, 0, socketBuffer.Length);
                    
                    while (readSize > 0)
                    {
                        SocketStatistic.WeHaveReceiveEvent(readSize);

                        trafficReader.NewPackage(socketBuffer, readSize);

                        socketBuffer = new byte[TcpSerializer.BufferSize];

                        readSize =
                            await SocketStream.ReadAsync(socketBuffer, 0, socketBuffer.Length);
                    }

                    if (readSize == 0)
                    {
                        await trafficReader.StopAsync();
                        throw new Exception("Disconnected");
                    }

                });
                
                
                while (TcpClient.Connected)
                {
                    await foreach (var incomingDataPacket in TcpSerializer.DeserializeAsync(trafficReader))
                    {
                        await HandleIncomingDataAsync(incomingDataPacket);
                    }
                }



                await trafficWriterTask;
            }
            finally
            {
                await DisconnectAsync();
            }

        }

        public long MaxSendPackageSize { get;  } = 1024 * 1024;

        private async Task<ReadOnlyMemory<byte>> GetDataToSendAsync()
        {

            var socketData = await _queueToSend.ConsumeAsync();
            var packageToSend = TcpSerializer.Serialize(socketData);

            if (_queueToSend.Count <= 0)
                return packageToSend;

            var result = new MemoryStream();
            result.Write(packageToSend.Span);

            while (result.Length<MaxSendPackageSize)
            {

                socketData = await _queueToSend.ConsumeAsync();
                packageToSend = TcpSerializer.Serialize(socketData);
                result.Write(packageToSend.Span);
                
                if (_queueToSend.Count <= 0)
                    break;

            }

            return result.ToArray();
        }

        internal async Task WriteLoopAsync()
        {

            try
            {
                while (Connected)
                {
                    var dataToSend = await GetDataToSendAsync();
                    SocketStatistic.WeHaveSendEvent(dataToSend.Length);
                    await SocketStream.WriteAsync(dataToSend);
                }
            }
            finally
            {
                WriteLog("Exiting WriteLoop for socket: " + Id + " with name: " + ContextName +
                         ". Socket is connected: " + Connected);
                await DisconnectAsync();
            }
            
        }
        
        
        private readonly ProducerConsumer<TSocketData> _queueToSend = new ProducerConsumer<TSocketData>();

        public void SendPacket(TSocketData data)
        {
            _queueToSend.Produce(data);
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


        public ValueTask DisconnectAsync()
        {
            lock (this)
            {
                if (!Connected)
                    return new ValueTask();

                Connected = false;
            }

            try
            {
                _queueToSend.Stop();
            }
            catch (Exception e)
            {
                WriteLog("_queueToSend.Stop(): "+e);
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

            return ProcessOnDisconnectAsync();

        }

        public long Id { get; internal set; }

        public string ContextName { get; private set; }

        protected void SetContextName(string contextName)
        {
            ContextName = contextName;
            WriteLog($"Changed context name to: {contextName} for socket id: {Id} with ip: {TcpClient.Client.RemoteEndPoint}");
        }

        public SocketStatistic SocketStatistic { get; private set; }

        public bool Connected { get; private set; }


        private Action<object> _log;

        protected void WriteLog(object data)
        {
            _log?.Invoke(data);
        }

        internal ValueTask StartAsync(TcpClient tcpClient, ITcpSerializer<TSocketData> tcpSerializer, Action<object> log)
        {

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