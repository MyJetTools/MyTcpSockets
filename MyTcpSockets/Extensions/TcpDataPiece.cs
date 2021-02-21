using System;
using System.Collections.Generic;

namespace MyTcpSockets.Extensions
{
    public class TcpDataPiece
    {
        private readonly byte[] _data;

        public int ReadyToReadStart { get; private set; }
        public int ReadyToReadSize { get; private set; }

        public int WriteIndex => ReadyToReadStart + ReadyToReadSize;

        public int WriteSize => _data.Length - WriteIndex;
        
        public int AllocatedToWrite { get; private set; }

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

        public int BufferSize => _data.Length;


        internal void Gc()
        {
            if (AllocatedToWrite >0)
                return;
            
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

            AllocatedToWrite += writeSize;
            
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
            AllocatedToWrite -= size;
        }

        public ReadOnlyMemory<byte> TryRead(int size)
        {
            return ReadyToReadSize < size 
                ? new ReadOnlyMemory<byte>() 
                : new ReadOnlyMemory<byte>(_data, ReadyToReadStart, size);
        }


        public ReadOnlyMemory<byte> GetWhateverWeHave()
        {
            return ReadyToReadSize == 0
                ? new ReadOnlyMemory<byte>() 
                : new ReadOnlyMemory<byte>(_data, ReadyToReadStart, ReadyToReadSize);
        }

        public byte ReadByte()
        {
            return _data[ReadyToReadStart];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        /// <returns>Available size after commit</returns>
        public void CommitReadData(int size)
        {
            if (size > ReadyToReadSize)
                throw new Exception($"You try to commit {size} which iS grater size than {ReadyToReadSize}");
            
            ReadyToReadStart += size;
            ReadyToReadSize -= size;

            if (ReadyToReadSize == 0 && AllocatedToWrite == 0)
                ReadyToReadSize = 0;
        }

    }

}