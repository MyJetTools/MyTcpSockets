using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTcpSockets.DataSender
{

    public class OutDataSender
    {

        private readonly Dictionary<long, ITcpContext> _socketsWithData = new Dictionary<long, ITcpContext>();

        private readonly byte[] _bufferToSend;
        
        private readonly AsyncMutex _asyncMutex = new AsyncMutex();

        public OutDataSender(int maxPacketSize)
        {
            _bufferToSend = new byte[maxPacketSize];
        }

        private Action<ITcpContext, object> _log;

        public void RegisterLog(Action<ITcpContext, object> log)
        {
            _log = log;
        }

        private bool Working { get; set; }


        public void PushData(ITcpContext tcpContext)
        {
            lock (_socketsWithData)
            {
                if (!_socketsWithData.ContainsKey(tcpContext.Id))
                    _socketsWithData.Add(tcpContext.Id, tcpContext);
                
                _asyncMutex.Update(true);
            }
            
            
        }


        private void CleanDisconnectedSocket(ITcpContext ctx)
        {

            lock (_socketsWithData)
            {
                _socketsWithData.Remove(ctx.Id);
                _asyncMutex.Update(_socketsWithData.Count>0);
            }
                
            
        }

        
        
        private readonly List<ITcpContext> _socketsReusableObject = new List<ITcpContext>();

        private IReadOnlyList<ITcpContext> GetSocketsToSend()
        {
            _socketsReusableObject.Clear();
            lock (_socketsWithData)
            {
                _socketsReusableObject.AddRange(_socketsWithData.Values);
                return _socketsReusableObject;
            }
                
        }


        private async Task WriteThreadAsync()
        {

            while (Working)
            {

                await _asyncMutex.AwaitDataAsync();

                foreach (var tcpContext in GetSocketsToSend())
                {

                    if (!tcpContext.Connected)
                    {
                        CleanDisconnectedSocket(tcpContext);
                        continue;
                    }
                    
                    try
                    {
                        var dataToSend = tcpContext.DataToSend.Dequeue(_bufferToSend);
                        if (dataToSend.Length >0)
                            await tcpContext.SocketStream.WriteAsync(dataToSend);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        _log?.Invoke(tcpContext, e);

                        await tcpContext.DisconnectAsync();
                   
                        CleanDisconnectedSocket(tcpContext);
                    }
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

            Working = false;
            _asyncMutex.Stop();

            _task.Wait();
        }

    }
}