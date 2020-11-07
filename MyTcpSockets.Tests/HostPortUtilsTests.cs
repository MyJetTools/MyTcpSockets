using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class HostPortUtilsTests
    {
        
        [SetUp]
        public void Init()
        {
            TestUtils.PrepareTest();
        }

        [Test]
        public async Task ParseSimpleIp()
        {
            var hostPorts = await "127.0.0.1:4565".ParseAndResolveHostPort();
            
            Assert.AreEqual(1, hostPorts.Count);
            
            Assert.AreEqual("127.0.0.1", hostPorts[0].Address.ToString());
            Assert.AreEqual(4565, hostPorts[0].Port);
        }
        
        
        [Test]
        public async Task ParseSimpleLocalhost()
        {
            var hostPorts = await "127.0.0.1:4565".ParseAndResolveHostPort();
            
            Assert.IsTrue(hostPorts.Count>0);

            foreach (var hostPort in hostPorts)
            {
                Console.WriteLine(hostPort);
                Assert.AreEqual(4565, hostPort.Port);
            }
        }
    }
}