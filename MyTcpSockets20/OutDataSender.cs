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
                    taskCompletionSource.SetResult(0);
                    
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
                
                    var itm = _socketsWithData.First();

                    if (!itm.Value.Connected)
                    {
                        itm.Value.DataToSend.Clear();
                        _socketsWithData.Remove(itm.Key);
                        continue;
                    }
                
                    var dataToSend = itm.Value.CompileDataToSend(_bufferToSend);

                    if (itm.Value.DataToSend.Count == 0)
                        _socketsWithData.Remove(itm.Key);

                    return (itm.Value, dataToSend); 
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