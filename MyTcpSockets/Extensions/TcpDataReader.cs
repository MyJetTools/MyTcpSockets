using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{

    public interface ITcpDataReader
    {
        ValueTask<byte> ReadByteAsync(CancellationToken token);
        ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] marker, CancellationToken token);
        ValueTask<ReadOnlyMemory<byte>> ReadAsyncAsync(int size, CancellationToken token);
        void CommitReadData(byte b);
        void CommitReadData(ReadOnlyMemory<byte> data);
        void CommitReadDataSize(int size);
    }
    

    public class TcpDataReader : ITcpDataReader
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
                var lastOne = _incomingPackages[_incomingPackages.Count - 1];
                lastOne.CommitWrittenData(len);
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
            var taskResult = _taskCompletionSourceByte;
            _taskCompletionSourceByte = null;
            taskResult.SetResult(result);

        }

        private void TryToPushSizedRead()
        {
            if (_taskCompletionSource != null)
            {
                var sizedResult = _incomingPackages.TryCompilePackage(_sizeToRead);
                if (sizedResult.Length == 0)
                    return;

                Monitor.Enter(_readLock);
                _sizeToRead = -1;

                var taskResult = _taskCompletionSource;
                _taskCompletionSource = null;
                taskResult.SetResult(sizedResult);
            }
        }
        
        private void TryToPushMarkerRead()
        {
            if (_taskCompletionSource != null)
            {
                var size = _incomingPackages.GetSizeByMarker(_marker);
                if (size < 0)
                    return;
                Monitor.Enter(_readLock);

                var result = _incomingPackages.TryCompilePackage(size);
                _marker = null;
                var resultTask = _taskCompletionSource;
                _taskCompletionSource = null;
                resultTask.SetResult(result);
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
                    _taskCompletionSource = new TaskCompletionSource<ReadOnlyMemory<byte>>(token);
                    _marker = marker;
                    return new ValueTask<ReadOnlyMemory<byte>>(_taskCompletionSource.Task);
                }
                
                Monitor.Enter(_readLock);
                
                var result = _incomingPackages.TryCompilePackage(size);
                return new ValueTask<ReadOnlyMemory<byte>>(result);

            }
        }

        void ITcpDataReader.CommitReadData(byte b)
        {
            CommitReadDataSize(1);
        }

        void ITcpDataReader.CommitReadData(ReadOnlyMemory<byte> data)
        {
            CommitReadDataSize(data.Length);
        }

        public void CommitReadDataSize(int size)
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