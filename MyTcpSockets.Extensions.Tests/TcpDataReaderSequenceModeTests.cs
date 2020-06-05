using System;
using NUnit.Framework;

namespace MyTcpSockets.Extensions.Tests
{
    public class TcpDataReaderSequenceModeTests
    {
        [Test]
        public void TestFindingTheSequenceFeatureAtTheSameArray()
        {
            var trafficReader = new TcpDataReader();

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 13};

            trafficReader.NewPackage(incomingArray1);

            var data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}).Result;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5}).ArraysAreEqual(data);
        }  
        
        [Test]
        public void TestFindingTheSequenceFeatureByReadingToArraysAtTheEnd()
        {
            var trafficReader = new TcpDataReader();

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);

            
            var data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}).Result;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArraysAreEqual(data);

            data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}).Result;
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArraysAreEqual(data);
        }  
        
        
        [Test]
        public void TestFindingTheSequenceFeatureByReadingToArraysAtTheEndOtherWayAround()
        {
            var trafficReader = new TcpDataReader();
            
            var dataTask = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11});
            
            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);
            
            var data = dataTask.Result;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArraysAreEqual(data);

            data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}).Result;
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArraysAreEqual(data);
        } 
    }
}