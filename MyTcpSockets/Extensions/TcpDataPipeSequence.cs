using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{

    public class TcpDataPipeSequence : ITcpDataPipe
    {
        private readonly List<TcpDataPiece> _incomingPackages = new List<TcpDataPiece>();

        private TaskCompletionSource<ReadOnlyMemory<byte>> _sequenceTask;
        private byte[] _sequence;
        
        public void PushData(TcpDataPiece tcpDataPiece)
        {
            _incomingPackages.Add(tcpDataPiece);
            
            if (_sequenceTask == null)
                return;

            var dataToSend = SearchSequence(_sequence);

            if (dataToSend.Length == 0)
                return;

            var seqTask = _sequenceTask;
            _sequenceTask = null;
            seqTask.SetResult(dataToSend);
        }


        public int UncommittedSize { get; private set; }
        public int ListIndex { get; private set; }
        public int ItemIndex { get; private set; }


        public (bool hasElement, byte el) GetNextElement()
        {
            if (ListIndex >= _incomingPackages.Count)
                return (false, 0);

            try
            {
                UncommittedSize++;
                return (true, _incomingPackages[ListIndex].Data[ItemIndex++]);
            }
            finally
            {
                if (ItemIndex >= _incomingPackages[ListIndex].Data.Length)
                {
                    ItemIndex = 0;
                    ListIndex++;
                }
            }
        }

        public ReadOnlyMemory<byte> GetUncommittedSequence()
        {
            var result = _incomingPackages.GetAndClean(UncommittedSize);
            ListIndex = 0;
            ItemIndex = _incomingPackages.Count > 0 ? _incomingPackages[0].StartIndex : 0;
            UncommittedSize = 0;
            return result;
        }


        private int _foundIndex;
        private ReadOnlyMemory<byte> SearchSequence(byte[] sequence)
        {

            if (_incomingPackages.Count == 0)
                return Array.Empty<byte>();

            var next = GetNextElement();

            while (next.hasElement)
            {

                if (next.el == sequence[_foundIndex])
                {
                    _foundIndex++;

                    if (_foundIndex >= sequence.Length)
                    {
                        _foundIndex = 0;
                        return GetUncommittedSequence();
                    }
                }
                else
                    _foundIndex = 0;
                
                next = GetNextElement();
                
                
            }
            
            return Array.Empty<byte>();

        }

        
        
        public ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] sequence, CancellationToken token)
        {
            _sequence = sequence;

            var seq = SearchSequence(sequence);

            if (seq.Length > 0 )
                return new ValueTask<ReadOnlyMemory<byte>>(seq);

            _sequenceTask = new TaskCompletionSource<ReadOnlyMemory<byte>>(token);
            _sequence = sequence;
            return new ValueTask<ReadOnlyMemory<byte>>(_sequenceTask.Task);
        }

        public async Task StopAsync()
        {
            while (_sequenceTask == null)
                await Task.Delay(100);

            _sequenceTask?.SetException(new Exception("Disconnect"));
        }
    }
}