using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public class TcpSocketPublisherSubscriber
    {
        private readonly LinkedList<ReadOnlyMemory<byte>> _queue = new LinkedList<ReadOnlyMemory<byte>>();

        private TaskCompletionSource<ReadOnlyMemory<byte>> _awaitingTask;
        private byte[] _buffer;

        private readonly object _lockObject;
        public TcpSocketPublisherSubscriber(object lockObject)
        {
            _lockObject = lockObject;
        }
        public void Publish(ReadOnlyMemory<byte> itm)
        {
            if (_stopped)
                return;
            
            lock (_lockObject)
            {
                _queue.AddLast(itm);

                if (_awaitingTask != null)
                    ProcessAwaitingTask();
            }
        }

        private bool _stopped;

        private void ProcessAwaitingTask()
        {
            var result = _queue.ReadAndCopyTo(_buffer);
            var task = _awaitingTask;
            _awaitingTask = null;
            task.SetResult(result);
        }


        public ValueTask<ReadOnlyMemory<byte>> DequeueAsync(byte[] deliveryBuffer)
        {
            lock (_lockObject)
            {
                var result = _queue.ReadAndCopyTo(deliveryBuffer);
                if (result.Length > 0)
                    return new ValueTask<ReadOnlyMemory<byte>>(result);

                _buffer = deliveryBuffer;
                _awaitingTask = new TaskCompletionSource<ReadOnlyMemory<byte>>();
                return new ValueTask<ReadOnlyMemory<byte>>(_awaitingTask.Task);
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                _stopped = true;
                _queue.Clear();
                _awaitingTask?.SetException(new Exception("PublisherSubscriber is stopped"));
                _awaitingTask = null;
            }
        }
    }
}