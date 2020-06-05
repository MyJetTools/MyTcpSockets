using System;
using System.Text;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public static class TcpReaderExtensions
    {
        public static async ValueTask<byte> ReadByteAsync(this TcpDataReader reader)
        {
            var data = await reader.ReadAsyncAsync(1);
            return data.Span[0];
        }
        
        public static async ValueTask<ushort> ReadUShortAsync(this TcpDataReader reader)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ushort));
            return BitConverter.ToUInt16(data.Span);
        }     
        
        public static async ValueTask<short> ReadShortAsync(this TcpDataReader reader)
        {
            var data = await reader.ReadAsyncAsync(sizeof(short));
            return BitConverter.ToInt16(data.Span);
        } 
        
        public static async ValueTask<int> ReadIntAsync(this TcpDataReader reader)
        {
            var data = await reader.ReadAsyncAsync(sizeof(int));
            return BitConverter.ToInt32(data.Span);
        }     
        
        public static async ValueTask<uint> ReadUIntAsync(this TcpDataReader reader)
        {
            var data = await reader.ReadAsyncAsync(sizeof(uint));
            return BitConverter.ToUInt32(data.Span);
        } 
        
        public static async ValueTask<long> ReadLongAsync(this TcpDataReader reader)
        {
            var data = await reader.ReadAsyncAsync(sizeof(long));
            return BitConverter.ToInt64(data.Span);
        }     
        
        public static async ValueTask<ulong> ReadULongAsync(this TcpDataReader reader)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ulong));
            return BitConverter.ToUInt64(data.Span);
        }

        public static async ValueTask<string> ReadPascalStringAsync(this TcpDataReader reader, Encoding encoding = null)
        {
            var strLen = await reader.ReadByteAsync();
            var data = await reader.ReadAsyncAsync(strLen);
            encoding ??= Encoding.UTF8;
            return encoding.GetString(data.Span);
        }
        
        public static async ValueTask<string> ReadStringAsync(this TcpDataReader reader, Encoding encoding = null)
        {
            var strLen = await reader.ReadIntAsync();
            var data = await reader.ReadAsyncAsync(strLen);
            encoding ??= Encoding.UTF8;
            return encoding.GetString(data.Span);
        }
        
        public static async ValueTask<ReadOnlyMemory<byte>> ReadByteArrayAsync(this TcpDataReader reader)
        {
            var strLen = await reader.ReadIntAsync();
            return await reader.ReadAsyncAsync(strLen);
        }
        
    }
}