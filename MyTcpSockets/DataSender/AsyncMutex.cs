using System;
using System.Threading.Tasks;

namespace MyTcpSockets.DataSender
{
    public class AsyncMutex
    {
        private readonly object _lockObject = new  object();
        private bool _goAhead;
        
        public void Update(bool goAhead)
        {
            lock (_lockObject)
            {
                
                if (_goAhead == goAhead)
                    return;
                
                _goAhead = goAhead;

                if (_goAhead && _awaitingTask != null)
                {
                    var task = _awaitingTask;
                    _awaitingTask = null;
                    task.SetResult(0);
                }
            }
        }

        private TaskCompletionSource<int> _awaitingTask;

        private bool _stopped;

        public ValueTask AwaitDataAsync()
        {
            lock (_lockObject)
            {
                if (_stopped)
                    throw new Exception("Stopped");
                
                if (_goAhead)
                    return new ValueTask();
                
                _awaitingTask = new TaskCompletionSource<int>();
                return new ValueTask(_awaitingTask.Task);
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                _stopped = true;
                
                if (_awaitingTask != null)
                {
                    var task = _awaitingTask;
                    _awaitingTask = null;
                    task.SetException(new Exception("Stopped"));
                }
            }
        }
    }
}