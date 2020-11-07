using System;
using System.Threading;
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
        public void TestBasicFeature()
        {
            var trafficReader = new TcpDataReader();

            var incomingArray = new byte[] {1, 2, 3, 4, 5, 6};

            trafficReader.NewPackage(incomingArray, incomingArray.Length);

            var token = new CancellationTokenSource();
            var data = trafficReader.ReadAsyncAsync(3, token.Token).Result;

            TestExtensions.ArraysAreEqual(TestExtensions.AsReadOnlyMemory(incomingArray, 0, 3), data);
   
            data = trafficReader.ReadAsyncAsync(2, token.Token).Result;
            TestExtensions.ArraysAreEqual(TestExtensions.AsReadOnlyMemory(incomingArray, 3, 2), data);
            
        }

        [Test]
        public void TestOverflowFeature()
        {
            var trafficReader = new TcpDataReader();

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66};

            trafficReader.NewPackage(incomingArray1, incomingArray1.Length);
            trafficReader.NewPackage(incomingArray2, incomingArray2.Length);

            var token = new CancellationTokenSource();
            var data = trafficReader.ReadAsyncAsync(3, token.Token).Result;

            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}), data);

            data = trafficReader.ReadAsyncAsync(4, token.Token).Result;
            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11}), data);

        }

        [Test]
        public void TestDoubleOverflowFeature()
        {
            var trafficReader = new TcpDataReader();

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);
            trafficReader.NewPackage(incomingArray3);

            var token = new CancellationTokenSource();
            var data = trafficReader.ReadAsyncAsync(3, token.Token).Result;

            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}), data);

            data = trafficReader.ReadAsyncAsync(6, token.Token).Result;
            TestExtensions.ArraysAreEqual(new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 111}), data);

        }

        [Test]
        public void TestDoubleVeryOverflowFeature()
        {
            var trafficReader = new TcpDataReader();

            var incomingArray1 = new byte[] {1, 2, 3, 4, 5, 6};
            var incomingArray2 = new byte[] {11, 22, 33, 44, 55, 66,};
            var incomingArray3 = new byte[] {111, 222, 233, 244, 254, 255};

            trafficReader.NewPackage(incomingArray1);
            trafficReader.NewPackage(incomingArray2);
            trafficReader.NewPackage(incomingArray3);

            var token = new CancellationTokenSource();
            var data = trafficReader.ReadAsyncAsync(3, token.Token).Result;

            new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}).ArraysAreEqual(data);
            
            data = trafficReader.ReadAsyncAsync(10, token.Token).Result;
            new ReadOnlyMemory<byte>(new byte[] {4, 5, 6, 11, 22, 33, 44, 55, 66, 111}).ArraysAreEqual(data);

        }

    }
}