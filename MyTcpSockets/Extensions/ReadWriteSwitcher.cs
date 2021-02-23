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

        private Task _writeModeAwaiter;

        private void TaskProcess()
        {
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
                    return new ValueTask(_writeModeAwaiter);

                _writeModeAwaiter = new Task(TaskProcess, token);
                return new ValueTask(_writeModeAwaiter);
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
                result.Start();
            }
        }


        private Task _readModeAwaiter;
        public ValueTask WaitUntilReadModeIsSetAsync(CancellationToken token)
        {
            if (_mode == TcpReaderSwitchMode.Stopped)
                throw new Exception("Service is stopping");
            
            lock (_lockObject)
            {
                if (_mode == TcpReaderSwitchMode.Read)
                    return new ValueTask();

                if (_readModeAwaiter != null)
                    return new ValueTask(_readModeAwaiter);

                _readModeAwaiter = new Task(TaskProcess, token);
                return new ValueTask(_readModeAwaiter);
            }
        }
        
        NotCompile
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
                result.Start();
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
                    readModeAwaiter.Start();
                }
                
                if (_writeModeAwaiter != null)
                {
                    var writeModeAwaiter = _writeModeAwaiter;
                    _writeModeAwaiter = null;
                    writeModeAwaiter.Start();
                }
            }
        }

    }
    
}