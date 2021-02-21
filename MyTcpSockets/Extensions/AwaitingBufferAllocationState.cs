using System;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public class AwaitingBufferAllocationState
    {
        private TaskCompletionSource<Memory<byte>> _bufferAllocationTask;
        private TaskCompletionSource<(byte[] buffer, int start, int len)> _bufferAllocationTaskLegacy;
        
        public int AllocatedBufferSize { get; private set; }


        public Task<Memory<byte>> AllocateTask()
        {
            _bufferAllocationTask = new TaskCompletionSource<Memory<byte>>();
            return _bufferAllocationTask.Task;
        }
        
        public Task<(byte[] buffer, int start, int len)> AllocateLegacyTask()
        {
            _bufferAllocationTaskLegacy = new TaskCompletionSource<(byte[] buffer, int start, int len)>();
            return _bufferAllocationTaskLegacy.Task;
        }


        public void SetMemoryIsAllocated(byte[] buffer, int start, int len)
        {
            if (_bufferAllocationTask != null)
            {
                var result = _bufferAllocationTask;
                _bufferAllocationTask = null;
                AllocatedBufferSize = len;
                result.SetResult(new Memory<byte>(buffer, start, len));
            }
            
            if (_bufferAllocationTaskLegacy != null)
            {
                var result = _bufferAllocationTaskLegacy;
                _bufferAllocationTaskLegacy = null;
                AllocatedBufferSize = len;
                result.SetResult((buffer, start, len));
            }
        }

        public void SetMemoryIsAllocated(int len)
        {
            AllocatedBufferSize = len;
        }


        public void TryToAllocateBufferAgain(TcpDataPiece dataPiece)
        {
            if (_bufferAllocationTask != null)
            {
                var allocatedBuffer = dataPiece.AllocateBufferToWrite();
                var resultTask = _bufferAllocationTask;
                _bufferAllocationTask = null;
                AllocatedBufferSize = allocatedBuffer.Length;
                resultTask.SetResult(allocatedBuffer);
                return;
            }
            
            if (_bufferAllocationTaskLegacy != null)
            {
                var allocatedBuffer = dataPiece.AllocateBufferToWriteLegacy();
                var resultTask = _bufferAllocationTaskLegacy;
                _bufferAllocationTask = null;
                AllocatedBufferSize = allocatedBuffer.len;
                resultTask.SetResult(allocatedBuffer);
            }
            
        }
        
    }

}