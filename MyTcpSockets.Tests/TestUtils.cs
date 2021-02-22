using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        
        public static void ArraysAreEqual(this byte[] from, ReadOnlyMemory<byte> to)
        {
            var toSpan = to.Span;
            Assert.IsTrue(from.Length == to.Length);

            for (var i = 0; i < from.Length; i++)
            {
                Assert.AreEqual(from[i], toSpan[i]);
            }
        }
        
        public static void PopulateArray(this Memory<byte>dest,  byte[] src)
        {
            for (var i = 0; i < src.Length; i++)
            {
                dest.Span[i] = src[i];
            }
        }

        public static void PrepareTest()
        {
            SocketMemoryUtils.AllocateByteArray = size => GC.AllocateUninitializedArray<byte>(size);
        }

        public static async Task NewPackageAsync(this TcpDataReader tcpDataReader, byte[] data)
        {

            var remainSize = data.Length;
            var pos = 0;

            while (remainSize>0)
            {
                var cancellationToken = new CancellationTokenSource();

                var buf = await tcpDataReader.AllocateBufferToWriteAsync(cancellationToken.Token);

                var copySize = buf.Length < remainSize ? buf.Length : remainSize;
                
                new ReadOnlyMemory<byte>(data, pos, copySize).CopyTo(buf);
                tcpDataReader.CommitWrittenData(copySize);
                pos += copySize;
                remainSize -= copySize;
            }
        }

        public static async Task NewPackagesAsync(this TcpDataReader tcpDataReader, params byte[][] dataChunks)
        {
            foreach (var dataChunk in dataChunks)
            {
                await NewPackageAsync(tcpDataReader, dataChunk);
            }
        }

        public static void Print(this ReadOnlyMemory<byte> src)
        {
            Console.Write("[");
            foreach (var b in src.Span)
            {
                Console.Write(b+",");
            }
            Console.WriteLine("]");
        }
    }
}