using System;
using System.Threading;
using MyTcpSockets.Extensions;
using MyTcpSockets.Extensions.Tests;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TcpDataReaderSizeTests
    {
        [Test]
        public void TestBasicFeature()
        {
            var trafficReader = new TcpDataReader(1024, 512);

            var incomingArray = new byte[] {1, 2, 3, 4, 5, 6};

            trafficReader.NewPackage(incomingArray);

            var tc = new CancellationTokenSource();
            var data = trafficReader.ReadAsyncAsync(3, tc.Token).Result;

            TestExtensions.ArraysAreEqual(TestExtensions.AsReadOnlyMemory(incomingArray, 0, 3), data);
   
            data = trafficReader.ReadAsyncAsync(2, tc.Token).Result;
            TestExtensions.ArraysAreEqual(TestExtensions.AsReadOnlyMemory(incomingArray, 3, 2), data);
            
        }

        [Test]
        public void TestOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024, 512);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);

            var tc = new CancellationTokenSource();
            var data = trafficReader.ReadAsyncAsync(3, tc.Token).Result;

            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}), data);

            data = trafficReader.ReadAsyncAsync(4, tc.Token).Result;
            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11}), data);

        }

        [Test]
        public void TestDoubleOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024, 512);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);
            trafficReader.NewPackage(incomingArray3);

            var tc = new CancellationTokenSource();
            var data = trafficReader.ReadAsyncAsync(3, tc.Token).Result;

            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArraysAreEqual(data);

            data = trafficReader.ReadAsyncAsync(6, tc.Token).Result;
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 111}).ArraysAreEqual(data);

        }

        [Test]
        public void TestDoubleVeryOverflowFeature()
        {
            var trafficReader = new TcpDataReader(1024, 512);

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);
            trafficReader.NewPackage(incomingArray3);
            
            
            var tc = new CancellationTokenSource();
            var data = trafficReader.ReadAsyncAsync(3, tc.Token).Result;

            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}), data);

            data = trafficReader.ReadAsyncAsync(10, tc.Token).Result;
            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 33, 44, 55, 66, 111}), data);

        }
    }
}