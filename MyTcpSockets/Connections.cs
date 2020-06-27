using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MyTcpSockets
{

        public class Connections<T>
        {
            private readonly Dictionary<long, TcpContext<T>> _sockets = new Dictionary<long, TcpContext<T>>();

            private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

            public void RemoveSocket(long connectionId)
            {
                _lockSlim.EnterWriteLock();
                try
                {
                    if (_sockets.ContainsKey(connectionId))
                        _sockets.Remove(connectionId);
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }

            }

            public void AddSocket(TcpContext<T> connection)
            {
                _lockSlim.EnterWriteLock();
                try
                {
                    _sockets.Add(connection.Id, connection);
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }

            }

            public IReadOnlyList<TcpContext<T>> GetAllConnections()
            {
                _lockSlim.EnterReadLock();
                try
                {
                    return _sockets.Values.ToList();
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }
            }

            public IReadOnlyList<TcpContext<T>> GetConnections(Func<TcpContext<T>, bool> condition)
            {
                _lockSlim.EnterReadLock();
                try
                {
                    return _sockets.Values.Where(condition).ToList();
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }
            }

            public int Count
            {
                get
                {
                    _lockSlim.EnterReadLock();
                    try
                    {
                        return _sockets.Count;
                    }
                    finally
                    {
                        _lockSlim.ExitReadLock();
                    }

                }
            }
        }

}
