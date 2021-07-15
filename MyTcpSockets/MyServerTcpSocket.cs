using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MyTcpSockets
{
    public class MyServerTcpSocket<TSocketData>
    {
        private TcpListener _serverSocket;

        private readonly Connections<TSocketData>_connections;
        private readonly IPEndPoint _ipEndPoint;
        private readonly int _sendBufferSize;
        private readonly ISocketLogInvoker _log;
        public readonly SocketLog<MyServerTcpSocket<TSocketData>> Logs;

        public TimeSpan ReceiveDataTimeoutToKill { get; private set; } = TimeSpan.FromMinutes(1);
        
        /// <summary>
        /// When connection is established - init package must be sent by client to make sure it's a valid connection;
        /// </summary>
        public TimeSpan InitTimeoutToKill { get; private set; } = TimeSpan.FromSeconds(5);
        
        private readonly object _lockObject = new object();


        private int _readBufferSize = 1024 * 1024 * 2;

        public MyServerTcpSocket(IPEndPoint ipEndPoint, int sendBufferSize = 1024*1024)
        {
            if (sendBufferSize < 1024)
                throw new Exception("Buffer size must be more then 1024. Now size:" + sendBufferSize);
            
            _ipEndPoint = ipEndPoint;
            _sendBufferSize = sendBufferSize;
            _connections = new Connections<TSocketData>();

            Logs = new SocketLog<MyServerTcpSocket<TSocketData>>(this);
            _log = Logs;
        }

        public MyServerTcpSocket<TSocketData> SetReadBufferSize(int readBufferSize)
        {
            _readBufferSize = readBufferSize;
            return this;
        }

        private Func<ITcpSerializer<TSocketData>> _getSerializer;
        
        public MyServerTcpSocket<TSocketData> RegisterSerializer(Func<ITcpSerializer<TSocketData>> getSerializer)
        {
            _getSerializer = getSerializer;
            return this;
        }

        private Func<TcpContext<TSocketData>> _getContext;
        public MyServerTcpSocket<TSocketData> SetService(Func<TcpContext<TSocketData>> createContext)
        {
            _getContext = createContext;
            return this;
        }

        public MyServerTcpSocket<TSocketData> SetReceiveDataTimeoutToKill(TimeSpan timeSpan)
        {
            ReceiveDataTimeoutToKill = timeSpan;
            return this;
        }
        
        public MyServerTcpSocket<TSocketData> SetInitTimeoutToKill(TimeSpan timeSpan)
        {
            InitTimeoutToKill = timeSpan;
            return this;
        }



        private async Task CheckDeadConnectionsLoopAsync()
        {

            while (_working)
            {
                var now = DateTime.UtcNow;
                var connections =
                    _connections.GetAllConnections();

                foreach (var connection in connections.Where(itm => itm.Connected))
                {
                    try
                    {
                        connection.SocketStatistic.EachSecondTimer();

                        if (!connection.IsServerSocketDead(now, ReceiveDataTimeoutToKill, InitTimeoutToKill)) 
                            continue;
                        
                        _log.InvokeInfoLog(connection, $"Found dead connection {connection.ContextName} with ID {connection.Id}. Disconnecting...");
                        connection.Disconnect();
                    }
                    catch (Exception e)
                    {
                        _log.InvokeExceptionLog(connection, e, false);
                    }
                    
                }

                await Task.Delay(1000);
                
            }
        }

        private async Task KickOffNewSocketAsync(TcpContext<TSocketData> tcpContext, TcpClient acceptedSocket)
        {

            var bufferToSend = SocketMemoryUtils.AllocateByteArray(_sendBufferSize);

            await tcpContext.StartAsync(acceptedSocket, _getSerializer(), _lockObject, _log,
                socket => { _connections.RemoveSocket(socket.Id); }, bufferToSend);

            _log.InvokeInfoLog(tcpContext,
                $"Socket Accepted; Ip:{acceptedSocket.Client.RemoteEndPoint}. Id=" + tcpContext.Id);

            tcpContext.StartReadThread(_readBufferSize);
        }

        private async Task AcceptSocketLoopAsync()
        {

            _serverSocket = new TcpListener(_ipEndPoint);
            _serverSocket.Start();
            
            _log.InvokeInfoLog(null, "Started listening tcp socket: " + _ipEndPoint.Port);
            var socketId = 0;


            while (_working)
            {
                try
                {
                    var acceptedSocket = await _serverSocket.AcceptTcpClientAsync();
                    var connection = _getContext();
                    connection.Id = socketId;
                    socketId++;
                    
                    if (!_working)
                    {
                        connection.Disconnect();
                        throw new Exception("Server is being stopped. Socket accept process is canceled");
                    }

                    _connections.AddSocket(connection);
                    await KickOffNewSocketAsync(connection, acceptedSocket);
                }
                catch (Exception ex)
                {
                    _log.InvokeExceptionLog(null, ex, true);
                }
            }

        }

        public IReadOnlyList<TcpContext<TSocketData>> GetConnections()
        {
            return _connections.GetAllConnections();
        }

        public IEnumerable<TcpContext<TSocketData>> GetConnections(
            Func<TcpContext<TSocketData>, bool> filterCondition)
        {
            return _connections.GetConnections(filterCondition);
        }

        private Task _theTask;
        private bool _working; 
        public void Start()
        {
            
            if (_getContext == null)
                throw new Exception("Please specify socket context factory");

            
            if (_getSerializer == null)
                throw new Exception("Please specify socket serializer factory");

            if (_working)
                return;

            _working = true;
            
            Task.Run(CheckDeadConnectionsLoopAsync);

            _theTask = AcceptSocketLoopAsync();
        }

        public void Stop()
        {
            
            if (!_working)
                return;

            _working = false;

            _serverSocket.Server.Close(1000);
            _serverSocket.Stop();

            foreach (var connection in _connections.GetAllConnections())
            {
                connection.Disconnect();
            }

            _theTask.Wait();
            
        }

        public int Count => _connections.Count;

    }

}

