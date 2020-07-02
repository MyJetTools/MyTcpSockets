using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public static class TcpReaderExtensions
    {
        public static async ValueTask<byte> ReadByteAsync(this TcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(1, ct);
            return data.Span[0];
        }
        
        public static async ValueTask<ushort> ReadUShortAsync(this TcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ushort), ct);
            return BitConverter20.ToUInt16(data.Span);
        }     
        
        public static async ValueTask<short> ReadShortAsync(this TcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(short), ct);
            return BitConverter20.ToInt16(data.Span);
        } 
        
        public static async ValueTask<int> ReadIntAsync(this TcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(int), ct);
            return BitConverter20.ToInt32(data.Span);
        }     
        
        public static async ValueTask<uint> ReadUIntAsync(this TcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(uint), ct);
            return BitConverter20.ToUInt32(data.Span);
        } 
        
        public static async ValueTask<long> ReadLongAsync(this TcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(long), ct);
            return BitConverter20.ToInt64(data.Span);
        }     
        
        public static async ValueTask<ulong> ReadULongAsync(this TcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ulong), ct);
            return BitConverter20.ToUInt64(data.Span);
        }

        public static async ValueTask<string> ReadPascalStringAsync(this TcpDataReader reader, CancellationToken ct, Encoding encoding = null)
        {
            var strLen = await reader.ReadByteAsync(ct);
            var data = await reader.ReadAsyncAsync(strLen, ct);
            
            if (encoding == null)
                encoding = Encoding.UTF8;
            
            return encoding.GetString(data.ToArray());
        }
        
        public static async ValueTask<string> ReadStringAsync(this TcpDataReader reader, CancellationToken ct, Encoding encoding = null)
        {
            var strLen = await reader.ReadIntAsync(ct);
            var data = await reader.ReadAsyncAsync(strLen,ct);
            
            if (encoding == null)
                encoding = Encoding.UTF8;
            
            return encoding.GetString(data.ToArray());
        }
        
        public static async ValueTask<ReadOnlyMemory<byte>> ReadByteArrayAsync(this TcpDataReader reader, CancellationToken ct)
        {
            var strLen = await reader.ReadIntAsync(ct);
            return await reader.ReadAsyncAsync(strLen, ct);
        }
        
    }
}