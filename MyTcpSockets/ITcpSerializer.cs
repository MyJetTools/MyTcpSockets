using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;

namespace MyTcpSockets
{


    public interface ITcpSerializer<T>
    {
        ReadOnlyMemory<byte> Serialize(T data);
        
        int BufferSize { get; }
        
        #if NETSTANDARD2_1
        IAsyncEnumerable<T> DeserializeAsync(TcpDataReader reader, CancellationToken ct);
        #else
        ValueTask<T> DeserializeAsync(TcpDataReader reader, CancellationToken ct);
        #endif

    }
    
    
    
    
}


