using System;
using System.Threading.Tasks;

namespace MyTcpSockets.DataSender
{

    public class OutDataSender
    {

        private readonly byte[] _bufferToSend;
        
        private readonly ProducerConsumer<ITcpContext> _producerConsumer = new ProducerConsumer<ITcpContext>();

        private readonly SendDataQueue _sendDataQueue;

        private readonly object _lockObject;

        private readonly ITcpContext _tcpContext;

        public OutDataSender(ITcpContext tcpContext, object lockObject, byte[] bufferToSend, Action<ITcpContext, object> log)
        {
            _tcpContext = tcpContext;
            _lockObject = lockObject;
            _sendDataQueue = new SendDataQueue(lockObject);
            _bufferToSend = bufferToSend;
            _log = log;
            Start();
        }

        private readonly Action<ITcpContext, object> _log;

        public bool Working { get; private set; }
        
        
        private Task _writeTask;


        public void PushData(ReadOnlyMemory<byte> dataToSend)
        {
            _sendDataQueue.Enqueue(dataToSend);
            _producerConsumer.Produce(_tcpContext);
            
            
            if (_writeTask != null) return;
            
            lock (_lockObject)
                _writeTask ??= WriteThreadAsync();

        }


        private async Task WriteThreadAsync()
        {
            while (_tcpContext.Connected)
            {
                var tcpContext = await _producerConsumer.ConsumeAsync();

                var sendData = _sendDataQueue.Dequeue(_bufferToSend);

                if (sendData.Length > 0)
                    await tcpContext.SendDataToSocketAsync(sendData);

            }

        }

        private Task _task;

        private void Start()
        {
            Working = true;

            _task = WriteThreadAsync();
        }

        public Task StopAsync()
        {
            Working = false;
            _producerConsumer.Stop();
            _sendDataQueue.Clear();
            return _task;
        }

    }
}