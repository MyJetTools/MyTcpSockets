using System;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{

    public class TcpDataReaderSizeTests
    {
        
        [SetUp]
        public void Init()
        {
            TestUtils.PrepareTest();
        }
        

        [Test]
        public async Task TestBasicFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray = new byte[] {1, 2, 3, 4, 5, 6};

            await trafficReader.NewPackageAsync(incomingArray);

            var token = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, token.Token);

            TestExtensions.AsReadOnlyMemory(incomingArray, 0, 3).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
   
            data = trafficReader.ReadAsyncAsync(2, token.Token).Result;
            TestExtensions.AsReadOnlyMemory(incomingArray, 3, 2).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
            
        }

        [Test]
        public async Task TestOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66};

            await trafficReader.NewPackageAsync(incomingArray1);
            await trafficReader.NewPackageAsync(incomingArray2);

            var token = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, token.Token);

            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
            
            data = await trafficReader.ReadAsyncAsync(4, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
        }

        [Test]
        public async Task TestDoubleOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            await trafficReader.NewPackageAsync(incomingArray1);
            await trafficReader.NewPackageAsync(incomingArray2);
            await trafficReader.NewPackageAsync(incomingArray3);

            var token = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
            
            data = await trafficReader.ReadAsyncAsync(6, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 111}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
            
        }

        [Test]
        public async Task TestDoubleVeryOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            await trafficReader.NewPackageAsync(incomingArray1);
            await trafficReader.NewPackageAsync(incomingArray2);
            await trafficReader.NewPackageAsync(incomingArray3);

            var token = new CancellationTokenSource();
            var data = await trafficReader.ReadAsyncAsync(3, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
            
            data = await trafficReader.ReadAsyncAsync(10, token.Token);
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 33, 44, 55, 66, 111}).ArrayIsEqualWith(data.AsArray());
            trafficReader.CommitReadData(data);
            
        }
        

    }

}