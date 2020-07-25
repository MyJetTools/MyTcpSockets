using System;

namespace MyTcpSockets.Extensions
{
    public class TcpDataPiece
    {
        public TcpDataPiece(byte[] data, int length)
        {
            Data = data;
            DataLength = length;
            Count = length;
        }
        
        public TcpDataPiece(byte[] data)
        {
            Data = data;
            DataLength = data.Length;
            Count = DataLength;
        }
 

        public ReadOnlyMemory<byte> GetAsMuchAsPossible(int dataToGet)
        {
            if (Count == 0)
                return Array.Empty<byte>();

            if (dataToGet > Count)
                dataToGet = Count;
            
            try
            {
                return Data.AsMemory(StartIndex, dataToGet);
            }
            finally
            {
                StartIndex += dataToGet;
                Count -= dataToGet;
            }
        }

        public bool IsEmpty => StartIndex == DataLength;

        public byte[] Data { get; }
        public int DataLength { get; }

        public int StartIndex { get; private set; }
        public int Count { get; private set; }
        

    }

}