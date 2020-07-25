using System;
using System.Threading;
using MyTcpSockets.Extensions;
using MyTcpSockets.Extensions.Tests;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TcpDataReaderSequenceModeTests
    {
        [Test]
        public void TestFindingTheSequenceFeatureAtTheSameArray()
        {
            var trafficReader = new TcpDataReader();

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 13};

            trafficReader.NewPackage(incomingArray1);

            var token = new CancellationTokenSource();
            var data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token).Result;
            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5}), data);
        }  
        
        [Test]
        public void TestFindingTheSequenceFeatureByReadingToArraysAtTheEnd()
        {
            var trafficReader = new TcpDataReader();

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);

            var token = new CancellationTokenSource();
            var data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, token.Token).Result;
            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}), data);

            data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token).Result;
            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}), data);
        }  
        
        
        [Test]
        public void TestFindingTheSequenceFeatureByReadingToArraysAtTheEndOtherWayAround()
        {
            var trafficReader = new TcpDataReader();
            
            var token = new CancellationTokenSource();
            var dataTask = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, token.Token);

            trafficReader.NewPackage(new byte[] {1, 2, 3, 4, 5});
            trafficReader.NewPackage(new byte[] {11, 12, 13, 4, 5});
            
            var data = dataTask.Result;
            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}), data);

            data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token).Result;
            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}), data);
        } 
    }
}