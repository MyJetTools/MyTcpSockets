using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTcpSockets
{
    public class AsyncLock : IDisposable
    {
        private readonly object _lockObject;

        public AsyncLock(object lockObject)
        {
            _lockObject = lockObject ?? new object();
        }
        
        private readonly Queue<TaskCompletionSource<int>> _awaitingLocks = new Queue<TaskCompletionSource<int>>();
        
        private int _entered;
        private bool _stopped;
        
        public ValueTask LockAsync()
        {
            lock (_lockObject)
            {
                if (_stopped)
                    throw new Exception("AsyncLock is stopped");
                
                try
                {
                    if (_entered == 0)
                        return new ValueTask();

                    var task = new TaskCompletionSource<int>();
                    _awaitingLocks.Enqueue(task);
                    return new ValueTask();
                }
                finally
                {
                    _entered++;
                }
            }
        }

        public void Unlock()
        {
            lock (_lockObject)
            {
                try
                {
                    if (_awaitingLocks.Count == 0) 
                        return;
                    
                    var awaitingLock = _awaitingLocks.Dequeue();
                    awaitingLock.SetResult(0);
                }
                finally
                {
                    _entered--;
                }
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                _stopped = true;

                while (_awaitingLocks.Count>0)
                {
                    var awaitingLock = _awaitingLocks.Dequeue();
                    awaitingLock.SetException(new Exception("AsyncLock is stopped"));
                    _entered--;
                }
            }
        }
    }
}