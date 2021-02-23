using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public class TcpSocketPublisherSubscriber
    {
        private readonly LinkedList<ReadOnlyMemory<byte>> _queue = new LinkedList<ReadOnlyMemory<byte>>();

        private Task<ReadOnlyMemory<byte>> _awaitingTask;
        private byte[] _buffer;

        private readonly object _lockObject;
        public TcpSocketPublisherSubscriber(object lockObject)
        {
            _lockObject = lockObject;
        }


        private ReadOnlyMemory<byte> _bytesToYield;
        private ReadOnlyMemory<byte> TaskProc()
        {
            if (_stopped)
                throw new Exception("Publisher/Subscriber is stopped");
            
            return _bytesToYield;
        }
        
        public void Publish(ReadOnlyMemory<byte> itm)
        {
            if (_stopped)
                return;

            Task<ReadOnlyMemory<byte>> awaitingTask;
            
            lock (_lockObject)
            {
                _queue.AddLast(itm);

                if (_awaitingTask == null)
                    return;
                
                _bytesToYield = _queue.CompileAndCopyAndDispose(_buffer);
                awaitingTask = _awaitingTask;
                _awaitingTask = null;
            }

            awaitingTask?.Start();
        }

        private bool _stopped;

        public ValueTask<ReadOnlyMemory<byte>> DequeueAsync(byte[] deliveryBuffer)
        {
            lock (_lockObject)
            {
                var result = _queue.CompileAndCopyAndDispose(deliveryBuffer);
                if (result.Length > 0)
                    return new ValueTask<ReadOnlyMemory<byte>>(result);

                _buffer = deliveryBuffer;
                _awaitingTask = new Task<ReadOnlyMemory<byte>>(TaskProc);
                
                return new ValueTask<ReadOnlyMemory<byte>>(_awaitingTask);
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                _stopped = true;
                _queue.Clear();
                _awaitingTask?.Start();
                _awaitingTask = null;
            }
        }
    }
}