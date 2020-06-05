using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TestProducerConsumer
    {
        [Test]
        public void BasicTest()
        {
            var pc = new ProducerConsumer<int>();
            
            pc.Produce(1);
            Assert.AreEqual(1, pc.Count);

            var result = pc.ConsumeAsync().Result;
            
            Assert.AreEqual(1, result);
        }
        
        [Test]
        public void BasicTest123()
        {
            var pc = new ProducerConsumer<int>();
            
            pc.Produce(1);
            pc.Produce(2);
            pc.Produce(3);
            Assert.AreEqual(3, pc.Count);
            
            var result = pc.ConsumeAsync().Result;
            Assert.AreEqual(1, result);
            Assert.AreEqual(2, pc.Count);
            
            result = pc.ConsumeAsync().Result;
            Assert.AreEqual(2, result);
            Assert.AreEqual(1, pc.Count);
            
            result = pc.ConsumeAsync().Result;
            Assert.AreEqual(3, result);
            Assert.AreEqual(0, pc.Count);
        }
        
        [Test]
        public void BasicTestConsumeFirst()
        {
            var pc = new ProducerConsumer<int>();

            var resultTask = pc.ConsumeAsync();
            Assert.AreEqual(-1, pc.Count);
            pc.Produce(1);
            pc.Produce(2);
            Assert.AreEqual(1, pc.Count);
            Assert.AreEqual(1, resultTask.Result);
            
            resultTask = pc.ConsumeAsync();
            Assert.AreEqual(2, resultTask.Result);
            Assert.AreEqual(0, pc.Count);
            
            resultTask = pc.ConsumeAsync();
            pc.Produce(3); 
            Assert.AreEqual(3, resultTask.Result);
            Assert.AreEqual(0, pc.Count);

        }
        
    }
}