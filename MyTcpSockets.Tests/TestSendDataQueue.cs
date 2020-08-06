using System;
using MyTcpSockets.DataSender;

using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TestEndDataQueue
    {

        [Test]
        public void TestOnePacketOneData()
        {
            var sendDataQueue = new SendDataQueue();

            var sourceData = new byte[] {1, 2, 3, 4};
            
            sendDataQueue.Enqueue(sourceData);

            var sharedBuffer = new byte[256];

            var result = sendDataQueue.Dequeue(sharedBuffer);
            
            sourceData.ArraysAreEqual(result);
        }
        
        [Test]
        public void TestTwoInOneOut()
        {
            var sendDataQueue = new SendDataQueue();

            var sourceData = new byte[] {1, 2, 3, 4};
            var sourceData2 = new byte[] {5, 6, 7, 8};
            
            sendDataQueue.Enqueue(sourceData);
            sendDataQueue.Enqueue(sourceData2);

            var sharedBuffer = new byte[256];

            var result = sendDataQueue.Dequeue(sharedBuffer);
            
            new byte[]{1,2,3,4,5,6,7,8}.ArraysAreEqual(result);
        }
        
        [Test]
        public void TestOnePacketLessThenTheBuffer()
        {
            var sendDataQueue = new SendDataQueue();

            var sourceData = new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4});
            
            sendDataQueue.Enqueue(sourceData);

            var sharedBuffer = new byte[3];

            var result = sendDataQueue.Dequeue(sharedBuffer);
            new byte[]{1,2,3}.ArraysAreEqual(result);
            
            result = sendDataQueue.Dequeue(sharedBuffer);
            new byte[]{4}.ArraysAreEqual(result);
            
        }
        
        
        
        [Test]
        public void TestComplexMix()
        {
            var sendDataQueue = new SendDataQueue();

            
            sendDataQueue.Enqueue(new byte[] {1, 2, 3, 4});
            sendDataQueue.Enqueue(new byte[] {5, 6, 7, 8});
            sendDataQueue.Enqueue(new byte[] {9, 0, 1, 2});

            
            var sharedBuffer = new byte[6];

            var result = sendDataQueue.Dequeue(sharedBuffer);
            
            new byte[]{1,2,3,4,5,6}.ArraysAreEqual(result);
            
            result = sendDataQueue.Dequeue(sharedBuffer);
            new byte[]{7,8,9,0,1,2}.ArraysAreEqual(result);
        }
        
        [Test]
        public void TestComplexMix2()
        {
            var sendDataQueue = new SendDataQueue();

            
            sendDataQueue.Enqueue(new byte[] {1, 2, 3, 4});
            sendDataQueue.Enqueue(new byte[] {5, 6, 7, 8});
            sendDataQueue.Enqueue(new byte[] {9, 0, 1, 2});

            
            var sharedBuffer = new byte[7];

            var result = sendDataQueue.Dequeue(sharedBuffer);
            
            new byte[]{1,2,3,4,5,6,7}.ArraysAreEqual(result);
            
            result = sendDataQueue.Dequeue(sharedBuffer);
            new byte[]{8,9,0,1,2}.ArraysAreEqual(result);
        }
    }
}