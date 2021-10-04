using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public static class TcpReaderExtensions
    {
        
        public static async ValueTask<ushort> ReadUShortAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadBytesAsync(sizeof(ushort), ct);
            return BitConverter20.ToUInt16(data.Span);
        }     
        
        public static async ValueTask<short> ReadShortAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadBytesAsync(sizeof(short), ct);
            return BitConverter20.ToInt16(data.Span);
        } 
        
        public static async ValueTask<int> ReadIntAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadBytesAsync(sizeof(int), ct);
            return BitConverter20.ToInt32(data.Span);
        }     
        
        public static async ValueTask<uint> ReadUIntAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadBytesAsync(sizeof(uint), ct);
            return BitConverter20.ToUInt32(data.Span);
        } 
        
        public static async ValueTask<long> ReadLongAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadBytesAsync(sizeof(long), ct);
            return BitConverter20.ToInt64(data.Span);
        }     
        
        public static async ValueTask<ulong> ReadULongAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadBytesAsync(sizeof(ulong), ct);
            return BitConverter20.ToUInt64(data.Span);
        }

        public static async ValueTask<string> ReadPascalStringAsync(this ITcpDataReader reader, CancellationToken ct, Encoding encoding = null)
        {
            var strLen = await reader.ReadByteAsync(ct);
            var data = await reader.ReadBytesAsync(strLen, ct);
            
            if (encoding == null)
                encoding = Encoding.UTF8;
            
            return encoding.GetString(data.ToArray());
        }
        
        public static async ValueTask<string> ReadStringAsync(this ITcpDataReader reader, CancellationToken ct, Encoding encoding = null)
        {
            var strLen = await reader.ReadIntAsync(ct);
            var data = await reader.ReadBytesAsync(strLen,ct);
            
            if (encoding == null)
                encoding = Encoding.UTF8;
            
            return encoding.GetString(data.ToArray());
        }
        
        public static async ValueTask<byte[]> ReadByteArrayAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var strLen = await reader.ReadIntAsync(ct);
            var data = await reader.ReadBytesAsync(strLen, ct);
            return data.ToArray();
        }
        
    }
}