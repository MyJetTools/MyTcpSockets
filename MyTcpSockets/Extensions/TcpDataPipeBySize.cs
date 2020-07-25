using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public class TcpDataPipeBySize : ITcpDataPipe
    {
        private readonly List<TcpDataPiece> _incomingPackages = new List<TcpDataPiece>();

        private int _collectedSize;


        private int _awaitingSize;
        private TaskCompletionSource<ReadOnlyMemory<byte>> _activeTask;

        public void PushData(TcpDataPiece tcpDataPiece)
        {
            _collectedSize += tcpDataPiece.Count;
            _incomingPackages.Add(tcpDataPiece);
            
            if (_activeTask == null)
                return;


            if (_awaitingSize > _collectedSize) 
                return;
            
            var activeTask = _activeTask;
            _activeTask = null;
            
            activeTask.SetResult(CompileResult(_awaitingSize));
        }

        public async Task StopAsync()
        {
            while (_activeTask == null)
                await Task.Delay(100);

            _activeTask?.SetException(new Exception("Disconnect"));
        }

        private ReadOnlyMemory<byte> CompileResult(int size)
        {
            var result = _incomingPackages.GetAndClean(size);
            _collectedSize -= result.Length;
            return result;
        }
        
        public ValueTask<ReadOnlyMemory<byte>> ReadAsyncAsync(int size, CancellationToken token)
        {

            if (size <= _collectedSize)
                return new ValueTask<ReadOnlyMemory<byte>>(CompileResult(size));

            _activeTask = new TaskCompletionSource<ReadOnlyMemory<byte>>(token);
            _awaitingSize = size;
            return new ValueTask<ReadOnlyMemory<byte>>(_activeTask.Task);
        }

        
    }
}