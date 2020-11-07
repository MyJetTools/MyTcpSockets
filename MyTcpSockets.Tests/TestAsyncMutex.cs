using MyTcpSockets.DataSender;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TestAsyncMutex
    {
        
        [SetUp]
        public void Init()
        {
            TestUtils.PrepareTest();
        }
        
        [Test]
        public void TestGoAhead()
        {
            
            var mutex = new AsyncMutex();
            
            mutex.Update(true);

            var result = mutex.AwaitDataAsync();
            
            Assert.IsTrue(result.IsCompleted);

        }
        
        [Test]
        public void TestNoAndGoAhead()
        {
            
            var mutex = new AsyncMutex();
            
            mutex.Update(false);

            var result = mutex.AwaitDataAsync();
            
            Assert.IsFalse(result.IsCompleted);
            
            mutex.Update(true);
            
            Assert.IsTrue(result.IsCompleted);

        }
    }
}