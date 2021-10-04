using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public interface IIncomingTcpTrafficReader
    {
        ValueTask<byte> ReadByteAsync(CancellationToken token);
        ValueTask<int> ReadBytesAsync(Memory<byte> buffer, CancellationToken token);
    }


    public class IncomingTcpClientTrafficReader : IIncomingTcpTrafficReader
    {
        private readonly SocketStatistic _socketStatistic;

        private readonly byte[] _byteBuffer = {0};

        private readonly NetworkStream _networkStream;

        public IncomingTcpClientTrafficReader(TcpClient tcpClient, SocketStatistic socketStatistic)
        {
            _socketStatistic = socketStatistic;
            _networkStream = tcpClient.GetStream();
        }
        
        public async ValueTask<byte> ReadByteAsync(CancellationToken token)
        {

            var result = await _networkStream.ReadAsync(_byteBuffer, 0, 1, token);

            if (result <= 0)
            {
                throw new Exception($"Disconnected. Read byte result is {result}");
            }

            _socketStatistic.WeHaveReceiveEvent(1);
            return _byteBuffer[0];
        }

        public async ValueTask<int> ReadBytesAsync(Memory<byte> buffer, CancellationToken token)
        {
            var bytesToWrite = new byte[buffer.Length];
            var result = await _networkStream.ReadAsync(bytesToWrite, 0, buffer.Length, token);

            if (result <= 0)
            {
                throw new Exception($"Disconnected. Read ByteArray result is {result}");
            }

            _socketStatistic.WeHaveReceiveEvent(result);
            
            bytesToWrite.CopyTo(buffer);

            return result;
        }
    }
}