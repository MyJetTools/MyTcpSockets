using System.IO;
using NUnit.Framework;

namespace MyTcpSockets.Extensions.Tests
{
    public class TestsSteams
    {
        
        [Test]
        public void TestShort()
        {
            const short testValue = 478;
            var testStream = new MemoryStream();

            testStream.WriteShort(testValue);

            testStream.Position = 0;

            var result = testStream.ReadShortAsync().Result;

            Assert.AreEqual(testValue, result);
        }
        
        [Test]
        public void TestUShort()
        {
            const ushort testValue = 478;
            var testStream = new MemoryStream();

            testStream.WriteUlong(testValue);

            testStream.Position = 0;

            var result = testStream.ReadUshortAsync().Result;

            Assert.AreEqual(testValue, result);
        }        
        
        [Test]
        public void TestLong()
        {
            const long testValue = 478;
            var testStream = new MemoryStream();

            testStream.WriteLong(testValue);

            testStream.Position = 0;

            var result = testStream.ReadLongAsync().Result;

            Assert.AreEqual(testValue, result);
        }
        
        [Test]
        public void TestULong()
        {
            const ulong testValue = 478;
            var testStream = new MemoryStream();

            testStream.WriteUlong(testValue);

            testStream.Position = 0;

            var result = testStream.ReadUlongAsync().Result;

            Assert.AreEqual(testValue, result);
        }

        [Test]
        public void TestInt()
        {
            const int testValue = 478;
            var testStream = new MemoryStream();

            testStream.WriteInt(testValue);

            testStream.Position = 0;

            var result = testStream.ReadIntAsync().Result;

            Assert.AreEqual(testValue, result);

        }

        [Test]
        public void TestUint()
        {
            const uint testValue = 478545652;
            var testStream = new MemoryStream();

            testStream.WriteUint(testValue);

            testStream.Position = 0;

            var result = testStream.ReadUintAsync().Result;

            Assert.AreEqual(testValue, result);

        }

        [Test]
        public void TestString()
        {
            const string testValue = "Test";
            var testStream = new MemoryStream();

            testStream.WriteString(testValue);

            testStream.Position = 0;

            var result = testStream.ReadStringAsync().Result;

            Assert.AreEqual(testValue, result.result);
            Assert.AreEqual(8, result.dataSize);
        }

        [Test]
        public void TestPascalString()
        {
            const string testValue = "Test";
            var testStream = new MemoryStream();

            testStream.WritePascalString(testValue);

            testStream.Position = 0;

            var result = testStream.ReadPascalStringAsync().Result;

            Assert.AreEqual(testValue, result.result);
            Assert.AreEqual(5, result.dataSize);
        }
        
        
        [Test]
        public void TestArray()
        {
            var testValue = new byte[]{0,1,2,3};
            var testStream = new MemoryStream();

            testStream.WriteByteArray(testValue);

            testStream.Position = 0;

            var result = testStream.ReadByteArrayAsync().Result;

            Assert.AreEqual(testValue.Length, result.data.Length);

            for (var i = 0; i < testValue.Length; i++)
                Assert.AreEqual(testValue[i], result.data[i]);

            Assert.AreEqual(8, result.dataSize);
        }

    }
}