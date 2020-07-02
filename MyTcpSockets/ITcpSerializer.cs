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
        IAsyncEnumerable<T> DeserializeAsync(TcpDataReader reader, CancellationToken ct);
    }
    
    
    
    
}


