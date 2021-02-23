using System;
using System.IO;
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
        ValueTask<TcpReadResult> ReadAsyncAsync(int size, CancellationToken token);
        void CommitReadData(TcpReadResult tcpReadResult);
        ValueTask<TcpReadResult> ReadWhileWeGetSequenceAsync(byte[] marker, CancellationToken token);
    }


    public class TcpDataReader : ITcpDataReader
    {
        
        private readonly TcpDataPiece _readBuffer;

        private readonly ReadWriteSwitcher _readWriteSwitcher = new ReadWriteSwitcher();

        public TcpDataReader(int readBufferSize)
        {
            _readBuffer =  new TcpDataPiece(readBufferSize);
        }

        #region write
        
        public async ValueTask<Memory<byte>> AllocateBufferToWriteAsync(CancellationToken token)
        {
            while (true)
            {
                await _readWriteSwitcher.WaitUntilWriteModeIsSetAsync(token);
                var result =  _readBuffer.AllocateBufferToWrite();
                if (result.Length > 0)
                    return result;
                _readWriteSwitcher.SetToReadMode();
            }

        }

        public async ValueTask<(byte[] buffer, int start, int len)> AllocateBufferToWriteLegacyAsync(CancellationToken token)
        {
            while (true)
            {
                await _readWriteSwitcher.WaitUntilWriteModeIsSetAsync(token);
                var result = _readBuffer.AllocateBufferToWriteLegacy();
                if (result.len > 0)
                    return result;
                _readWriteSwitcher.SetToReadMode();
            }

        }

        public void CommitWrittenData(int len)
        {
            _readBuffer.CommitWrittenData(len);
            _readWriteSwitcher.SetToReadMode();
        }

        #endregion

        #region read
        public async ValueTask<byte> ReadAndCommitByteAsync(CancellationToken token)
        {
            await _readWriteSwitcher.WaitUntilReadModeIsSetAsync(token);

            while (true)
            { 
                if (_readBuffer.ReadyToReadSize > 0)
                {
                    var result = _readBuffer.ReadByte();
                    _readBuffer.CommitReadData(1);
                    return result;
                }
                _readWriteSwitcher.SetToWriteMode();
                await _readWriteSwitcher.WaitUntilReadModeIsSetAsync(token);
            }

        }

        private async ValueTask<ReadOnlyMemory<byte>> ReadAsInternalBufferAsync(int size, CancellationToken token)
        {
            while (true)
            {
                var result = _readBuffer.TryRead(size);
                if (result.Length > 0)
                    return result;
                
                _readWriteSwitcher.SetToWriteMode();
                await _readWriteSwitcher.WaitUntilReadModeIsSetAsync(token);
            }
            
        }
        
        private async ValueTask<byte[]> ReadAsNewByteArrayBufferAsync(int size, CancellationToken token)
        {
            var result = new byte[size];
            var remainSize = size;
            var pos = 0;
            while (remainSize>0)
            {
                var buffer = _readBuffer.GetWhateverWeHave();
                if (buffer.Length == 0)
                {
                    _readWriteSwitcher.SetToWriteMode();
                    await _readWriteSwitcher.WaitUntilReadModeIsSetAsync(token);
                    buffer = _readBuffer.GetWhateverWeHave();
                }

                var copySize = buffer.Length > remainSize ? remainSize : buffer.Length;
                
                buffer.Slice(0, copySize).CopyTo(result.AsMemory(pos, copySize));
                _readBuffer.CommitReadData(copySize);

                pos += copySize;
                remainSize -= copySize;
            }

            return result;
        }

        public async ValueTask<TcpReadResult> ReadAsyncAsync(int size, CancellationToken token)
        {
            await _readWriteSwitcher.WaitUntilReadModeIsSetAsync(token);

            if (size > _readBuffer.BufferSize)
            {
                var resultAsArray = await ReadAsNewByteArrayBufferAsync(size, token);
                return new TcpReadResult(default, resultAsArray);
            }

            var result = await ReadAsInternalBufferAsync(size, token);
            return  new TcpReadResult(result, null);
        }


        public void CommitReadData(TcpReadResult tcpReadResult)
        {
            if (tcpReadResult.UncommittedMemory.Length > 0)
                _readBuffer.CommitReadData(tcpReadResult.UncommittedMemory.Length);
        }


        private async ValueTask<ReadOnlyMemory<byte>> TryReadFromTheBufferAsync(byte[] marker, CancellationToken token)
        {

            while (!_readBuffer.FullOfData)
            {
                var size = _readBuffer.GetSizeByMarker(marker);

                if (size > -1)
                    return _readBuffer.TryRead(size);

                _readWriteSwitcher.SetToWriteMode();
                await _readWriteSwitcher.WaitUntilReadModeIsSetAsync(token);
            }

            return default;
        }


        private async ValueTask<byte[]> TryReadAsArrayAsync(byte[] marker, CancellationToken token)
        {
            var result = new MemoryStream();

            var chunk = _readBuffer.GetWhateverWeHave();
            if (chunk.Length > 0)
            {
                result.Write(chunk.Span);
                _readBuffer.CommitReadData(chunk.Length);
            }

            while (true)
            {
                _readWriteSwitcher.SetToWriteMode();
                await _readWriteSwitcher.WaitUntilReadModeIsSetAsync(token);

                var size = result.GetSizeByMarker(_readBuffer, marker);

                if (size == -1)
                {
                    var newChunk = _readBuffer.GetWhateverWeHave();
                    result.Write(newChunk.Span);
                    _readBuffer.CommitReadData(newChunk.Length);
                    continue;
                }

                var remainSize = size - (int) result.Length;
                var lastChunk = _readBuffer.TryRead(remainSize);
                result.Write(lastChunk.Span);
                _readBuffer.CommitReadData(lastChunk.Length);
                return result.ToArray();
            }

        }


        public async ValueTask<TcpReadResult> ReadWhileWeGetSequenceAsync(byte[] marker, CancellationToken token)
        {
            await _readWriteSwitcher.WaitUntilReadModeIsSetAsync(token);
            

            var resultAsMem = await TryReadFromTheBufferAsync(marker, token);

            if (resultAsMem.Length > 0)
                return new TcpReadResult(resultAsMem, null);

            var resultAsArray = await TryReadAsArrayAsync(marker, token);
            return new TcpReadResult(default, resultAsArray);

        }

        #endregion

        public void Stop()
        {
            _readWriteSwitcher.Stop();
        }

        public override string ToString()
        {
            return "Start:" + _readBuffer.ReadyToReadStart + "Len:" + _readBuffer.ReadyToReadSize;
        }


    }
}