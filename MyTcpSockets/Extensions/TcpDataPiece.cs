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
        public int AllocatedToRead { get; private set; }


        public bool FullOfData => ReadyToReadSize == _data.Length;

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
            Gc();
            
            var writeSize = WriteSize;

            AllocatedToWrite += writeSize;
            
            return writeSize == 0 ? null 
                : new Memory<byte>(_data, WriteIndex, writeSize);
        }

        public (byte[] buffer, int start, int len) AllocateBufferToWriteLegacy()
        {
            Gc();
            
            var writeSize = WriteSize;
            
            AllocatedToWrite += writeSize;
            
            return writeSize == 0 
                ? (_data, 0,0) 
                : (_data, WriteIndex, writeSize);
        }
        
        public void CommitWrittenData(int size)
        {
            if (size > AllocatedToWrite)
                throw new Exception($"You can not commit written amount {size} because allocated amount was {AllocatedToWrite}");
            
            ReadyToReadSize += size;
            AllocatedToWrite = 0;
        }

        public ReadOnlyMemory<byte> TryRead(int size)
        {
            Gc();
            AllocatedToRead = ReadyToReadSize < size ? 0 : size;
            
            return  AllocatedToRead == 0
                ? new ReadOnlyMemory<byte>() 
                : new ReadOnlyMemory<byte>(_data, ReadyToReadStart, AllocatedToRead);
        }


        public ReadOnlyMemory<byte> GetWhateverWeHave()
        {
            Gc();
            AllocatedToRead = ReadyToReadSize;
            
            return AllocatedToRead == 0
                ? new ReadOnlyMemory<byte>() 
                : new ReadOnlyMemory<byte>(_data, ReadyToReadStart, AllocatedToRead);
        }

        public byte ReadByte()
        {
            AllocatedToRead = 1;
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

            AllocatedToRead = 0;
            
            ReadyToReadStart += size;
            ReadyToReadSize -= size;

            if (ReadyToReadSize == 0 && AllocatedToWrite == 0)
                ReadyToReadSize = 0;
            
        }

    }

}
