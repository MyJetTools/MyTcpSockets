using System;
using System.Collections.Generic;
using System.Linq;

namespace MyTcpSockets.Extensions
{
    public static class TcpDataPieceUtils
    {

        internal static (bool hasResult, byte result) TryReadByte(this List<TcpDataPiece> dataList)
        {
            if (dataList[0].ReadyToReadSize == 0)
                return (false, 0);

            return (true, dataList[0].ReadByte());
        }

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

        private static int ReadyToReadSize(this IEnumerable<TcpDataPiece> src)
        {
            return src.Sum(itm => itm.ReadyToReadSize);
        }


        internal static IEnumerable<byte> GetAll(this IEnumerable<TcpDataPiece> src)
        {
            return src.SelectMany(itm => itm.Iterate());
        }

        public static int GetSizeByMarker(this IEnumerable<TcpDataPiece> src, IReadOnlyList<byte> marker)
        {
            var pos = 0;
            var markerPos = 0;
            foreach (var b in src.GetAll())
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

            return-1;
        }

        
    }
}