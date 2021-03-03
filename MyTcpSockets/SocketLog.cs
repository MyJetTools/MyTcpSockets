using System;
using System.Collections.Generic;

namespace MyTcpSockets
{

#nullable enable
    
    public interface ISocketLogInvoker
    {
        void InvokeInfoLog(ITcpContext? ctx, string message);
        void InvokeExceptionLog(ITcpContext? ctx, Exception ex, bool debugLog);
    }
    
    public class SocketLog<T> : ISocketLogInvoker
    {
        private readonly T _masterObject;

        public SocketLog(T masterObject)
        {
            _masterObject = masterObject;
        }

        public bool DebugLogsAreEnabled { get; private set; }
        public void EnableDebugLog()
        {
            DebugLogsAreEnabled = true;
        }
        
        private readonly List<Action<ITcpContext?, string>> _logInfo = new List<Action<ITcpContext?, string>>();
        public T AddLogInfo(Action<ITcpContext?, string> logInfo)
        {
            _logInfo.Add(logInfo);
            return _masterObject;
        }
        
        private readonly List<Action<ITcpContext?, Exception>> _logException = new List<Action<ITcpContext?, Exception>>();
        public T AddLogException(Action<ITcpContext?, Exception> logException)
        {
            _logException.Add(logException);
            return _masterObject;
        }

        void ISocketLogInvoker.InvokeInfoLog(ITcpContext? ctx, string message)
        {
            if (_logInfo.Count == 0)
            {
                Console.WriteLine(ctx == null
                    ? $"{DateTime.UtcNow}: Socket Info: "
                    : $"{DateTime.UtcNow}: Socket {ctx.Id} Info: ");

                return;
            }
            
            foreach (var action in _logInfo)
            {
                try
                {
                    action(ctx, message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error invoking Info Log");
                    Console.WriteLine(e);
                }

            }
        }

        void ISocketLogInvoker.InvokeExceptionLog(ITcpContext? ctx, Exception ex, bool debugLog)
        {
            
            if (debugLog && !DebugLogsAreEnabled)
                return;
            
            if (_logInfo.Count == 0)
            {
                Console.WriteLine(ctx == null
                    ? $"{DateTime.UtcNow}: Socket Exception: "+ex
                    : $"{DateTime.UtcNow}: Socket {ctx.Id} Exception: "+ex);
                
                return;
            }

            foreach (var action in _logException)
            {
                try
                {
                    action(ctx, ex);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error invoking Exception Log");
                    Console.WriteLine(e);
                }
            }
        }
    }
}