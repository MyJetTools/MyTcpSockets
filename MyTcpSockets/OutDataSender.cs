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

        private readonly List<TaskCompletionSource<int>> _notifyMePlease = new List<TaskCompletionSource<int>>();

        private readonly byte[] _bufferToSend;

        public OutDataSender(object lockObject, int maxPacketSize)
        {
            _lockObject = lockObject;
            _bufferToSend = new byte[maxPacketSize];
        }
        
        private bool Working { get; set; }


        private void PushTask()
        {
            if (_notifyMePlease.Count > 0)
            {
                foreach (var taskCompletionSource in _notifyMePlease)
                    try
                    {
                        taskCompletionSource.SetResult(0);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Warning: set result to send task");
                        Console.WriteLine(e);
                    }
                    
                _notifyMePlease.Clear();
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
                if (_socketsWithData.Count>0)
                    return Task.CompletedTask;
                
                var result = new TaskCompletionSource<int>();
                _notifyMePlease.Add(result);
                return result.Task;
            }
        }


        private (ITcpContext tcpContext, ReadOnlyMemory<byte> dataToSend) GetNextSocketToSendData()
        {
            lock (_lockObject)
            {

                while (_socketsWithData.Count>0)
                {
                
                    var (socketId, tcpContext) = _socketsWithData.First();

                    if (!tcpContext.Connected)
                    {
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