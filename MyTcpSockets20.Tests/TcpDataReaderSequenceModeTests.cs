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
        public async Task TestFindingTheSequenceFeatureAtTheSameArray()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 13};

            await trafficReader.NewPackageAsync(incomingArray1);
            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5}).ArraysAreEqual(data.AsArray());
        }  
        
        [Test]
        public async Task TestFindingTheSequenceFeatureByReadingToArraysAtTheEnd()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            var writingTask = trafficReader.NewPackagesAsync(incomingArray1, incomingArray2);

            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);

            await writingTask;
        }  
        
        
        [Test]
        public async Task TestFindingTheSequenceFeatureByReadingToArraysAtTheEndOtherWayAround()
        {
            var trafficReader = new TcpDataReader(1024);
            var tc = new CancellationTokenSource();
            var dataTask = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, tc.Token);
            
            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            var writingTask =  trafficReader.NewPackagesAsync(incomingArray1, incomingArray2);
            
            var data = await dataTask;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);

            await writingTask;
        } 
    }
}