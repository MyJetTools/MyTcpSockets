using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MyTcpSockets
{

    public class MyClientTcpSocket<TSocketData>
    {

        private readonly Func<string> _getHostPort;
        private readonly TimeSpan _reconnectTimeOut;

        private Func<ClientTcpContext<TSocketData>> _socketContextFactory;

        private Func<ITcpSerializer<TSocketData>> _socketSerializerFactory;

        private readonly object _lockObject = new object();


        private readonly ISocketLogInvoker _log;
        public readonly SocketLog<MyClientTcpSocket<TSocketData>> Logs;

        public TimeSpan PingInterval { get; private set; }

        private TimeSpan _disconnectInterval;

        private readonly byte[] _deliveryBuffer;
        
        private int _readBufferSize = 1024 * 1024 * 2;

        
        public MyClientTcpSocket<TSocketData> SetReadBufferSize(int readBufferSize)
        {
            _readBufferSize = readBufferSize;
            return this;
        }


        public MyClientTcpSocket(Func<string> getHostPort, TimeSpan reconnectTimeOut, int sendBufferSize = 1024 * 1024)
        {
            Logs = new SocketLog<MyClientTcpSocket<TSocketData>>(this);
            _log = Logs;
            _getHostPort = getHostPort;
            _reconnectTimeOut = reconnectTimeOut;
            SetPingInterval(TimeSpan.FromSeconds(3));
            _deliveryBuffer = new byte[sendBufferSize];
        }

        public MyClientTcpSocket<TSocketData> RegisterTcpContextFactory(
            Func<ClientTcpContext<TSocketData>> socketContextFactory)
        {
            _socketContextFactory = socketContextFactory;
            
            return this;
        }

        public MyClientTcpSocket<TSocketData> SetPingInterval(TimeSpan pingInterval)
        {
            PingInterval = pingInterval;
            _disconnectInterval = pingInterval + pingInterval + pingInterval;
            return this;
        }

        public MyClientTcpSocket<TSocketData> RegisterTcpSerializerFactory(
            Func<ITcpSerializer<TSocketData>> socketSerializerFactory)
        {
            _socketSerializerFactory = socketSerializerFactory;
            return this;
        }


        private bool _working;

        private async Task<ClientTcpContext<TSocketData>> ConnectAsync(IPEndPoint ipEndPoint, long socketId)
        {
            
            
            _log.InvokeInfoLog(null, "Attempt To Connect:" + ipEndPoint.Address + ":" + ipEndPoint.Port);

            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port);

            var clientTcpContext = _socketContextFactory();
            clientTcpContext.Id = socketId;

            await clientTcpContext.StartAsync(tcpClient,
                _socketSerializerFactory(),
                _lockObject,
                _log,
                null, 
                _deliveryBuffer);

            _log.InvokeInfoLog(clientTcpContext, "Connected. Id=" + clientTcpContext.Id);

            return clientTcpContext;

        }

        private async Task CheckDeadSocketAsync(ClientTcpContext<TSocketData> connection)
        {

            while (connection.Connected)
            {
                await Task.Delay(1000);
                connection.SocketStatistic.EachSecondTimer();
                var now = DateTime.UtcNow;

                var receiveInterval = now - connection.SocketStatistic.LastReceiveTime;

                if (receiveInterval > _disconnectInterval)
                {
                    var message = "Long time [" + receiveInterval +
                                  "] no received activity. Disconnecting socket " + connection.ContextName;
                    _log.InvokeInfoLog(connection, message);
                    
                    return;
                }

                if (DateTime.UtcNow - connection.SocketStatistic.LastPingSentDateTime > PingInterval)
                {
                    connection.SocketStatistic.LastPingSentDateTime = now;
                    await connection.SendPingAsync();
                }
            }

        }

        public ClientTcpContext<TSocketData> CurrentTcpContext { get; private set; }

        private async Task SocketThread()
        {

            long socketId = 0;
            while (_working)
            {
                var ipEndPoints = await _getHostPort().ParseAndResolveHostPort();

                foreach (var ipEndPoint in ipEndPoints)
                {
                    try
                    {
                        CurrentTcpContext = await ConnectAsync(ipEndPoint, socketId);
                        Connected = true;
                        socketId++;

                        CurrentTcpContext.StartReadThread(_readBufferSize);

                        await CheckDeadSocketAsync(CurrentTcpContext);
                        Console.WriteLine("Here");
                        CurrentTcpContext.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        _log.InvokeInfoLog(CurrentTcpContext, "Connection support exception:" + ex.Message);

                    }
                    finally
                    {
                        Connected = false;
                        CurrentTcpContext = null;
                    }
                }

                _log.InvokeInfoLog(null, "Making reconnection timeout: "+_reconnectTimeOut.ToString("g"));
                await Task.Delay(_reconnectTimeOut);
            }
            
        }

        private Task _socketLoop;
        
        public void Start()
        {

            if (_socketContextFactory == null)
                throw new Exception("Please specify socket factory");


            if (_socketSerializerFactory == null)
                throw new Exception("Please specify socket serializer factory");

            if (_working)
                return;

            _working = true;
            _socketLoop = SocketThread();
        }

        public void Stop()
        {
            _working = false;
            var currentTcpContext = CurrentTcpContext;
            currentTcpContext.Disconnect();
            var socketLoopTask = _socketLoop;
            socketLoopTask?.Wait();
        }
        
        
        public bool Connected { get; private set; }


    }
}
