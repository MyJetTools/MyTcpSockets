using System;
using System.Collections.Generic;

namespace MyTcpSockets.Extensions
{
    public static class TcpDataPieceUtils
    {

        private static ReadOnlyMemory<byte> GetAsSingleElement(this List<TcpDataPiece> dataList, int size)
        {
            var firstElement = dataList[0];
            var resultAsSingleElement = firstElement.GetAsMuchAsPossible(size);
                
            if (firstElement.IsEmpty)
                dataList.RemoveAt(0);

            return resultAsSingleElement;
        }

        private static byte[] CompileResult(this List<TcpDataPiece> dataList, int size)
        {
            var result = SocketMemoryUtils.AllocateByteArray(size);
            var position = 0;
            var remainSize = size;
            
            while (remainSize >0)
            {
                var itm = dataList[0];

                var theChunk = itm.GetAsMuchAsPossible(remainSize);
                theChunk.CopyTo(result.AsMemory(position, theChunk.Length));

                position += theChunk.Length;
                remainSize -= theChunk.Length;
                
                if (itm.IsEmpty)
                    dataList.RemoveAt(0);
            }

            return result; 
        }
        
        
        public static ReadOnlyMemory<byte> GetAndClean(this List<TcpDataPiece> dataList, int size)
        {

            var firstElement = dataList[0];
            return firstElement.Count >= size 
                ? dataList.GetAsSingleElement(size) 
                : dataList.CompileResult(size);
        }
        
    }
}