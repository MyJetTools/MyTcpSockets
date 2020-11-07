using System;
using System.IO;
using System.Threading;
using MyTcpSockets.Extensions;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TcpDataPipeExtensionsTests
    {
        [SetUp]
        public void Init()
        {
            TestUtils.PrepareTest();
        }
        
        [Test]
        public void TestByte()
        {
            var data = new byte[] {1, 2, 3, 4, 5};
            
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(data);
            
            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadByteAsync(token.Token).Result;
            
            Assert.AreEqual(1, result);
        }
        
        [Test]
        public void TestUShort()
        {
            var data = new byte[10];
            const ushort testValue = 30000;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(data);

            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadUShortAsync(token.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }   
        
        [Test]
        public void TestShort()
        {
            var data = new byte[10];
            const short testValue = 30000;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(data);
            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadShortAsync(token.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }  
        
        [Test]
        public void TestUInt()
        {
            var data = new byte[10];
            const uint testValue = 1234567;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(data);
            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadUIntAsync(token.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }   
        
        [Test]
        public void TestInt()
        {
            var data = new byte[10];
            const int testValue = 1234567;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(data);
            
            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadIntAsync(token.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }        
        
        
        [Test]
        public void TestULong()
        {
            var data = new byte[10];
            const ulong testValue = 123456789012;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(data);
            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadULongAsync(token.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }   
        
        [Test]
        public void TestLong()
        {
            var data = new byte[10];
            const long testValue = 1234567890123;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(data);
            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadLongAsync(token.Token).Result;
            
            Assert.AreEqual(testValue, result);
        } 
        
        
        [Test]
        public void TestPascalString()
        {
           var memoryStream = new MemoryStream();
           const string testValue = "My test String";
           
           memoryStream.WritePascalString(testValue);
         
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(memoryStream.ToArray());
            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadPascalStringAsync(token.Token).Result;
            
            Assert.AreEqual(testValue, result);
        } 
        
        [Test]
        public void TestString()
        {
            var memoryStream = new MemoryStream();
            const string testValue = "My test String";
           
            memoryStream.WriteString(testValue);
         
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(memoryStream.ToArray());
            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadStringAsync(token.Token).Result;
            
            Assert.AreEqual(testValue, result);
        }      
        
        [Test]
        public void TestByteArray()
        {
            var memoryStream = new MemoryStream();
            var testValue =  new byte[]{1,2,3,4,5,6};
           
            memoryStream.WriteByteArray(testValue);
         
            var tcpDataReader = new TcpDataReader();
            
            tcpDataReader.NewPackage(memoryStream.ToArray());
            var token = new CancellationTokenSource();
            var result = tcpDataReader.ReadByteArrayAsync(token.Token).Result;

            TestExtensions.AsReadOnlyMemory(testValue).ArraysAreEqual(result);
        } 
        
    }
}