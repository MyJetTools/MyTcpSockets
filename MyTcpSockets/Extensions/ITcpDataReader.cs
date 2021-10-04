using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public interface ITcpDataReader
    {
        public ValueTask<byte> ReadByteAsync(CancellationToken token);

        public ValueTask<ReadOnlyMemory<byte>> ReadBytesAsync(int size, CancellationToken token);

        ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] marker, CancellationToken token);
    }
}