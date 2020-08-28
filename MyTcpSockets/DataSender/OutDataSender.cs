using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyTcpSockets.DataSender;

namespace MyTcpSockets
{

    public class OutDataSender
    {

        private readonly Dictionary<long, ITcpContext> _socketsWithData = new Dictionary<long, ITcpContext>();

        private readonly byte[] _bufferToSend;
        
        private readonly ProducerConsumer<ITcpContext> _producerConsumer = new ProducerConsumer<ITcpContext>();

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
            _producerConsumer.Produce(tcpContext);
        }


        private async Task WriteThreadAsync()
        {

            while (Working)
            {

                var tcpContext = await _producerConsumer.ConsumeAsync();

                var sendData = tcpContext.DataToSend.Dequeue(_bufferToSend);
                if (sendData.Length > 0)
                    try
                    {
                        await tcpContext.SocketStream.WriteAsync(sendData);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        _log?.Invoke(tcpContext, e);

                        await tcpContext.DisconnectAsync();

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
            _producerConsumer.Stop();
            _task.Wait();
        }

    }
}