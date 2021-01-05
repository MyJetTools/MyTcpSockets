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
        ValueTask<T> DeserializeAsync(ITcpReader reader, CancellationToken ct);

    }
    
}


