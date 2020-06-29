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
        private readonly OutDataSender _outDataSender;

        private readonly object _lockObject = new object();
        
        private Action<object> _log;
        
        public TimeSpan PingInterval { get; private set; } = TimeSpan.FromSeconds(5);

        public MyClientTcpSocket(Func<string> getHostPort, TimeSpan reconnectTimeOut, int sendBufferSize = 1024*1024)
        {
            _outDataSender = new OutDataSender(_lockObject, sendBufferSize);
            _getHostPort = getHostPort;
            _reconnectTimeOut = reconnectTimeOut;
        }

        public MyClientTcpSocket<TSocketData> AddLog(Action<object> log)
        {
            _log = log;
            return this;
        }

        public MyClientTcpSocket<TSocketData> RegisterTcpContextFactory(Func<ClientTcpContext<TSocketData>> socketContextFactory)
        {
            _socketContextFactory = socketContextFactory;
            return this;
        }

        public MyClientTcpSocket<TSocketData> SetPingInterval(TimeSpan pingInterval)
        {
            PingInterval = pingInterval;
            return this;
        }

        public MyClientTcpSocket<TSocketData> RegisterTcpSerializerFactory(Func<ITcpSerializer<TSocketData>> socketSerializerFactory)
        {
            _socketSerializerFactory = socketSerializerFactory;
            return this;
        }


        private bool _working;

        private async Task<ClientTcpContext<TSocketData>> ConnectAsync(IPEndPoint ipEndPoint, long socketId)
        {

            _log?.Invoke("Attempt To Connect:" + ipEndPoint.Address + ":" + ipEndPoint.Port);

            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port);

            var clientTcpContext = _socketContextFactory();
            clientTcpContext.Id = socketId;

            await clientTcpContext.StartAsync(tcpClient, _socketSerializerFactory(), _outDataSender, _lockObject, _log, null);

            _log?.Invoke("Connected. Id=" + clientTcpContext.Id);

            return clientTcpContext;

        }

        private async Task CheckDeadSocketLoopAsync(ClientTcpContext<TSocketData> connection)
        {

            var lastSendPingTime = DateTime.UtcNow;

            try
            {
                while (connection.Connected)
                {
                    await Task.Delay(1000);

                    connection.SocketStatistic.EachSecondTimer();

                    var receiveInterval = DateTime.UtcNow - connection.SocketStatistic.LastReceiveTime;

                    if (receiveInterval > PingInterval * 3)
                    {
                        var message = "Long time [" + receiveInterval +
                                      "] no received activity. Disconnecting socket " + connection.ContextName;
                        _log?.Invoke(message);
                        throw new Exception(message);
                    }

                    if (DateTime.UtcNow - lastSendPingTime > PingInterval)
                    {
                        await connection.SendPingAsync();
                        lastSendPingTime = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception exception)
            {
                _log?.Invoke($"Socket {connection.Id} Ping Thread Exception: " + exception.Message);
            }
            finally
            {
                await connection.DisconnectAsync(); 
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
                        socketId++;

                        var readDataTask = CurrentTcpContext.ReadLoopAsync();
                        var pingLoopTask = CheckDeadSocketLoopAsync(CurrentTcpContext);

                        await Task.WhenAny(readDataTask, pingLoopTask);

                    }
                    catch (SocketException se)
                    {
                        if (se.SocketErrorCode == SocketError.ConnectionRefused)
                            _log?.Invoke("Connection support exception: " + se);
                    }
                    catch (Exception ex)
                    {
                        _log?.Invoke("Connection support fatal exception:" + ex);
                        _log?.Invoke(ex);
                    }
                    finally
                    {
                        CurrentTcpContext = null;
                    }
                }

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
            _outDataSender.Start();
            _socketLoop = SocketThread();
        }

        public void Stop()
        {
            _working = false;
            var currentTcpContext = CurrentTcpContext;
            currentTcpContext.DisconnectAsync().AsTask().Wait();
            var socketLoopTask = _socketLoop;
            socketLoopTask?.Wait();
            
            _outDataSender.Stop();

        }

        public bool Connected => CurrentTcpContext != null;

    }
}
