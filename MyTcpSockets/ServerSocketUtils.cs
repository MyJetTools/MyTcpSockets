using System;

namespace MyTcpSockets
{
    public static class ServerSocketUtils
    {

        public static bool IsServerSocketDead(this ITcpContext tcpContext, DateTime now, 
            TimeSpan receiveTimeOut, TimeSpan initTimeOut)
        {

            if (tcpContext.Inited)
            {
                if (now - tcpContext.SocketStatistic.LastReceiveTime > receiveTimeOut)
                    return true; 
                
                if (now - tcpContext.SocketStatistic.LastSendTime > receiveTimeOut)
                    return true; 
            }
            else
            {
                if (now - tcpContext.SocketStatistic.LastReceiveTime > initTimeOut)
                    return true;
            }

            return false;
        }
        
    }
}