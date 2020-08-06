using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyTcpSockets.DataSender
{

    public class OutDataSender
    {
        private readonly object _lockObject;

        private readonly Dictionary<long, ITcpContext> _socketsWithData = new Dictionary<long, ITcpContext>();

        private readonly byte[] _bufferToSend;
        
        
        private readonly AsyncMutex _asyncMutex = new AsyncMutex();

        public OutDataSender(object lockObject, int maxPacketSize)
        {
            _lockObject = lockObject;
            _bufferToSend = new byte[maxPacketSize];
        }

        private Action<ITcpContext, object> _log;

        public void RegisterLog(Action<ITcpContext, object> log)
        {
            _log = log;
        }

        private bool Working { get; set; }
        
        public int Count { get; private set; }


        public void EnqueueSendData(ITcpContext tcpContext, ReadOnlyMemory<byte> dataToSend)
        {

            lock (_lockObject)
            {
                tcpContext.DataToSend.Enqueue(dataToSend);
                if (!_socketsWithData.ContainsKey(tcpContext.Id))
                    _socketsWithData.Add(tcpContext.Id, tcpContext);

                Count = _socketsWithData.Count;
            }
            
            _asyncMutex.Update(true);
        }


        private void CleanDisconnectedSocket(ITcpContext ctx)
        {
            ctx.DataToSend.Clear();
            _socketsWithData.Remove(ctx.Id);
            Count = _socketsWithData.Count;
            _asyncMutex.Update(_socketsWithData.Count>0);
        }

        private (ITcpContext tcpContext, ReadOnlyMemory<byte> dataToSend) GetNextSocketToSendData()
        {
            lock (_lockObject)
            {
                if (_socketsWithData.Count == 0)
                    return (null, Array.Empty<byte>());

                var tcpContext = _socketsWithData.Values.First();

                if (tcpContext.DataToSend.Length == 0)
                {
                    _socketsWithData.Remove(tcpContext.Id);
                    _asyncMutex.Update(_socketsWithData.Count>0);
                    return (null, Array.Empty<byte>());
                }

                if (!tcpContext.Connected)
                {
                    Console.WriteLine("Skipping sending to Disconnected socket: " + tcpContext.Id);
                    CleanDisconnectedSocket(tcpContext);
                    return (null, Array.Empty<byte>());
                }

                var dataToSend = tcpContext.DataToSend.Dequeue(_bufferToSend);
                return (tcpContext, dataToSend);
            }

        }


        private async Task WriteThreadAsync()
        {
            while (Working)
            {
                Console.WriteLine("BeforeMutex. Count: "+Count);
                await _asyncMutex.AwaitDataAsync();
                Console.WriteLine("AfterMutex. Count: "+Count);
                var (tcpContext, dataToSend) = GetNextSocketToSendData();
                while (tcpContext != null)
                {
                    try
                    {
                        await tcpContext.SocketStream.WriteAsync(dataToSend);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        _log?.Invoke(tcpContext, e);

                        await tcpContext.DisconnectAsync();
                        lock (_lockObject)
                            CleanDisconnectedSocket(tcpContext);
                    }

                    (tcpContext, dataToSend) = GetNextSocketToSendData();
                }

            }

        }

        private Task _task;

        public void Start()
        {
            Working = true;

            _task = WriteThreadAsync();
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                Working = false;
                _asyncMutex.Stop();
            }

            _task.Wait();
        }

    }
}