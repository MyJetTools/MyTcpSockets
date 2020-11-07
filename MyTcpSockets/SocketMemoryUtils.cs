using System;

namespace MyTcpSockets
{
    public static class SocketMemoryUtils
    {
        public static Func<int, byte[]> AllocateByteArray = size =>new byte[size];
    }
}