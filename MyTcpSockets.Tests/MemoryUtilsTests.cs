using System;
using System.Collections.Generic;
using MyTcpSockets.Extensions;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class MemoryUtilsTests
    {

        [Test]
        public void TestOnePieceOneRead()
        {

            var list = new LinkedList<ReadOnlyMemory<byte>>();

            list.AddFirst(new byte[] {1, 2, 3, 4, 5});

            var buffer = new byte[3];

            var result = list.ReadAndCopyTo(buffer);
            
            result.ToArray().ArraysAreEqual(new byte[]{1,2,3});
            
            list.First.Value.ToArray().ArraysAreEqual(new byte[]{4,5});
            
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void TestTwoPiecesOneRead()
        {
            var list = new LinkedList<ReadOnlyMemory<byte>>();

            list.AddFirst(new byte[] {1, 2, 3, 4, 5});
            list.AddLast(new byte[] {6, 7, 8, 9, 10});

            var buffer = new byte[6];

            var result = list.ReadAndCopyTo(buffer);

            result.ToArray().ArraysAreEqual(new byte[] {1, 2, 3, 4, 5, 6});

            list.First.Value.ToArray().ArraysAreEqual(new byte[] {7, 8, 9, 10});
            
            Assert.AreEqual(1, list.Count);
        }
        
        [Test]
        public void TestTwoPiecesOneFullRead()
        {
            var list = new LinkedList<ReadOnlyMemory<byte>>();

            list.AddFirst(new byte[] {1, 2, 3, 4, 5});
            list.AddLast(new byte[] {6, 7, 8, 9, 10});

            var buffer = new byte[10];

            var result = list.ReadAndCopyTo(buffer);

            result.ToArray().ArraysAreEqual(new byte[] {1, 2, 3, 4, 5, 6, 7,8,9,10});
            
            Assert.AreEqual(0, list.Count);

        }
        
    }
}