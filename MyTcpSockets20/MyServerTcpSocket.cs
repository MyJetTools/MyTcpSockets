using System;
using System.Collections.Generic;
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
        private Action<object> _log;
        
        private readonly OutDataSender _outDataSender;
        
        private readonly object _lockObject = new object();

        public MyServerTcpSocket(IPEndPoint ipEndPoint, int sendBufferSize = 1024 * 1024)
        {
            _ipEndPoint = ipEndPoint;
            _connections = new Connections<TSocketData>();
            _outDataSender = new OutDataSender(_lockObject, sendBufferSize);
        }


        public MyServerTcpSocket<TSocketData> AddLog(Action<object> log)
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



        private async Task CheckDeadConnectionsLoopAsync()
        {

            while (_working)
            {
                var now = DateTime.UtcNow;
                var connections =
                    _connections.GetAllConnections();


                foreach (var connection in connections)
                {
                    try
                    {
                        connection.SocketStatistic.EachSecondTimer();

                        if ((now - connection.SocketStatistic.LastReceiveTime).TotalMinutes >= 1)
                        {
                            _log.Invoke($"Found dead connection {connection.ContextName} with ID {connection.Id}. Disconnecting...");
                            await connection.DisconnectAsync();
                            _connections.RemoveSocket(connection.Id);
                        }

                    }
                    catch (Exception e)
                    {
                        _log.Invoke(e);
                    }
                    
                }

                await Task.Delay(1000);
                
            }
        }

        private void KickOffNewSocket(TcpContext<TSocketData> tcpContext, TcpClient acceptedSocket)
        {
            Task.Run(async ()=>
            {
                await tcpContext.StartAsync(acceptedSocket, _getSerializer(),_outDataSender, _lockObject, _log);  
                _log?.Invoke($"Socket Accepted; Ip:{acceptedSocket.Client.RemoteEndPoint}. Id=" + tcpContext.Id);
                _connections.AddSocket(tcpContext);
                
                try
                {
                    await tcpContext.ReadLoopAsync();
                }
                finally
                {
                    _log?.Invoke("Removing connection: "+tcpContext.ContextName+" with id:"+tcpContext.Id);
                    _connections.RemoveSocket(tcpContext.Id);
                    await tcpContext.DisconnectAsync();
                }
                
            });
        }
        
        private async Task AcceptSocketLoopAsync()
        {

            _serverSocket = new TcpListener(_ipEndPoint);
            _serverSocket.Start();
            _outDataSender.Start();
            _log?.Invoke("Started listening tcp socket: " + _ipEndPoint.Port);
            var socketId = 0;
            

            while (_working)
            {
                try
                {
                    _log?.Invoke("Trying to accept a socket on: ");
                    var acceptedSocket = await _serverSocket.AcceptTcpClientAsync();
                    var connection = _getContext();
                    connection.Id = socketId;
                    socketId++;
                    
                    if (!_working)
                    {
                        connection.DisconnectAsync().AsTask().Wait();
                        throw new Exception("Server is being stopped. Socket accept process is canceled");
                    }

                    KickOffNewSocket(connection, acceptedSocket);

                }
                catch (Exception ex)
                {
                    _log?.Invoke("Error accepting socket: " + ex.Message);
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
            
            _outDataSender.Stop();
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

