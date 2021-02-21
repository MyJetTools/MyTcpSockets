using System;
using System.Threading;
using System.Threading.Tasks;
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
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 13};

            trafficReader.NewPackage(incomingArray1);
            var tc = new CancellationTokenSource();
            var data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, tc.Token).Result;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5}).ArraysAreEqual(data);
        }  
        
        [Test]
        public async Task TestFindingTheSequenceFeatureByReadingToArraysAtTheEnd()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);

            var tc = new CancellationTokenSource();
            var data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, tc.Token).Result;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);
        }  
        
        
        [Test]
        public async Task TestFindingTheSequenceFeatureByReadingToArraysAtTheEndOtherWayAround()
        {
            var trafficReader = new TcpDataReader(1024);
            var tc = new CancellationTokenSource();
            var dataTask = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, tc.Token);
            
            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);
            
            var data = await dataTask;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);
        } 
    }
}