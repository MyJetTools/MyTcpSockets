using System;

namespace MyTcpSockets
{
    public static class ServerSocketUtils
    {

        public static bool IsServerSocketDead(this ITcpContext tcpContext, DateTime now, 
            TimeSpan activityTimeOut, TimeSpan initTimeOut)
        {

            if (tcpContext.Inited)
            {
                if (now - tcpContext.SocketStatistic.LastReceiveTime > activityTimeOut && now - tcpContext.SocketStatistic.LastSendTime > activityTimeOut)
                    return true; 
                
            }
            else
            {
                if (now - tcpContext.SocketStatistic.LastReceiveTime > initTimeOut && now - tcpContext.SocketStatistic.LastSendTime > activityTimeOut)
                    return true;
            }

            return false;
        }
        
    }
}