using System;
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
        ValueTask<ReadOnlyMemory<byte>> ReadAsyncAsync(int size, CancellationToken token);
        void CommitReadDataSize(int size);
        
        //   ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] marker, CancellationToken token);

    }


    public class TcpDataReader : ITcpDataReader
    {
        
        private readonly TcpDataPiece _readBuffer;

        private readonly object _lockObject = new object();

        public TcpDataReader(int readBufferSize)
        {
            _readBuffer =  new TcpDataPiece(readBufferSize);
        }

        #region write

        private readonly AwaitingReadBuffer _awaitingReadBuffer = new AwaitingReadBuffer();

        private readonly AwaitingBufferAllocationState _awaitingBufferAllocationState = new AwaitingBufferAllocationState();


        public ValueTask<Memory<byte>> AllocateBufferToWriteAsync()
        {
            lock (_lockObject)
            {
                var result = _readBuffer.AllocateBufferToWrite();

                if (result.Length == 0)
                {
                    var task = _awaitingBufferAllocationState.AllocateTask();
                    return new ValueTask<Memory<byte>>(task);
                }
  
                _awaitingBufferAllocationState.SetMemoryIsAllocated(result.Length);
                return new ValueTask<Memory<byte>>(result);
            }
        }

        public ValueTask<(byte[] buffer, int start, int len)> AllocateBufferToWriteLegacyAsync()
        {
            lock (_lockObject)
            {
                var result = _readBuffer.AllocateBufferToWriteLegacy();

                if (result.len == 0)
                {
                    var task = _awaitingBufferAllocationState.AllocateLegacyTask();
                    return new ValueTask<(byte[] buffer, int start, int len)>(task);
                }

                _awaitingBufferAllocationState.SetMemoryIsAllocated(result.len);
                return new ValueTask<(byte[] buffer, int start, int len)>(result);
            }
        }

        public void CommitWrittenData(int len)
        {

            if (len > _awaitingBufferAllocationState.AllocatedBufferSize)
                throw new Exception(
                    $"You are trying to commit grater mem size:{len} than you have allocated earlier: {_awaitingBufferAllocationState.AllocatedBufferSize} ");
         
            lock (_lockObject)
            {
                _readBuffer.CommitWrittenData(len);
                _readBuffer.Gc();

                _awaitingReadBuffer.NewBytesAppeared(_readBuffer);
            }

        }
        #endregion
  
        
        public ValueTask<byte> ReadAndCommitByteAsync(CancellationToken token)
        {
            lock (_lockObject)
            {

                if (_readBuffer.ReadyToReadSize > 0)
                {
                    var result = CommitByte();
                    return new ValueTask<byte>(result); 
                }

                return _awaitingReadBuffer.EngageToReadByte();
            }
        }

        public ValueTask<ReadOnlyMemory<byte>> ReadAsyncAsync(int size, CancellationToken token)
        {
            lock (_lockObject)
            {
                var result = _readBuffer.TryRead(size);

                if (result.Length > 0)
                    return new ValueTask<ReadOnlyMemory<byte>>(result);

                var compileBuffer = size > _readBuffer.BufferSize;
                return _awaitingReadBuffer.EngageToRead(size, compileBuffer);
            }
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
               _readBuffer.CommitReadData(size);
            }
        }

        public void Stop()
        {

            lock (_lockObject)
            {
               //ToDo - Implement - how to stop
            }

        }

        public override string ToString()
        {
            lock (_lockObject)
            {
                return "Start:" + _readBuffer.ReadyToReadStart + "Len:" + _readBuffer.ReadyToReadSize + "[" +
                       _readBuffer + "]";
            }
        }


    }
}