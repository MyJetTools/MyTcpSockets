using System;
using System.IO;
using System.Threading;
using MyTcpSockets.Extensions;
using MyTcpSockets.Extensions.Tests;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TcpDataPipeExtensionsTests
    {
        [Test]
        public void TestByte()
        {
            var data = new byte[] {1, 2, 3, 4, 5};
            
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(data);

            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadByteAsync(tc.Token).Result;
            
            Assert.AreEqual(1, result);
        }
        
        [Test]
        public void TestUShort()
        {
            var data = new byte[10];
            const ushort testValue = 30000;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(data);
            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadUShortAsync(tc.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }   
        
        [Test]
        public void TestShort()
        {
            var data = new byte[10];
            const short testValue = 30000;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(data);
            
            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadShortAsync(tc.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }  
        
        [Test]
        public void TestUInt()
        {
            var data = new byte[10];
            const uint testValue = 1234567;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(data);

            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadUIntAsync(tc.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }   
        
        [Test]
        public void TestInt()
        {
            var data = new byte[10];
            const int testValue = 1234567;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(data);

            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadIntAsync(tc.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }        
        
        
        [Test]
        public void TestULong()
        {
            var data = new byte[10];
            const ulong testValue = 123456789012;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(data);

            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadULongAsync(tc.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }   
        
        [Test]
        public void TestLong()
        {
            var data = new byte[10];
            const long testValue = 1234567890123;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(data);

            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadLongAsync(tc.Token).Result;
            
            Assert.AreEqual(testValue, result);
        } 
        
        
        [Test]
        public void TestPascalString()
        {
           var memoryStream = new MemoryStream();
           const string testValue = "My test String";
           
           memoryStream.WritePascalString(testValue);
         
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(memoryStream.ToArray());

            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadPascalStringAsync(tc.Token).Result;
            
            Assert.AreEqual(testValue, result);
        } 
        
        [Test]
        public void TestString()
        {
            var memoryStream = new MemoryStream();
            const string testValue = "My test String";
           
            memoryStream.WriteString(testValue);
         
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(memoryStream.ToArray());

            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadStringAsync(tc.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }      
        
        [Test]
        public void TestByteArray()
        {
            var memoryStream = new MemoryStream();
            var testValue =  new byte[]{1,2,3,4,5,6};
           
            memoryStream.WriteByteArray(testValue);
         
            var tcpDataReader = new TcpDataReader(1024, 512);
            
            tcpDataReader.NewPackage(memoryStream.ToArray());

            var tc = new CancellationTokenSource();
            var result = tcpDataReader.ReadByteArrayAsync(tc.Token).Result;

            TestExtensions.ArraysAreEqual(TestExtensions.AsReadOnlyMemory(testValue), result);
        } 
        
    }
}