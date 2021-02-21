using System;
using System.IO;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public class AwaitingReadBuffer
    {
        public int SizeToRead { get; private set; }
            

        private TaskCompletionSource<TcpReadResult> _waitingForBufferBeingReadTask;
        
        private TaskCompletionSource<byte> _taskCompletionSourceByte;
        
        private MemoryStream _memoryStream;

        public ValueTask<TcpReadResult> EngageToRead(ReadOnlyMemory<byte> initMemoryStream, bool sizeIsHuge, int sizeToRead)
        {
            SizeToRead = sizeToRead;

            if (sizeIsHuge)
            {
                _memoryStream = new MemoryStream();
                if (initMemoryStream.Length >0)
                    _memoryStream.Write(initMemoryStream.Span);
            }
                
            _waitingForBufferBeingReadTask = new TaskCompletionSource<TcpReadResult>();
            return new ValueTask<TcpReadResult>(_waitingForBufferBeingReadTask.Task);
        }

        public ValueTask<byte> EngageToReadByte()
        {
            _taskCompletionSourceByte = new TaskCompletionSource<byte>();
            return new ValueTask<byte>(_taskCompletionSourceByte.Task);
        }
        



        public void NewBytesAppeared(TcpDataPiece readyBuffer)
        {
            if (_taskCompletionSourceByte != null)
            {
                var byteResult = readyBuffer.ReadByte();
                readyBuffer.CommitReadData(1);
                SetByteResult(byteResult);
                return;
            }

            if (_waitingForBufferBeingReadTask != null)
            {
                if (_memoryStream == null)
                    ReadPackageIsSmallerThenBuffer(readyBuffer);
                else
                    ReadPackageIsBiggerThenBuffer(readyBuffer);
                return;
            }

            if (_searchByMarkerTask != null)
            {
                ReadSequence(readyBuffer);
            }
        }
        
        private void SetByteResult(byte result)
        {
            var task = _taskCompletionSourceByte;
            _taskCompletionSourceByte = null;
            task.SetResult(result);
        }

        private void ReadPackageIsSmallerThenBuffer(TcpDataPiece readyBuffer)
        {
            var result = readyBuffer.TryRead(SizeToRead);
            if (result.Length == 0)
                return;

            var task = _waitingForBufferBeingReadTask;
            _waitingForBufferBeingReadTask = null;
            SizeToRead = -1;
            task.SetResult(new TcpReadResult(result, null));
        }
        
        private void ReadPackageIsBiggerThenBuffer(TcpDataPiece readyBuffer)
        {
            var remainsToRead = SizeToRead - (int)_memoryStream.Length;
            var readSize = remainsToRead > readyBuffer.ReadyToReadSize ? readyBuffer.ReadyToReadSize : remainsToRead;
            var memToWrite = readyBuffer.TryRead(readSize);
            _memoryStream.Write(memToWrite.Span);
            readyBuffer.CommitReadData(readSize);

            if (_memoryStream.Length == SizeToRead)
            {
                var task = _waitingForBufferBeingReadTask;
                _waitingForBufferBeingReadTask = null;
                var result = _memoryStream.ToArray();
                task.SetResult(new TcpReadResult(default, result));
            }
        }

        private void ReadSequence(TcpDataPiece readBuffer)
        {
            if (_memoryStream != null)
            {
                ReadSequenceWithStream(readBuffer);
                return;
            }
            
            var size = readBuffer.GetSizeByMarker(_marker);
            if (size == -1)
                return;

            var resultTask = _searchByMarkerTask;
            _searchByMarkerTask = null;
            resultTask.SetResult(new TcpReadResult(readBuffer.TryRead(size), null));
        }

        private void ReadSequenceWithStream(TcpDataPiece readBuffer)
        {
            var size = _memoryStream.GetSizeByMarker(readBuffer, _marker);
            
            if (size == -1)
                return;

            var lastPieceSize = (int)(size - _memoryStream.Length);
            _memoryStream.Write(readBuffer.TryRead(lastPieceSize).Span);
            readBuffer.CommitReadData(lastPieceSize);
            
            var resultTask = _searchByMarkerTask;
            _searchByMarkerTask = null;
            var result = _memoryStream.ToArray();
            resultTask.SetResult(new TcpReadResult(default, result));
        }


        #region ByMarker
  
        private byte[] _marker;

        private TaskCompletionSource<TcpReadResult> _searchByMarkerTask;

        public ValueTask<TcpReadResult> EngageSearchByMarker(ReadOnlyMemory<byte> initMemoryStream, bool createMem, byte[] marker)
        {
            _marker = marker;

            if (createMem)
            {
                _memoryStream = new MemoryStream();
                if (initMemoryStream.Length>0)
                    _memoryStream.Write(initMemoryStream.Span);
            }
            
            _searchByMarkerTask = new TaskCompletionSource<TcpReadResult>();
            return new ValueTask<TcpReadResult>(_searchByMarkerTask.Task);
        }



        #endregion
    }

}