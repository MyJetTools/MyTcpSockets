using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MyTcpSockets.Extensions;

namespace MyTcpSockets
{


    public interface ITcpSerializer<T>
    {
        ReadOnlyMemory<byte> Serialize(T data);
        
        int BufferSize { get; }
        ValueTask<T> DeserializeAsync(TcpDataReader reader);
    }
    
    
    
    
}


