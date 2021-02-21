using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyTcpSockets.Extensions
{
    
    public struct TcpReadResult
    {
        public ReadOnlyMemory<byte> UncommittedMemory { get;  }
        
        public byte[] CommittedMemory { get; }

        public TcpReadResult(ReadOnlyMemory<byte> uncommittedMemory, byte[] committedMemory)
        {
            UncommittedMemory = uncommittedMemory;
            CommittedMemory = committedMemory;
        }

        public int Length => CommittedMemory?.Length ?? UncommittedMemory.Length;


        public ReadOnlySpan<byte> Span => CommittedMemory == null ? UncommittedMemory.Span : CommittedMemory.AsSpan();

        public byte[] AsArray()
        {
            return CommittedMemory ?? UncommittedMemory.ToArray();
        }

    }
    
    
    
    public static class TcpDataPieceUtils
    {

        internal static (bool hasResult, byte result) TryReadByte(this List<TcpDataPiece> dataList)
        {
            if (dataList[0].ReadyToReadSize == 0)
                return (false, 0);

            return (true, dataList[0].ReadByte());
        }

        
        /*
        internal static ReadOnlyMemory<byte> TryCompilePackage(this List<TcpDataPiece> dataList, int size)
        {
            if (size == 0)
                return Array.Empty<byte>();
            try
            {
                
                var readyToReadSize = dataList.ReadyToReadSize();

                if (readyToReadSize < size)
                    return Array.Empty<byte>();


                var memChunk = dataList[0].Read(size);

                if (memChunk.Length == size)
                {
                    return memChunk;
                }

                var result = SocketMemoryUtils.AllocateByteArray(size);
                
                memChunk.CopyTo(result.AsMemory(0, memChunk.Length));
                var pos = memChunk.Length;
                size -= memChunk.Length;


                for (var i = 1; i < dataList.Count; i++)
                {
                    memChunk = dataList[i].Read(size);
                    memChunk.CopyTo(result.AsMemory(pos, memChunk.Length));  
                    pos += memChunk.Length;
                    size -= memChunk.Length;
                }

                return result;
            }
            finally
            {
                while (dataList.Count>1)
                {
                    if (dataList[0].ReadyToReadSize >0)
                        break;
                    
                    dataList.RemoveAt(0);
                }
              
            }
                
        }
        */
        
        private static int ReadyToReadSize(this IEnumerable<TcpDataPiece> src)
        {
            return src.Sum(itm => itm.ReadyToReadSize);
        }


        internal static IEnumerable<byte> GetAll(this IEnumerable<TcpDataPiece> src)
        {
            return src.SelectMany(itm => itm.Iterate());
        }


        private static IEnumerable<byte> Iterate(this MemoryStream stream, TcpDataPiece src)
        {
            var buffer = stream.GetBuffer();
            for (var i=0; i<stream.Length; i++)
            {
                yield return buffer[i];
            }

            foreach (var b in src.Iterate())
            {
                yield return b;
            }
            
        }
        

        public static int GetSizeByMarker(this MemoryStream stream, TcpDataPiece src, IReadOnlyList<byte> marker)
        {
            return stream.Iterate(src).GetSizeByMarker(marker);
        }

        public static int GetSizeByMarker(this TcpDataPiece src, IReadOnlyList<byte> marker)
        {
            return src.Iterate().GetSizeByMarker(marker);
        }

        public static int GetSizeByMarker(this IEnumerable<byte> src, IReadOnlyList<byte> marker)
        {
            var pos = 0;
            var markerPos = 0;
            foreach (var b in src)
            {
                if (marker[markerPos] == b)
                {
                    markerPos++;
                    if (markerPos == marker.Count)
                        return pos+1;
                }
                else
                    markerPos = 0;
                pos++;
            }

            return -1;
        }

        
    }
}