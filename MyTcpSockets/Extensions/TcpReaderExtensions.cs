using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public static class TcpReaderExtensions
    {
        public static async ValueTask<byte> ReadByteAsync(this TcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(1, token);
            return data.Span[0];
        }
        
        public static async ValueTask<ushort> ReadUShortAsync(this TcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ushort), token);
            return BitConverter.ToUInt16(data.Span);
        }     
        
        public static async ValueTask<short> ReadShortAsync(this TcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(short),token);
            return BitConverter.ToInt16(data.Span);
        } 
        
        public static async ValueTask<int> ReadIntAsync(this TcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(int), token);
            return BitConverter.ToInt32(data.Span);
        }     
        
        public static async ValueTask<uint> ReadUIntAsync(this TcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(uint), token);
            return BitConverter.ToUInt32(data.Span);
        } 
        
        public static async ValueTask<long> ReadLongAsync(this TcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(long), token);
            return BitConverter.ToInt64(data.Span);
        }     
        
        public static async ValueTask<ulong> ReadULongAsync(this TcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ulong), token);
            return BitConverter.ToUInt64(data.Span);
        }

        public static async ValueTask<string> ReadPascalStringAsync(this TcpDataReader reader, CancellationToken token, Encoding encoding = null)
        {
            var strLen = await reader.ReadByteAsync(token);
            var data = await reader.ReadAsyncAsync(strLen, token);
            encoding ??= Encoding.UTF8;
            return encoding.GetString(data.Span);
        }
        
        public static async ValueTask<string> ReadStringAsync(this TcpDataReader reader, CancellationToken token, Encoding encoding = null)
        {
            var strLen = await reader.ReadIntAsync(token);
            var data = await reader.ReadAsyncAsync(strLen, token);
            encoding ??= Encoding.UTF8;
            return encoding.GetString(data.Span);
        }
        
        public static async ValueTask<ReadOnlyMemory<byte>> ReadByteArrayAsync(this TcpDataReader reader, CancellationToken token)
        {
            var strLen = await reader.ReadIntAsync(token);
            return await reader.ReadAsyncAsync(strLen, token);
        }
        
    }
}