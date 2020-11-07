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
        private Action<ITcpContext, object> _log;

        public TimeSpan ReceiveDataTimeoutToKill { get; private set; } = TimeSpan.FromMinutes(1);
        
        /// <summary>
        /// When connection is established - init package must be sent by client to make sure it's a valid connection;
        /// </summary>
        public TimeSpan InitTimeoutToKill { get; private set; } = TimeSpan.FromSeconds(5);
        
        private readonly object _lockObject = new object();

        public MyServerTcpSocket(IPEndPoint ipEndPoint, int sendBufferSize = 0)
        {
            _ipEndPoint = ipEndPoint;
            _sendBufferSize = sendBufferSize;
            _connections = new Connections<TSocketData>();
        }

        public MyServerTcpSocket<TSocketData> AddLog(Action<ITcpContext, object> log)
        {
            _log = log;
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
                        
                        _log.Invoke(connection, $"Found dead connection {connection.ContextName} with ID {connection.Id}. Disconnecting...");
                        await connection.DisconnectAsync();
                    }
                    catch (Exception e)
                    {
                        _log.Invoke(connection, e);
                    }
                    
                }

                await Task.Delay(1000);
                
            }
        }

        private async Task KickOffNewSocketAsync(TcpContext<TSocketData> tcpContext, TcpClient acceptedSocket)
        {

            var bufferToSend = _sendBufferSize > 0 ? SocketMemoryUtils.AllocateByteArray(_sendBufferSize) : null;

            await tcpContext.StartAsync(acceptedSocket, _getSerializer(), _lockObject, _log,
                socket => { _connections.RemoveSocket(socket.Id); }, bufferToSend);

            _log?.Invoke(tcpContext,
                $"Socket Accepted; Ip:{acceptedSocket.Client.RemoteEndPoint}. Id=" + tcpContext.Id);

            tcpContext.StartReadThread();
        }

        private async Task AcceptSocketLoopAsync()
        {

            _serverSocket = new TcpListener(_ipEndPoint);
            _serverSocket.Start();
            
            _log?.Invoke(null, "Started listening tcp socket: " + _ipEndPoint.Port);
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
                        connection.DisconnectAsync().AsTask().Wait();
                        throw new Exception("Server is being stopped. Socket accept process is canceled");
                    }

                    _connections.AddSocket(connection);
                    await KickOffNewSocketAsync(connection, acceptedSocket);
                }
                catch (Exception ex)
                {
                    _log?.Invoke(null, "Error accepting socket: " + ex.Message);
                }
            }

        }

        public IReadOnlyList<TcpContext<TSocketData>> GetConnections(
            Func<TcpContext<TSocketData>, bool> filterCondition = null)
        {

            return filterCondition == null
                ? _connections.GetAllConnections()
                : _connections.GetConnections(filterCondition);
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
                connection.DisconnectAsync().AsTask().Wait();
            }

            _theTask.Wait();
            
        }

        public int Count => _connections.Count;

    }

}

