﻿using System;
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

        private Action<ITcpContext, object> _log;

        public TimeSpan PingInterval { get; private set; } = TimeSpan.FromSeconds(5);

        public MyClientTcpSocket(Func<string> getHostPort, TimeSpan reconnectTimeOut, int sendBufferSize = 1024 * 1024)
        {
            _outDataSender = new OutDataSender(_lockObject, sendBufferSize);
            _getHostPort = getHostPort;
            _reconnectTimeOut = reconnectTimeOut;
        }

        public MyClientTcpSocket<TSocketData> AddLog(Action<ITcpContext, object> log)
        {
            _log = log;
            return this;
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
            _log?.Invoke(null, "Attempt To Connect:" + ipEndPoint.Address + ":" + ipEndPoint.Port);

            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port);

            var clientTcpContext = _socketContextFactory();
            clientTcpContext.Id = socketId;

            await clientTcpContext.StartAsync(tcpClient,
                _socketSerializerFactory(),
                _outDataSender,
                _lockObject,
                _log,
                null);

            _log?.Invoke(clientTcpContext, "Connected. Id=" + clientTcpContext.Id);

            return clientTcpContext;

        }


        private async Task DeadSocketLoopAsync()
        {
            while (_working)
            {
                var currentSocket = CurrentTcpContext;

                if (currentSocket != null)
                {
                    try
                    {
                        if (currentSocket.Connected)
                            await CheckDeadSocketAsync(currentSocket);
                    }
                    catch (Exception e)
                    {
                        _log?.Invoke(currentSocket, e);
                    }
                }

                await Task.Delay(1000);
            }
        }

        private async Task CheckDeadSocketAsync(ClientTcpContext<TSocketData> connection)
        {

            var lastSendPingTime = DateTime.UtcNow;

            connection.SocketStatistic.EachSecondTimer();

            var receiveInterval = DateTime.UtcNow - connection.SocketStatistic.LastReceiveTime;

            if (receiveInterval > PingInterval * 3)
            {
                var message = "Long time [" + receiveInterval +
                              "] no received activity. Disconnecting socket " + connection.ContextName;
                _log?.Invoke(connection, message);
                throw new Exception(message);
            }

            if (DateTime.UtcNow - lastSendPingTime > PingInterval)
            {
                await connection.SendPingAsync();
            }
        }


        public ClientTcpContext<TSocketData> CurrentTcpContext { get; private set; }

        private async Task SocketThread()
        {
            var checkConnectionsTask = DeadSocketLoopAsync();

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

                        await readDataTask;
                    }
                    catch (Exception ex)
                    {
                        _log?.Invoke(CurrentTcpContext, "Connection support exception:" + ex.Message);
                        _log?.Invoke(CurrentTcpContext, ex);
                    }
                    finally
                    {
                        CurrentTcpContext = null;
                    }
                }

                await Task.Delay(_reconnectTimeOut);
            }

            await checkConnectionsTask;
            
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
