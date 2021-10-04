using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;

namespace MyTcpSockets.Tests.Mocks
{
    public class IncomingTrafficReaderMock : IIncomingTcpTrafficReader
    {
        public readonly Queue<byte> IncomingTraffic = new ();

        public ValueTask<byte> ReadByteAsync(CancellationToken token)
        {
            var result = IncomingTraffic.Dequeue();
            return new ValueTask<byte>(result);
        }

        public ValueTask<int> ReadBytesAsync(Memory<byte> buffer, CancellationToken token)
        {
            var span = buffer.Span;

            for (var i = 0; i < buffer.Length; i++)
            {
                if (IncomingTraffic.Count == 0)
                    return new ValueTask<int>(i);
                var result = IncomingTraffic.Dequeue();
                span[i] = result;
            }

 
            return new ValueTask<int>(buffer.Length);
        }
    }
}