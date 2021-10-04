using System;

namespace MyTcpSockets.Extensions
{
    public class TcpDataReaderAsSequence
    {

        private readonly byte[] _buffer;
        
        public int PositionStart { get; private set; }
        
        public int PositionEnd { get; private set; }

        public TcpDataReaderAsSequence(byte[] buffer)
        {
            _buffer = buffer;
        }

        private static bool SequencesAreSame(ReadOnlyMemory<byte> src, ReadOnlyMemory<byte> dest)
        {
            if (src.Length != dest.Length)
            {
                return false;
            }

            var srcSpan = src.Span;
            var destSpan = dest.Span;

            for (var i = 0; i < src.Length; i++)
            {
                if (srcSpan[i] != destSpan[i])
                {
                    return false;
                }
            }

            return true;

        }

        private int FindSequence(byte[] sequence)
        {
            for (var i = PositionStart; i <= PositionEnd - sequence.Length; i++)
            {
                if (_buffer[i] == sequence[0])
                {
                    if (SequencesAreSame(new ReadOnlyMemory<byte>(_buffer, i, sequence.Length), sequence))
                        return i + sequence.Length;
                }
            }

            return -1;
        }


        public void CompactBuffer()
        {

            if (PositionStart == PositionEnd)
            {
                PositionStart = 0;
                PositionEnd = 0;
                return;
            }
            
            var newSize = PositionEnd - PositionStart;
            var src = new Memory<byte>(_buffer, PositionStart, newSize);
            var dest = new Memory<byte>(_buffer, 0, newSize);
            src.CopyTo(dest);

            PositionEnd -= PositionStart;
            PositionStart = 0;
        }


        public Memory<byte> GetNextMemoryToWrite()
        {
            return _buffer.AsMemory(PositionEnd, _buffer.Length - PositionEnd);
        }

        public void AppendWritten(int written)
        {
            PositionEnd += written;
        }

        public ReadOnlyMemory<byte> GetNextPackageIfExists(byte[] sequence)
        {
            var nextIndex = FindSequence(sequence);

            if (nextIndex < 0)
                return default;

            var result = new ReadOnlyMemory<byte>(_buffer, PositionStart, nextIndex-PositionStart);
            PositionStart = nextIndex;
            return result;
        }

    }
}