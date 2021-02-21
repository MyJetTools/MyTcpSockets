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

            trafficReader.NewPackage(incomingArray);

            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, tc.Token);

            TestExtensions.AsReadOnlyMemory(incomingArray, 0, 3).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);
   
            data = await trafficReader.ReadAsyncAsync(2, tc.Token);
            TestExtensions.AsReadOnlyMemory(incomingArray, 3, 2).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);
            
        }

        [Test]
        public async Task TestOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);

            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, tc.Token);

            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);

            data = await trafficReader.ReadAsyncAsync(4, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);

        }

        [Test]
        public async Task TestDoubleOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);
            trafficReader.NewPackage(incomingArray3);

            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, tc.Token);

            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);

            data = await trafficReader.ReadAsyncAsync(6, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 111}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);

        }

        [Test]
        public async Task TestDoubleVeryOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);
            trafficReader.NewPackage(incomingArray3);
            
            
            var tc = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);

            data = await trafficReader.ReadAsyncAsync(10, tc.Token);
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 33, 44, 55, 66, 111}).ArraysAreEqual(data);
            trafficReader.CommitReadDataSize(data.Length);
        }
    }
}