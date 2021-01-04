using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{

    public class TcpDataReader
    {
        public  int ReadBufferSize { get; }
        private readonly int _minAllocationSize;
        
        private readonly List<TcpDataPiece> _incomingPackages = new List<TcpDataPiece>();

        private readonly object _lockObject = new object();
        private readonly object _readLock = new object();

        public TcpDataReader(int readBufferSize, int minAllocationSize)
        {
            ReadBufferSize = readBufferSize;
            _minAllocationSize = minAllocationSize;
            _incomingPackages.Add(new TcpDataPiece(readBufferSize));
        }

        #region write

        public Memory<byte> AllocateBufferToWrite()
        {
            lock (_lockObject)
            {

                var result = _incomingPackages[_incomingPackages.Count - 1].AllocateBufferToWrite();

                if (result.Length > 0)
                    return result;

                _incomingPackages.Add(new TcpDataPiece(ReadBufferSize));

                return _incomingPackages[_incomingPackages.Count - 1].AllocateBufferToWrite();

            }
        }
        
        public (byte[] buffer, int start, int len) AllocateBufferToWriteLegacy()
        {
            lock (_lockObject)
            {
                var result = _incomingPackages[_incomingPackages.Count-1].AllocateBufferToWriteLegacy();
                
                if (result.len > 0)
                    return result;

                _incomingPackages.Add(new TcpDataPiece(ReadBufferSize));

                return _incomingPackages[_incomingPackages.Count-1].AllocateBufferToWriteLegacy(); 
            }
        }

        private void GcIfNeeded()
        {
            if (_incomingPackages[0].ReadyToReadStart == 0)
                return;

            if (_incomingPackages[0].ReadyToReadSize > 64 && _incomingPackages[0].WriteSize > _minAllocationSize)
                return;

            //We Wait until we finish reading
            Monitor.Enter(_readLock);
            try
            {
                _incomingPackages[0].Gc();
            }
            finally
            {
                Monitor.Exit(_readLock);
            }

        }

        public void CommitWrittenData(int len)
        {
            lock (_lockObject)
            {
                _incomingPackages[_incomingPackages.Count-1].CommitWrittenData(len);

                GcIfNeeded();
                
                if (_sizeToRead>0)
                    TryToPushSizedRead();
                
                if (_marker != null)
                    TryToPushMarkerRead();

                if (_taskCompletionSourceByte != null)
                    TryToPushByte();
            }

        }
        #endregion


        
        private void TryToPushByte()
        {
            var (hasResult, result) = _incomingPackages.TryReadByte();
            if (!hasResult) 
                return;
            
            Monitor.Enter(_readLock);
            _taskCompletionSourceByte.SetResult(result);
            _taskCompletionSourceByte = null;
        }

        private void TryToPushSizedRead()
        {
            if (_taskCompletionSource != null)
            {
                var sizedResult = _incomingPackages.TryCompilePackage(_sizeToRead);
                if (sizedResult.Length == 0)
                    return;

                Monitor.Enter(_readLock);
                _taskCompletionSource.SetResult(sizedResult);
                _sizeToRead = -1;
                _taskCompletionSource = null;
            }
        }
        
        private void TryToPushMarkerRead()
        {
            if (_taskCompletionSource != null)
            {
                var size = _incomingPackages.GetSizeByMarker(_marker);
                if (size < 0)
                    return;

                var result = _incomingPackages.TryCompilePackage(size);
                Monitor.Enter(_readLock);
                _taskCompletionSource.SetResult(result);
                _marker = null;
                _taskCompletionSource = null;
            }
        }


        private int _sizeToRead = -1;

        private byte[] _marker;
        private TaskCompletionSource<ReadOnlyMemory<byte>> _taskCompletionSource;
        private TaskCompletionSource<byte> _taskCompletionSourceByte;
        
        
        public ValueTask<byte> ReadByteAsync(CancellationToken token)
        {
            lock (_lockObject)
            {
                var (hasResult, result) = _incomingPackages.TryReadByte();

                if (hasResult)
                {
                    Monitor.Enter(_readLock);
                    return new ValueTask<byte>(result);
                }

                
                _taskCompletionSourceByte = new TaskCompletionSource<byte>(token);
                return new ValueTask<byte>(_taskCompletionSourceByte.Task);
            }
        }

        public ValueTask<ReadOnlyMemory<byte>> ReadAsyncAsync(int size, CancellationToken token)
        {
            lock (_lockObject)
            {
                var result = _incomingPackages.TryCompilePackage(size);

                Console.WriteLine(result.Length);

                if (result.Length > 0)
                {
                    Monitor.Enter(_readLock);
                    return new ValueTask<ReadOnlyMemory<byte>>(result);
                }
                    
                
                _sizeToRead = size;
                _taskCompletionSource = new TaskCompletionSource<ReadOnlyMemory<byte>>(token);
                return new ValueTask<ReadOnlyMemory<byte>>(_taskCompletionSource.Task);
            }
        }

        public ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] marker, CancellationToken token)
        {
            lock (_lockObject)
            {
                var size = _incomingPackages.GetSizeByMarker(marker);
             
                if (size < 0)
                {
                    _marker = marker;
                    _taskCompletionSource = new TaskCompletionSource<ReadOnlyMemory<byte>>(token);
                    return new ValueTask<ReadOnlyMemory<byte>>(_taskCompletionSource.Task);
                }
                
                Monitor.Enter(_readLock);
                var result = _incomingPackages.TryCompilePackage(size);
                return new ValueTask<ReadOnlyMemory<byte>>(result);

            }
        }

        public void CommitReadData(int size)
        {
            lock (_lockObject)
            {
                while (size>0)
                {
                    size = _incomingPackages[0].CommitReadData(size);

                    if (_incomingPackages.Count > 1)
                    {
                        if (_incomingPackages[0].ReadyToReadSize == 0)
                            _incomingPackages.RemoveAt(0);
                    }
                } 
                Monitor.Exit(_readLock);
            }
            
        }

        public void Stop()
        {

            lock (_lockObject)
            {
                _taskCompletionSource?.SetException(new Exception("DataReader is being stopped"));
            }

        }

        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append("Start:" + _incomingPackages[0].ReadyToReadStart);
            result.Append("Len:" + _incomingPackages[0].ReadyToReadSize);
            result.Append('[');
            foreach (var b in _incomingPackages.GetAll())
            {
                result.Append(b+",");
            }

            return result + "]";
        }


    }
}