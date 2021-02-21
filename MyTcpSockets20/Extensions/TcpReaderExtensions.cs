using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public static class TcpReaderExtensions
    {
        
        public static async ValueTask<ushort> ReadUShortAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ushort), ct);
            var result =  BitConverter20.ToUInt16(data.Span);
            reader.CommitReadData(data);
            return result;
        }     
        
        public static async ValueTask<short> ReadShortAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(short), ct);
            var result = BitConverter20.ToInt16(data.Span);
            reader.CommitReadData(data);
            return result;
        } 
        
        public static async ValueTask<int> ReadIntAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(int), ct);
            var result = BitConverter20.ToInt32(data.Span);
            reader.CommitReadData(data);
            return result;
        }     
        
        public static async ValueTask<uint> ReadUIntAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(uint), ct);
            var result = BitConverter20.ToUInt32(data.Span);
            reader.CommitReadData(data);
            return result;
        } 
        
        public static async ValueTask<long> ReadLongAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(long), ct);
            var result = BitConverter20.ToInt64(data.Span);
            reader.CommitReadData(data);
            return result;
        }     
        
        public static async ValueTask<ulong> ReadULongAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ulong), ct);
            var result = BitConverter20.ToUInt64(data.Span);
            reader.CommitReadData(data);
            return result;
        }

        public static async ValueTask<string> ReadPascalStringAsync(this ITcpDataReader reader, CancellationToken ct, Encoding encoding = null)
        {
            var strLen = await reader.ReadAndCommitByteAsync(ct);
            var data = await reader.ReadAsyncAsync(strLen, ct);
            
            if (encoding == null)
                encoding = Encoding.UTF8;
            
            var result =  encoding.GetString(data.AsArray());
            reader.CommitReadData(data);
            return result;
        }
        
        public static async ValueTask<string> ReadStringAsync(this ITcpDataReader reader, CancellationToken ct, Encoding encoding = null)
        {
            var strLen = await reader.ReadIntAsync(ct);
            var data = await reader.ReadAsyncAsync(strLen,ct);
            
            if (encoding == null)
                encoding = Encoding.UTF8;
            
            var result = encoding.GetString(data.AsArray());
            reader.CommitReadData(data);
            return result;
        }
        
        public static async ValueTask<byte[]> ReadByteArrayAsync(this ITcpDataReader reader, CancellationToken ct)
        {
            var strLen = await reader.ReadIntAsync(ct);
            var data = await reader.ReadAsyncAsync(strLen, ct);
            var result = data.AsArray();
            reader.CommitReadData(data);
            return result;
        }
        
    }
}