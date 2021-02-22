using System;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{

    public class TcpDataReaderSequenceModeTests
    {
        
        [SetUp]
        public void Init()
        {
            TestUtils.PrepareTest();
        }


        [Test]
        public async Task TestFindingTheSequenceFeatureAtTheSameArray()
        {

            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 15, 4, 5, 13};

            await trafficReader.NewPackageAsync(incomingArray1);

            var token = new CancellationTokenSource();
            var data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[] {4, 5}, token.Token);
            
            Console.WriteLine("DataLen:"+data.Length);
            
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 15, 4, 5}).ArrayIsEqualWith(data.AsArray());

        }

        [Test]
        public async Task TestFindingTheSequenceFeatureByReadingToArraysAtTheEnd()
        {
            var trafficReader = new TcpDataReader(1024);
            
            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            var writeTask = trafficReader.NewPackagesAsync(incomingArray1, incomingArray2);

            var token = new CancellationTokenSource();
            var data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);

            await writeTask;
        }  
        
        [Test]
        public async Task TestFindingTheSequenceRightAway()
        {
            var trafficReader = new TcpDataReader(1024);
            
            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            var writeTask = trafficReader.NewPackagesAsync(incomingArray1, incomingArray2);

            var token = new CancellationTokenSource();
            var data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {11, 12, 13, 4, 5}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);

            await writeTask;
        }  
        
        
        [Test]
        public async Task TestFindingTheSequenceFeatureByReadingToArraysAtTheEndOtherWayAround()
        {
            var trafficReader = new TcpDataReader(1024);
            
            var token = new CancellationTokenSource();
            var dataTask = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, token.Token);

            var writeTask = trafficReader.NewPackagesAsync(
                new byte[] {1, 2, 3, 4, 5},
                new byte[] {11, 12, 13, 4, 5});
            
            var data = await dataTask;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);

            await writeTask;
        }


        [Test]
        public async Task TestFindingSequenceWithTwoAsOnePieceAndOtherComplete()
        {
            var trafficReader = new TcpDataReader(1024);

            var token = new CancellationTokenSource();
            var dataTask = trafficReader.ReadWhileWeGetSequenceAsync(new byte[] {14, 15}, token.Token);

            var writeTask =  trafficReader.NewPackagesAsync(
                new byte[] {1, 2, 3, 4, 5},
                new byte[] {11, 12, 13, 14, 15},
                new byte[] {21, 22, 23, 14, 15});
            
            var data = await dataTask;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11, 12, 13, 14, 15}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);

            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[] {14, 15}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {21, 22, 23, 14, 15}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);

            await writeTask;
        }
        

    }

}