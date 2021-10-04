using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    
    public class TcpDataReader : ITcpDataReader
    {

        private readonly byte[] _buffer;

        private readonly TcpDataReaderAsSequence _readerAsSequence;
        
        
        private readonly IIncomingTcpTrafficReader _incomingTrafficReader;
        public TcpDataReader(IIncomingTcpTrafficReader incomingTrafficReader, int reusableBuffer)
        {
            _incomingTrafficReader = incomingTrafficReader;
            _buffer = SocketMemoryUtils.AllocateByteArray(reusableBuffer);
            _readerAsSequence = new TcpDataReaderAsSequence(_buffer);
        }

        private Memory<byte> AllocateBufferToRead(int size)
        {

            if (_buffer.Length >= size)
            {
                return _buffer;
            }

            return SocketMemoryUtils.AllocateByteArray(size);
        }

        public ValueTask<byte> ReadByteAsync(CancellationToken token)
        {
            return _incomingTrafficReader.ReadByteAsync(token);
        }

        public async ValueTask<ReadOnlyMemory<byte>> ReadBytesAsync(int size, CancellationToken token)
        {

            var buffer = AllocateBufferToRead(size);

            var pos = 0;
            while (pos < size)
            {
                var readAmount = await _incomingTrafficReader.ReadBytesAsync(buffer.Slice(pos, size - pos), token);
                pos += readAmount;
            }

            return buffer.Slice(0, size);
        }


        private async Task<ReadOnlyMemory<byte>> ReadFromSocket(byte[] sequence, CancellationToken token)
        {
            var writeMemory = _readerAsSequence.GetNextMemoryToWrite();
            while (writeMemory.Length > 0)
            {

                
                var writtenAmount = await _incomingTrafficReader.ReadBytesAsync(writeMemory, token);
                _readerAsSequence.AppendWritten(writtenAmount);

                var nextResult = _readerAsSequence.GetNextPackageIfExists(sequence);
                
                if (nextResult.Length>0)
                    return nextResult;
                
                writeMemory = _readerAsSequence.GetNextMemoryToWrite();
            }  
            
            throw new Exception($"Max Amount of the Incoming message is limited to {_buffer.Length}");
        }

        public ValueTask<ReadOnlyMemory<byte>> ReadWhileWeGetSequenceAsync(byte[] sequence, CancellationToken token)
        {
            var nextResult = _readerAsSequence.GetNextPackageIfExists(sequence);

            if (nextResult.Length > 0)
                return new ValueTask<ReadOnlyMemory<byte>>(nextResult);

            _readerAsSequence.CompactBuffer();

            return new ValueTask<ReadOnlyMemory<byte>>(ReadFromSocket(sequence, token));




     
        }
        
    }
   

    
}