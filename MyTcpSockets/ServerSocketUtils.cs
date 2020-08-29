using System;

namespace MyTcpSockets
{
    public static class ServerSocketUtils
    {

        public static bool IsServerSocketDead(this ITcpContext tcpContext, DateTime now, 
            TimeSpan receiveTimeOut, TimeSpan initTimeOut)
        {

            if (now - tcpContext.SocketStatistic.LastReceiveTime > receiveTimeOut)
                return true;
            
            if (!tcpContext.Inited && now - tcpContext.SocketStatistic.LastSendTime > initTimeOut)
                return true;

            return false;
        }
        
    }
}