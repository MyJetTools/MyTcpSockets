using System;
using System.Collections.Generic;
using System.IO;

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

        private static int GetSizeByMarker(this IEnumerable<byte> src, IReadOnlyList<byte> marker)
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