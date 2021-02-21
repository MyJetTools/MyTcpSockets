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
        public void TestFindingTheSequenceFeatureAtTheSameArray()
        {

            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 15, 4, 5, 13};

            trafficReader.NewPackage(incomingArray1);

            var token = new CancellationTokenSource();
            var data = trafficReader.ReadWhileWeGetSequenceAsync(new byte[] {4, 5}, token.Token).Result;
            
            Console.WriteLine("DataLen:"+data.Length);
            
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 15, 4, 5}).ArrayIsEqualWith(data);

        }

        [Test]
        public async Task TestFindingTheSequenceFeatureByReadingToArraysAtTheEnd()
        {
            var trafficReader = new TcpDataReader(1024);
            
            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);

            var token = new CancellationTokenSource();
            var data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArrayIsEqualWith(data);
            trafficReader.CommitReadDataSize(data.Length);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArrayIsEqualWith(data);
            trafficReader.CommitReadDataSize(data.Length);
        }  
        
        [Test]
        public async Task TestFindingTheSequenceRightAway()
        {
            var trafficReader = new TcpDataReader(1024);
            
            var incomingArray1 = new byte[] {1, 2, 3, 4, 5};
            var incomingArray2 = new byte[] {11, 12, 13, 4, 5};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);

            var token = new CancellationTokenSource();
            var data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5}).ArrayIsEqualWith(data);
            trafficReader.CommitReadDataSize(data.Length);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {11, 12, 13, 4, 5}).ArrayIsEqualWith(data);
            trafficReader.CommitReadDataSize(data.Length);
        }  
        
        
        [Test]
        public async Task TestFindingTheSequenceFeatureByReadingToArraysAtTheEndOtherWayAround()
        {
            var trafficReader = new TcpDataReader(1024);
            
            var token = new CancellationTokenSource();
            var dataTask = trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5, 11}, token.Token);

            trafficReader.NewPackage(new byte[] {1, 2, 3, 4, 5});
            trafficReader.NewPackage(new byte[] {11, 12, 13, 4, 5});
            
            var data = await dataTask;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11}).ArrayIsEqualWith(data);
            trafficReader.CommitReadDataSize(data.Length);
            
            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{4, 5}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {12, 13, 4, 5}).ArrayIsEqualWith(data);
            trafficReader.CommitReadDataSize(data.Length);
        }


        [Test]
        public async Task TestFindingSequenceWithTwoAsOnePieceAndOtherComplete()
        {

            var trafficReader = new TcpDataReader(1024);

            var token = new CancellationTokenSource();
            var dataTask = trafficReader.ReadWhileWeGetSequenceAsync(new byte[] {14, 15}, token.Token);

            trafficReader.NewPackage(new byte[] {1, 2, 3, 4, 5});
            trafficReader.NewPackage(new byte[] {11, 12, 13, 14, 15});
            trafficReader.NewPackage(new byte[] {21, 22, 23, 14, 15});
            
            var data = await dataTask;
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 11, 12, 13, 14, 15}).ArrayIsEqualWith(data);
            trafficReader.CommitReadDataSize(data.Length);

            data = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[] {14, 15}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {21, 22, 23, 14, 15}).ArrayIsEqualWith(data);
            trafficReader.CommitReadDataSize(data.Length);
        }

    }

}