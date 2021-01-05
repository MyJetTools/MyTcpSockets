using System;
using System.Collections.Generic;

namespace MyTcpSockets.Extensions
{
    public class TcpDataPiece
    {
        private readonly byte[] _data;

        public int ReadyToReadStart { get; internal set; }
        public int ReadyToReadSize { get; private set; }

        public int WriteIndex => ReadyToReadStart + ReadyToReadSize;

        public int WriteSize => _data.Length - WriteIndex;

        internal IEnumerable<byte> Iterate()
        {
            for (var i = ReadyToReadStart; i < ReadyToReadStart + ReadyToReadSize; i++)
            {
                yield return _data[i];
            }
        }
        
        public TcpDataPiece(int bufferSize)
        {
            _data = new byte[bufferSize];
        }


        internal void Gc()
        {
            if (ReadyToReadStart == 0)
                return;

            var spanSrc = _data.AsSpan(ReadyToReadStart, ReadyToReadSize);
            var destSrc = _data.AsSpan(0, ReadyToReadSize);

            spanSrc.CopyTo(destSrc);
            ReadyToReadStart = 0;
        }

        public Memory<byte> AllocateBufferToWrite()
        {
            var writeSize = WriteSize;
            
            return writeSize == 0 ? null 
                : new Memory<byte>(_data, WriteIndex, writeSize);
        }

        public (byte[] buffer, int start, int len) AllocateBufferToWriteLegacy()
        {
            var writeSize = WriteSize;
            return writeSize == 0 
                ? (_data, 0,0) 
                : (_data, WriteIndex, writeSize);
        }
        
        public void CommitWrittenData(int size)
        {
            ReadyToReadSize += size;
        }

        public ReadOnlyMemory<byte> Read(int size)
        {
            return ReadyToReadSize < size 
                ? new ReadOnlyMemory<byte>(_data, ReadyToReadStart, ReadyToReadSize) 
                : new ReadOnlyMemory<byte>(_data, ReadyToReadStart, size);
        }

        public byte ReadByte()
        {
            return _data[ReadyToReadStart];
        }

        public int CommitReadData(int size)
        {
            var sizeToCommit = size >= ReadyToReadSize ? ReadyToReadSize : size;
            
            ReadyToReadStart += sizeToCommit;
            ReadyToReadSize -= sizeToCommit;
            return size - sizeToCommit;
        }

    }

}