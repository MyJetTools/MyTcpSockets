using System;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public class AwaitingReadBuffer
    {
        public int SizeToRead { get; private set; }
            
        public byte[] CompilingBuffer { get; private set; }

        private TaskCompletionSource<ReadOnlyMemory<byte>> _waitingForBufferBeingReadTask;
        
        private TaskCompletionSource<byte> _taskCompletionSourceByte;

        public ValueTask<ReadOnlyMemory<byte>> EngageToRead(int sizeToRead, bool createCompilingBuffer)
        {
            SizeToRead = sizeToRead;
            CompilingBuffer = createCompilingBuffer ? new byte[sizeToRead] : null;
            _waitingForBufferBeingReadTask = new TaskCompletionSource<ReadOnlyMemory<byte>>();
            return new ValueTask<ReadOnlyMemory<byte>>(_waitingForBufferBeingReadTask.Task);
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
                if (CompilingBuffer == null)
                    ReadPackageIsSmallerThenBuffer(readyBuffer);
                else
                    ReadPackageIsBiggerThenBuffer(readyBuffer);
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
            task.SetResult(result);
        }
        
        private void ReadPackageIsBiggerThenBuffer(TcpDataPiece readyBuffer)
        {
            throw new NotImplementedException("Implement me");
        }
    }

}