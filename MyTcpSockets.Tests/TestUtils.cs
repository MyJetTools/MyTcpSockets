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
        
        public static void ArraysAreEqual(this ReadOnlyMemory<byte> from, ReadOnlyMemory<byte> to)
        {
            var fromSpan = from.Span;
            var toSpan = to.Span;
            Assert.IsTrue(from.Length == to.Length);

            for (var i = 0; i < fromSpan.Length; i++)
            {
                Assert.AreEqual(fromSpan[i], toSpan[i]);
            }
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
    }
}