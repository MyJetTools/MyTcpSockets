using System;

namespace MyTcpSockets
{
    public static class OutPackageCompiler
    {
        public static ReadOnlyMemory<byte> CompileDataToSend(this ITcpContext tcpContext, byte[] reusableBuffer)
        {

            if (tcpContext.DataToSend.Count == 1)
            {
                var result = tcpContext.DataToSend.Dequeue();
                return result;
            }

            var sendSize = 0;
            
            
            while (tcpContext.DataToSend.Count>0)
            {
                var nextPacket = tcpContext.DataToSend.Peek();
                
                if (nextPacket.Length > reusableBuffer.Length && sendSize == 0)
                {
                    var result = tcpContext.DataToSend.Dequeue();
                    return result;
                }
                
                if (nextPacket.Length + sendSize > reusableBuffer.Length)
                    break;

                nextPacket = tcpContext.DataToSend.Dequeue();
                
                nextPacket.CopyTo(reusableBuffer.AsMemory(sendSize, nextPacket.Length));

                sendSize += nextPacket.Length;
            }

            return new ReadOnlyMemory<byte>(reusableBuffer, 0,sendSize);

        }
    }
}