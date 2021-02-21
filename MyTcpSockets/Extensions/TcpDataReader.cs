using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{

    public interface ITcpDataReader
    {
        /// <summary>
        /// Read bytes and commits it
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        ValueTask<byte> ReadAndCommitByteAsync(CancellationToken token);
        ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] marker, CancellationToken token);
        ValueTask<ReadOnlyMemory<byte>> ReadAsyncAsync(int size, CancellationToken token);
        void CommitReadData(ReadOnlyMemory<byte> data);
        void CommitReadDataSize(int size);
    }
    

    public class TcpDataReader : ITcpDataReader
    {
        public class AwaitingBuffer
        {
            public int SizeToRead { get; private set; }
            
            public byte[] CompilingBuffer { get; private set; }
            
            public TaskCompletionSource<Memory<byte>> WaitingForBufferBeingReadTask { get; private set; }


            public void Set(int sizeToRead, bool createCompilingBuffer)
            {
                SizeToRead = sizeToRead;
                CompilingBuffer = createCompilingBuffer ? new byte[sizeToRead] : null;
                WaitingForBufferBeingReadTask = new TaskCompletionSource<Memory<byte>>();
            }

            public void Reset()
            {
                SizeToRead = 0;
                CompilingBuffer = null;
                WaitingForBufferBeingReadTask = null;
            }
        }
        
        
        private readonly TcpDataPiece _readBuffer;

        private readonly object _lockObject = new object();
        private readonly object _readLock = new object();

        public TcpDataReader(int readBufferSize)
        {
            _readBuffer =  new TcpDataPiece(readBufferSize);
        }

        #region write

        private AwaitingBuffer _awaitingBuffer = new AwaitingBuffer();

        private int _allocatedBufferSize;
        
        public ValueTask<Memory<byte>> AllocateBufferToWriteAsync()
        {
            lock (_lockObject)
            {

                var result = _readBuffer.AllocateBufferToWrite();

                if (result.Length == 0)
                {
                    _waitingForBufferBeingReadTask = new TaskCompletionSource<Memory<byte>>();
                    return new ValueTask<Memory<byte>>(_waitingForBufferBeingReadTask.Task);
                }
  
                _allocatedBufferSize = result.Length;
                return new ValueTask<Memory<byte>>(result);
            }
        }


        private TaskCompletionSource<(byte[] buffer, int start, int len)> _waitingForBufferBeingReadLegacyTask;
        public ValueTask<(byte[] buffer, int start, int len)> AllocateBufferToWriteLegacyAsync()
        {
            lock (_lockObject)
            {
                var result = _readBuffer.AllocateBufferToWriteLegacy();

                if (result.len == 0)
                {
                    _waitingForBufferBeingReadLegacyTask =
                        new TaskCompletionSource<(byte[] buffer, int start, int len)>();

                    return new ValueTask<(byte[] buffer, int start, int len)>();
                }

                _allocatedBufferSize = result.len;
                return new ValueTask<(byte[] buffer, int start, int len)>(result);
            }
        }

        private void GcIfNeeded()
        {
            if (_readBuffer.ReadyToReadStart == 0)
                return;

            //We Wait until we finish reading
            Monitor.Enter(_readLock);
            try
            {
                _readBuffer.Gc();
            }
            finally
            {
                Monitor.Exit(_readLock);
            }
        }

        public void CommitWrittenData(int len)
        {

            if (len > _allocatedBufferSize)
                throw new Exception(
                    $"You are trying to commit grater mem size:{len} than you have allocated earlier: {_allocatedBufferSize} ");
         
            lock (_lockObject)
            {
                _readBuffer.CommitWrittenData(len);
                GcIfNeeded();
                
                if (_sizeToRead>0)
                    TryToPushSizedRead();
                
                if (_marker != null)
                    TryToPushMarkerRead();

                if (_taskCompletionSourceByte != null)
                    TryToCommitByte();
            }

        }
        #endregion



        private void CheckIfWriteBufferHasToBePushed()
        {
            
        }
        
        
        
        private void TryToCommitByte()
        {
            if (_readBuffer.ReadyToReadSize == 0)
                return;
            var taskResult = _taskCompletionSourceByte;
            _taskCompletionSourceByte = null;
            var result = CommitByte();
            taskResult.SetResult(result);
        }


        private void CompleteReadingBufferTask(ReadOnlyMemory<byte> result)
        {
            Monitor.Enter(_readLock);
            _sizeToRead = -1;

            var taskResult = _taskCompletionSource;
            _taskCompletionSource = null;
            taskResult.SetResult(result);
        }

        private void TryToPushSizedRead()
        {
            if (_taskCompletionSource == null)
                return;

            if (_sizeToRead < _readBuffer.BufferSize)
            {
                if (_readBuffer.ReadyToReadSize < _sizeToRead)
                    return;
                
                var result = _readBuffer.Read(_sizeToRead);
                CompleteReadingBufferTask(result);
                return;
            }

            

        }

        private void TryToPushMarkerRead()
        {
            if (_taskCompletionSource != null)
            {
                var size = _readBuffer.GetSizeByMarker(_marker);
                if (size < 0)
                    return;
                Monitor.Enter(_readLock);

                var result = _readBuffer.TryCompilePackage(size);
                _marker = null;
                var resultTask = _taskCompletionSource;
                _taskCompletionSource = null;
                resultTask.SetResult(result);
            }
        }


        private int _sizeToRead = -1;
        private byte[] _bufferToCompile;

        private byte[] _marker;
        private TaskCompletionSource<ReadOnlyMemory<byte>> _taskCompletionSource;
        private TaskCompletionSource<byte> _taskCompletionSourceByte;
        
        
        public ValueTask<byte> ReadAndCommitByteAsync(CancellationToken token)
        {
            lock (_lockObject)
            {

                if (_readBuffer.ReadyToReadSize > 0)
                {
                    var result = CommitByte();
                    return new ValueTask<byte>(result); 
                }
                
                _taskCompletionSourceByte = new TaskCompletionSource<byte>(token);
                return new ValueTask<byte>(_taskCompletionSourceByte.Task);
            }
        }

        private ReadOnlyMemory<byte> ReadAndLockIfWeHaveData(int neededSize)
        {
            var result = _readBuffer.TryRead(neededSize);
            if (result.Length > 0)
                Monitor.Enter(_readLock);
            return result;
            
        }

        public ValueTask<ReadOnlyMemory<byte>> ReadAsyncAsync(int size, CancellationToken token)
        {
            lock (_lockObject)
            {

                var result = ReadAndLockIfWeHaveData(size);

                if (result.Length > 0)
                    return new ValueTask<ReadOnlyMemory<byte>>(result);
                
                _sizeToRead = size;
                if (_sizeToRead > _allocatedBufferSize)
                    _bufferToCompile = new byte[_sizeToRead];
                
                _taskCompletionSource = new TaskCompletionSource<ReadOnlyMemory<byte>>(token);
                return new ValueTask<ReadOnlyMemory<byte>>(_taskCompletionSource.Task);
            }
        }

        public ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] marker, CancellationToken token)
        {
            lock (_lockObject)
            {
                var size = _readBuffer.GetSizeByMarker(marker);
                
                if (size < 0)
                {
                    _taskCompletionSource = new TaskCompletionSource<ReadOnlyMemory<byte>>(token);
                    _marker = marker;
                    return new ValueTask<ReadOnlyMemory<byte>>(_taskCompletionSource.Task);
                }
                
                Monitor.Enter(_readLock);
                
                var result = _readBuffer.TryCompilePackage(size);
                return new ValueTask<ReadOnlyMemory<byte>>(result);

            }
        }
        

        void ITcpDataReader.CommitReadData(ReadOnlyMemory<byte> data)
        {
            CommitReadDataSize(data.Length);
        }

        private byte CommitByte()
        {
            var result = _readBuffer.ReadByte();
            _readBuffer.CommitReadData(1);
            return result;
        }

        public void CommitReadDataSize(int size)
        {
            lock (_lockObject)
            {
                while (size>0)
                {
                    size = _readBuffer.CommitReadData(size);

                    if (_readBuffer.Count > 1)
                    {
                        if (_readBuffer.ReadyToReadSize == 0)
                            _readBuffer.RemoveAt(0);
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
            result.Append("Start:" + _readBuffer.ReadyToReadStart);
            result.Append("Len:" + _readBuffer.ReadyToReadSize);
            result.Append('[');
            result.Append(_readBuffer);
            return result + "]";
        }


    }
}