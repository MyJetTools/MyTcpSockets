using System;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    
    public enum ReadMode
    {
        None, Sizes, Sequence
    }

    public class TcpDataReader
    {
        public readonly TcpDataPipe TcpDataPipe = new TcpDataPipe();

        private ReadMode _readMode = ReadMode.None;

        public void NewPackage(byte[] data)
        {
            TcpDataPipe.Add(data);
            PushData();
        }

        public void NewPackage(byte[] data, int len)
        {
            TcpDataPipe.Add(data, len);
            PushData();
        }

        private void PushData()
        {
            if (_readMode == ReadMode.Sizes)
                PushInSizeMode();

            if (_readMode == ReadMode.Sequence)
                PushInSequenceMode();
        }

        #region Reading as Sizes


        private void PushInSizeMode()
        {
            if (_activeTask == null)
                return;

            if (_awaitingSize <= TcpDataPipe.RemainsSize)
            {
                TcpDataPipe.AdvancePosition(_awaitingSize);
                var result = TcpDataPipe.GetUncommittedAmountAndCommit();
                var activeTask = _activeTask;
                _activeTask = null;
                activeTask.SetResult(result);
            }

        }

        private int _awaitingSize;
        private TaskCompletionSource<ReadOnlyMemory<byte>> _activeTask;

        public ValueTask<ReadOnlyMemory<byte>> ReadAsyncAsync(int size)
        {
            if (_readMode == ReadMode.Sequence)
                throw new Exception("Reader is already is Sequence Mode. Can not switch to Sizes Mode");
            
            _readMode = ReadMode.Sizes;

            if (size <= TcpDataPipe.RemainsSize)
            {
                TcpDataPipe.AdvancePosition(size);
                var result = TcpDataPipe.GetUncommittedAmountAndCommit();
                return new ValueTask<ReadOnlyMemory<byte>>(result);
            }

            _activeTask = new TaskCompletionSource<ReadOnlyMemory<byte>>();
            _awaitingSize = size;
            return new ValueTask<ReadOnlyMemory<byte>>(_activeTask.Task);
        }

        #endregion Reading as Sizes

        #region Reading as a Sequence


        private TaskCompletionSource<ReadOnlyMemory<byte>> _sequenceTask;
        private byte[] _sequence;


        private void PushInSequenceMode()
        {
            var found = SearchSequence(_sequence);

            if (!found)
                return;

            var result = TcpDataPipe.GetUncommittedAmountAndCommit();
            var seqTask = _sequenceTask;
            _sequenceTask = null;
            seqTask.SetResult(result);

        }

        private int _searchSeqIndex;


        private bool SearchSequence(byte[] sequence)
        {
            var positionAdvanced = TcpDataPipe.AdvancePosition();

            while (positionAdvanced)
            {

                if (sequence[_searchSeqIndex] == TcpDataPipe.CurrentElement)
                {
                    _searchSeqIndex++;

                    if (_searchSeqIndex >= sequence.Length)
                    {
                        _searchSeqIndex = 0;
                        return true;
                    }
                }
                else
                    _searchSeqIndex = 0;

                positionAdvanced = TcpDataPipe.AdvancePosition();
            }

            return false;
        }

        public ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] sequence)
        {

            if (_readMode == ReadMode.Sizes)
                throw new Exception("Reader is already is Sizes Mode. Can not switch to Sequence Mode");

            _readMode = ReadMode.Sequence;

            var found = SearchSequence(sequence);

            if (found)
            {

                var result = TcpDataPipe.GetUncommittedAmountAndCommit();
                return new ValueTask<ReadOnlyMemory<byte>>(result);
            }

            _sequenceTask = new TaskCompletionSource<ReadOnlyMemory<byte>>();
            _sequence = sequence;
            return new ValueTask<ReadOnlyMemory<byte>>(_sequenceTask.Task);
        }

        #endregion Reading as a Sequence

        public async Task StopAsync()
        {
            while (_activeTask == null && _sequenceTask == null)
                await Task.Delay(100);

            _activeTask?.SetException(new Exception("Disconnect"));
            _sequenceTask?.SetException(new Exception("Disconnect"));
        }
    }
}