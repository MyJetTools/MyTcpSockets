using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;
using MyTcpSockets.Tests.Mocks;
using NUnit.Framework;

namespace MyTcpSockets.Tests
{
    public class TestTcpTrafficReader
    {

        private static byte[] ClCr = { 13, 10 };

        [Test]
        public async Task ReadTcpTrafficWithSinglePacket()
        {
            var trafficReader = new IncomingTrafficReaderMock();
            
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue((byte)'E');
            trafficReader.IncomingTraffic.Enqueue((byte)'S');
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue(13);
            trafficReader.IncomingTraffic.Enqueue(10);


            var tcpTrafficReader = new TcpDataReader(trafficReader, 1024);

            var ct = new CancellationTokenSource();
            
            var result = await tcpTrafficReader.ReadWhileWeGetSequenceAsync(ClCr, ct.Token);

            var str = Encoding.UTF8.GetString(result.Span);
            
            Assert.AreEqual("TEST"+(char)13+(char)10, str);
        }
        
        [Test]
        public async Task ReadTcpTrafficWithTwoPackets()
        {
            var trafficReader = new IncomingTrafficReaderMock();
            
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue((byte)'E');
            trafficReader.IncomingTraffic.Enqueue((byte)'S');
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue((byte)'1');
            trafficReader.IncomingTraffic.Enqueue(13);
            trafficReader.IncomingTraffic.Enqueue(10);
            
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue((byte)'E');
            trafficReader.IncomingTraffic.Enqueue((byte)'S');
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue((byte)'2');
            trafficReader.IncomingTraffic.Enqueue(13);
            trafficReader.IncomingTraffic.Enqueue(10);


            var tcpTrafficReader = new TcpDataReader(trafficReader, 1024);
            
            /////////////

            var ct = new CancellationTokenSource();
            
            var result = await tcpTrafficReader.ReadWhileWeGetSequenceAsync(ClCr, ct.Token);

            var str = Encoding.UTF8.GetString(result.Span);
            
            Assert.AreEqual("TEST1"+(char)13+(char)10, str);

            ///////////
            
            ct = new CancellationTokenSource();
            
            result = await tcpTrafficReader.ReadWhileWeGetSequenceAsync(ClCr, ct.Token);

            str = Encoding.UTF8.GetString(result.Span);
            
            Assert.AreEqual("TEST2"+(char)13+(char)10, str);
        }
        
        
        [Test]
        public async Task ReadTcpTrafficWithTwoPacketsButNotSameTime()
        {
            var trafficReader = new IncomingTrafficReaderMock();
            
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue((byte)'E');
            trafficReader.IncomingTraffic.Enqueue((byte)'S');
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue((byte)'1');
            trafficReader.IncomingTraffic.Enqueue(13);
            trafficReader.IncomingTraffic.Enqueue(10);
            
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue((byte)'E');


            var tcpTrafficReader = new TcpDataReader(trafficReader, 1024);
            
            /////////////

            var ct = new CancellationTokenSource();
            
            var result = await tcpTrafficReader.ReadWhileWeGetSequenceAsync(ClCr, ct.Token);

            var str = Encoding.UTF8.GetString(result.Span);
            
            Assert.AreEqual("TEST1"+(char)13+(char)10, str);

            ///////////
            
            ct = new CancellationTokenSource();
            
            trafficReader.IncomingTraffic.Enqueue((byte)'S');
            trafficReader.IncomingTraffic.Enqueue((byte)'T');
            trafficReader.IncomingTraffic.Enqueue((byte)'2');
            trafficReader.IncomingTraffic.Enqueue(13);
            trafficReader.IncomingTraffic.Enqueue(10);

            
            result = await tcpTrafficReader.ReadWhileWeGetSequenceAsync(ClCr, ct.Token);

            str = Encoding.UTF8.GetString(result.Span);
            
            Assert.AreEqual("TEST2"+(char)13+(char)10, str);
        }
        
    }
}