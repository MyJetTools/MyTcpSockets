using System;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;
using MyTcpSockets.Extensions.Tests;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TcpDataReaderSizeTests
    {
        [Test]
        public async Task TestBasicFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray = new byte[] {1, 2, 3, 4, 5, 6};

            await trafficReader.NewPackageAsync(incomingArray);

            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, tc.Token);

            TestExtensions.AsReadOnlyMemory(incomingArray, 0, 3).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);
   
            data = await trafficReader.ReadAsyncAsync(2, tc.Token);
            TestExtensions.AsReadOnlyMemory(incomingArray, 3, 2).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);
            
        }

        [Test]
        public async Task TestOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66};

            var writingTasks = trafficReader.NewPackagesAsync(incomingArray1, incomingArray2);

            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, tc.Token);

            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);

            data = await trafficReader.ReadAsyncAsync(4, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);

            await writingTasks;

        }

        [Test]
        public async Task TestDoubleOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            var writingTask = trafficReader.NewPackagesAsync(incomingArray1, incomingArray2, incomingArray3);


            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, tc.Token);

            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);

            data = await trafficReader.ReadAsyncAsync(6, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 111}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);

            await writingTask;

        }

        [Test]
        public async Task TestDoubleVeryOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            var writingTasks = trafficReader.NewPackagesAsync(incomingArray1, incomingArray2, incomingArray3);
            
            
            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);

            data = await trafficReader.ReadAsyncAsync(10, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 33, 44, 55, 66, 111}).ArraysAreEqual(data.AsArray());
            trafficReader.CommitReadData(data);

            await writingTasks;
        }
    }
}