using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyTcpSockets
{
   public class OutDataSender
    {
        private readonly object _lockObject;

        private readonly Dictionary<long, ITcpContext> _socketsWithData = new Dictionary<long, ITcpContext>();

        private TaskCompletionSource<int> _notifyMePlease;

        private readonly byte[] _bufferToSend;

        public OutDataSender(object lockObject, int maxPacketSize)
        {
            _lockObject = lockObject;
            _bufferToSend = new byte[maxPacketSize];
        }
        
        private bool Working { get; set; }


        private void PushTask()
        {
            if (_notifyMePlease == null)
                return;

            try
            {
                var task = _notifyMePlease;
                _notifyMePlease = null;
                task.SetResult(0);
            }
            catch (Exception e)
            {
                Console.WriteLine("Warning: set result to send task");
                Console.WriteLine(e);
            }
            
        }

        public void EnqueueSendData(ITcpContext tcpContext, ReadOnlyMemory<byte> dataToSend)
        {
            lock (_lockObject)
            {
                tcpContext.DataToSend.Enqueue(dataToSend);
                if (!_socketsWithData.ContainsKey(tcpContext.Id))
                    _socketsWithData.Add(tcpContext.Id, tcpContext);
                
                PushTask();
            }
        }



        private Task WaitNewDataAsync()
        {
            lock (_lockObject)
            {
                if (_socketsWithData.Count > 0)
                    return Task.CompletedTask;

                _notifyMePlease = new TaskCompletionSource<int>();
                return _notifyMePlease.Task;
            }
        }


        private (ITcpContext tcpContext, ReadOnlyMemory<byte> dataToSend) GetNextSocketToSendData()
        {
            lock (_lockObject)
            {

                while (_socketsWithData.Count>0)
                {
                
                    var itm = _socketsWithData.First();

                    var socketId = itm.Key;
                    var tcpContext = itm.Value;

                    if (!tcpContext.Connected)
                    {
                        Console.WriteLine("Skipping sending to Disconnected socket: "+socketId);
                        tcpContext.DataToSend.Clear();
                        _socketsWithData.Remove(socketId);
                        continue;
                    }
                
                    var dataToSend = tcpContext.CompileDataToSend(_bufferToSend);

                    if (tcpContext.DataToSend.Count == 0)
                        _socketsWithData.Remove(socketId);
                
                    return (tcpContext, dataToSend); 
                }


            }
            
            return (null, null);
        }


        private async Task WriteThreadAsync()
        {
            while (Working)
            {
                await WaitNewDataAsync();
                var (tcpContext, dataToSend) = GetNextSocketToSendData();
                while (tcpContext != null)
                {
                    try
                    {
                        await tcpContext.SocketStream.WriteAsync(dataToSend);
                    }
                    catch (Exception)
                    {
                        await tcpContext.DisconnectAsync();
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
                PushTask();
            }

            _task.Wait();
        }
        
    }
}