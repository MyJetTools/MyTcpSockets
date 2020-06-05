using System;

namespace MyTcpSockets
{
    public class SocketStatistic
    {

        public DateTime ConnectionTime { get; } = DateTime.UtcNow;
        public DateTime LastSendTime { get; private set; }= DateTime.UtcNow;
        public DateTime LastReceiveTime { get; private set; }= DateTime.UtcNow;
        
        public DateTime DisconnectionTime { get; private set; } = DateTime.UtcNow;
        
        public long SentPerSecond { get; private set; }
        
        public long ReceivedPerSecond { get; private set; }


        private long _sendPerSecondIntervalData;
        private long _receivePerSecondIntervalData;

        internal void EachSecondTimer()
        {
            SentPerSecond = _sendPerSecondIntervalData;
            _sendPerSecondIntervalData = 0;

            ReceivedPerSecond = _receivePerSecondIntervalData;
            _receivePerSecondIntervalData = 0;
        }

        internal void WeHaveSendEvent(long amount)
        {
            Sent += amount;
            _sendPerSecondIntervalData += amount;
            LastSendTime= DateTime.UtcNow;
        }

        internal void WeHaveReceiveEvent(long amount)
        {
            Received += amount;
            _receivePerSecondIntervalData += amount;
            LastReceiveTime = DateTime.UtcNow;
        }

        internal void WeHaveDisconnect()
        {
            DisconnectionTime = DateTime.UtcNow;;
        }
        
        public long Sent { get; private set; }

        public long Received { get; private set; }


    }


}
