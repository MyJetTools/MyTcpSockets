using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;
using MyTcpSockets.Extensions.Tests;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TcpDataPipeExtensionsTests
    {
        [Test]
        public async Task TestByte()
        {
            var data = new byte[] {1, 2, 3, 4, 5};
            
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(data);

            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadAndCommitByteAsync(tc.Token);
            
            Assert.AreEqual(1, result);
        }
        
        [Test]
        public async Task TestUShort()
        {
            var data = new byte[10];
            const ushort testValue = 30000;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(data);
            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadUShortAsync(tc.Token);
            
            Assert.AreEqual(testValue, result);
        }   
        
        [Test]
        public async Task TestShort()
        {
            var data = new byte[10];
            const short testValue = 30000;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(data);
            
            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadShortAsync(tc.Token);
            
            Assert.AreEqual(testValue, result);
        }  
        
        [Test]
        public async Task TestUInt()
        {
            var data = new byte[10];
            const uint testValue = 1234567;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(data);

            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadUIntAsync(tc.Token);
            
            Assert.AreEqual(testValue, result);
        }   
        
        [Test]
        public async Task TestInt()
        {
            var data = new byte[10];
            const int testValue = 1234567;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(data);

            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadIntAsync(tc.Token);
            
            Assert.AreEqual(testValue, result);
        }        
        
        
        [Test]
        public async Task TestULong()
        {
            var data = new byte[10];
            const ulong testValue = 123456789012;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(data);

            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadULongAsync(tc.Token);
            
            Assert.AreEqual(testValue, result);
        }   
        
        [Test]
        public async Task TestLong()
        {
            var data = new byte[10];
            const long testValue = 1234567890123;
            BitConverter.TryWriteBytes(data, testValue);
            
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(data);

            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadLongAsync(tc.Token);
            
            Assert.AreEqual(testValue, result);
        } 
        
        
        [Test]
        public async Task TestPascalString()
        {
           var memoryStream = new MemoryStream();
           const string testValue = "My test String";
           
           memoryStream.WritePascalString(testValue);
         
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(memoryStream.ToArray());

            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadPascalStringAsync(tc.Token);
            
            Assert.AreEqual(testValue, result);
        } 
        
        [Test]
        public async Task TestString()
        {
            var memoryStream = new MemoryStream();
            const string testValue = "My test String";
           
            memoryStream.WriteString(testValue);
         
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(memoryStream.ToArray());

            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadStringAsync(tc.Token);
            
            Assert.AreEqual(testValue, result);
        }      
        
        [Test]
        public async Task TestByteArray()
        {
            var memoryStream = new MemoryStream();
            var testValue =  new byte[]{1,2,3,4,5,6};
           
            memoryStream.WriteByteArray(testValue);
         
            var tcpDataReader = new TcpDataReader(1024);
            
            await tcpDataReader.NewPackageAsync(memoryStream.ToArray());

            var tc = new CancellationTokenSource();
            var result = await tcpDataReader.ReadByteArrayAsync(tc.Token);

            TestExtensions.AsReadOnlyMemory(testValue).ArraysAreEqual(result);
        } 
        
    }
}