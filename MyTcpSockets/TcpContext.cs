using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;

namespace MyTcpSockets
{

    public interface ITcpContext
    {
        Stream SocketStream { get; }
        
        Queue<ReadOnlyMemory<byte>> DataToSend { get; }

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

        Queue<ReadOnlyMemory<byte>> ITcpContext.DataToSend { get; } = new Queue<ReadOnlyMemory<byte>>();

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


        private async Task PublishDataToTrafficReaderAsync(TcpDataReader trafficReader)
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
        }
        
        
        

        internal async Task ReadLoopAsync()
        {

            try
            {
                var trafficReader = new TcpDataReader();

                var trafficWriterTask = PublishDataToTrafficReaderAsync(trafficReader);
                
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
        
        public void SendPacket(TSocketData data)
        {
            if (!Connected)
                return;
            
            var dataToSend = TcpSerializer.Serialize(data);
            _outDataSender.EnqueueSendData(this, dataToSend);
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
            _log?.Invoke(this, data);
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