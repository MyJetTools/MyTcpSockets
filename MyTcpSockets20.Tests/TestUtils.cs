using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public static class TestUtils
    {
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(this IEnumerable<T> mem)
        {
            return new ReadOnlyMemory<T>(mem.ToArray());
        }
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(this IEnumerable<T> mem, int from, int size)
        {
            return new ReadOnlyMemory<T>(mem.Skip(from).Take(size).ToArray());
        }
        
        public static void ArraysIsEqualWith(this ReadOnlyMemory<byte> from, ReadOnlyMemory<byte> to)
        {
            var fromSpan = from.Span;
            var toSpan = to.Span;
            Assert.IsTrue(from.Length == to.Length);

            for (var i = 0; i < fromSpan.Length; i++)
            {
                Assert.AreEqual(fromSpan[i], toSpan[i]);
            }
        }


        public static async Task NewPackageAsync(this TcpDataReader tcpDataReader, byte[] data)
        {
            var remainSize = data.Length;
            var pos = 0;

            while (remainSize>0)
            {
                var buf = await tcpDataReader.AllocateBufferToWriteAsync();

                var copySize = buf.Length < remainSize ? buf.Length : remainSize;
                
                new ReadOnlyMemory<byte>(data, pos, copySize).CopyTo(buf);
                tcpDataReader.CommitWrittenData(copySize);
                pos += copySize;
                remainSize -= copySize;
            }
        }
    }
}