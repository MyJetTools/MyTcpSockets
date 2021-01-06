using System;
using System.Collections.Generic;

namespace MyTcpSockets.Extensions
{
    public static class ReadOnlyMemoryUtils
    {


        public static ReadOnlyMemory<byte> ReadAndCopyTo(this LinkedList<ReadOnlyMemory<byte>> src, byte[] finalDestination)
        {
            if (src.Count == 0)
                return Array.Empty<byte>();

            var pos = 0;
            var remainsToCopy = finalDestination.Length;
            
            while (remainsToCopy>0 && src.Count>0)
            {
                
                var sizeToCopy = src.First.Value.Length > remainsToCopy? remainsToCopy : src.First.Value.Length;

                var source = src.First.Value.Span.Slice(0, sizeToCopy);
                
                var dest = finalDestination.AsSpan(pos, sizeToCopy);
                source.CopyTo(dest);

                if (sizeToCopy == src.First.Value.Length)
                {
                    src.RemoveFirst();
                }
                else
                    src.First.Value = src.First.Value.Slice(sizeToCopy, src.First.Value.Length - sizeToCopy);

                remainsToCopy -= sizeToCopy;
                pos += sizeToCopy;
            }

            return new ReadOnlyMemory<byte>(finalDestination, 0, pos);
        }
        
    }
}