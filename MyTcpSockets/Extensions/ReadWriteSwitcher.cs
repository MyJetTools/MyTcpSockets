using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public enum TcpReaderSwitchMode
    {
       Write, Read, Stopped   
    }
    
    public class ReadWriteSwitcher
    {
        private TcpReaderSwitchMode _mode = TcpReaderSwitchMode.Write;

        private readonly object _lockObject = new object();

        private TaskCompletionSource<int> _writeModeAwaiter;

        private void TaskProcess()
        {
            if (_mode == TcpReaderSwitchMode.Read)
            {
                Console.WriteLine($"Read is in thead: {Thread.CurrentThread.ManagedThreadId}");
            }
            if (_mode == TcpReaderSwitchMode.Stopped)
                throw new Exception("Service is stopping");
        }
        
        public ValueTask WaitUntilWriteModeIsSetAsync(CancellationToken token)
        {

            if (_mode == TcpReaderSwitchMode.Stopped)
                throw new Exception("Service is stopping");
            
            lock (_lockObject)
            {
                if (_mode == TcpReaderSwitchMode.Write)
                    return new ValueTask();
                
                if (_writeModeAwaiter != null)
                    return new ValueTask(_writeModeAwaiter.Task);

                _writeModeAwaiter = new TaskCompletionSource<int>(token);
                return new ValueTask(_writeModeAwaiter.Task);
            }
        }

        public void SetToReadMode()
        {
            if (_mode == TcpReaderSwitchMode.Stopped)
                throw new Exception("Service is stopping");
            
            lock (_lockObject)
            {
                if (_mode == TcpReaderSwitchMode.Read)
                    return;

                _mode = TcpReaderSwitchMode.Read;
                
                if (_readModeAwaiter == null)
                    return;

                var result = _readModeAwaiter;
                _readModeAwaiter = null;
                result.SetResult(0);
            }
        }


        private TaskCompletionSource<int> _readModeAwaiter;
        public ValueTask WaitUntilReadModeIsSetAsync(CancellationToken token)
        {
            if (_mode == TcpReaderSwitchMode.Stopped)
                throw new Exception("Service is stopping");
            
            lock (_lockObject)
            {
                if (_mode == TcpReaderSwitchMode.Read)
                    return new ValueTask();

                if (_readModeAwaiter != null)
                    return new ValueTask(_readModeAwaiter.Task);

                _readModeAwaiter = new TaskCompletionSource<int>(token);
                return new ValueTask(_readModeAwaiter.Task);
            }
        }
        

        public void SetToWriteMode()
        {
            if (_mode == TcpReaderSwitchMode.Stopped)
                throw new Exception("Service is stopping");

            lock (_lockObject)
            {
                
                if (_mode == TcpReaderSwitchMode.Write)
                    return;

                _mode = TcpReaderSwitchMode.Write;
                
                if (_writeModeAwaiter == null)
                    return;

                var result = _writeModeAwaiter;
                _writeModeAwaiter = null;
                result.SetResult(0);
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                _mode = TcpReaderSwitchMode.Stopped;
                if (_readModeAwaiter != null)
                {
                    var readModeAwaiter = _readModeAwaiter;
                    _readModeAwaiter = null;
                    readModeAwaiter.SetException(new Exception("We are stopping"));
                }
                
                if (_writeModeAwaiter != null)
                {
                    var writeModeAwaiter = _writeModeAwaiter;
                    _writeModeAwaiter = null;
                    writeModeAwaiter.SetException(new Exception("We are stopping"));
                }
            }
        }

    }
    
}