using System;
using System.IO;
using System.Threading.Tasks;

namespace MyTcpSockets
{
    public static class BackwardCompatability
    {

        public static Task WriteAsync(this Stream stream, in ReadOnlyMemory<byte> data)
        {
            var array = data.ToArray();
            return stream.WriteAsync(array, 0, array.Length);
        }
        
    }
}