using System;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class DataReaderMultiLineTests
    {
        [Test]
        public async Task TestSizeWithSeveralChunks()
        {

            var trafficReader = new TcpDataReader(5);

            var incomingArray = new byte[] {1, 2, 3, 4, 5};

            await trafficReader.NewPackageAsync(incomingArray);
            
            incomingArray = new byte[] {6, 7, 8, 9, 10};

            var task = trafficReader.NewPackageAsync(incomingArray);

            var token = new CancellationTokenSource();
            var result1 = await trafficReader.ReadAsyncAsync(7, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 6, 7}).ArrayIsEqualWith(result1.AsArray());
            trafficReader.CommitReadData(result1);
 
            var result2 = await trafficReader.ReadAsyncAsync(3, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {8, 9, 10}).ArrayIsEqualWith(result2.AsArray());
            trafficReader.CommitReadData(result2);

            await task;
        }
        
        [Test]
        public async Task TestSearchWithSeveralChunks()
        {

            var trafficReader = new TcpDataReader(5);

            var incomingArray = new byte[] {1, 2, 3, 4, 5};

            await trafficReader.NewPackageAsync(incomingArray);
            
            incomingArray = new byte[] {6, 7, 8, 9, 10};

            var newSecondPackageTask = trafficReader.NewPackageAsync(incomingArray);

            var token = new CancellationTokenSource();
            var result1 = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{5,6}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3, 4, 5, 6}).ArrayIsEqualWith(result1.CommittedMemory);
            trafficReader.CommitReadData(result1);
 
            var result2 = await trafficReader.ReadWhileWeGetSequenceAsync(new byte[]{9,10}, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {7, 8, 9, 10}).ArrayIsEqualWith(result2.UncommittedMemory);
            trafficReader.CommitReadData(result1);

            await newSecondPackageTask;
        }
    }
}