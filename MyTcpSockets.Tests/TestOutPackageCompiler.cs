using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    
    
    public class TestOutPackageCompiler
    {


        public class TcpContextMock : ITcpContext
        {
            public Stream SocketStream { get; } = null;
            public Queue<ReadOnlyMemory<byte>> DataToSend { get; } = new Queue<ReadOnlyMemory<byte>>();
            public ValueTask DisconnectAsync()
            {
                Connected = false;
                return new ValueTask();
            }

            public long Id { get; }
            public bool Connected { get; set; } = true;
        }
        

        [Test]
        public void TaskPackage()
        {
            
            var tcpContext = new TcpContextMock();
            
            tcpContext.DataToSend.Enqueue(new byte[]{1,2,3});
            tcpContext.DataToSend.Enqueue(new byte[]{4});
            tcpContext.DataToSend.Enqueue(new byte[]{5});

            var buffer = new byte[2];

            var package = tcpContext.CompileDataToSend(buffer);
            package.ArraysAreEqual(new byte[]{1,2,3});
            Assert.AreEqual(2, tcpContext.DataToSend.Count);
            
            
            package = tcpContext.CompileDataToSend(buffer);
            package.ArraysAreEqual(new byte[]{4, 5});
            Assert.AreEqual(0, tcpContext.DataToSend.Count);

        }
        
        [Test]
        public void TaskPackageOne()
        {
            
            var tcpContext = new TcpContextMock();
            
            tcpContext.DataToSend.Enqueue(new byte[]{1,2,3});
            tcpContext.DataToSend.Enqueue(new byte[]{4});
            tcpContext.DataToSend.Enqueue(new byte[]{5});

            var buffer = new byte[5];

            var package = tcpContext.CompileDataToSend(buffer);
            package.ArraysAreEqual(new byte[]{1,2,3,4,5});
            Assert.AreEqual(0, tcpContext.DataToSend.Count);
            

        }
        
    }
}