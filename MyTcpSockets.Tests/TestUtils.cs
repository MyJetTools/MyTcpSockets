using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}