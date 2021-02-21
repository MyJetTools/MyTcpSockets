using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public static class TcpReaderExtensions
    {
        
        public static async ValueTask<ushort> ReadUShortAsync(this ITcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ushort), token);
            var result = BitConverter.ToUInt16(data.Span);
            reader.CommitReadData(data);
            return result;
        }     
        
        public static async ValueTask<short> ReadShortAsync(this ITcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(short),token);
            var result =  BitConverter.ToInt16(data.Span);
            reader.CommitReadData(data);
            return result;
        } 
        
        public static async ValueTask<int> ReadIntAsync(this ITcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(int), token);
            var result = BitConverter.ToInt32(data.Span);
            reader.CommitReadData(data);
            return result;
        }     
        
        public static async ValueTask<uint> ReadUIntAsync(this ITcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(uint), token);
            var result = BitConverter.ToUInt32(data.Span);
            reader.CommitReadData(data);
            return result;
        } 
        
        public static async ValueTask<long> ReadLongAsync(this ITcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(long), token);
            var result = BitConverter.ToInt64(data.Span);
            reader.CommitReadData(data);
            return result;            
        }     
        
        public static async ValueTask<ulong> ReadULongAsync(this ITcpDataReader reader, CancellationToken token)
        {
            var data = await reader.ReadAsyncAsync(sizeof(ulong), token);
            var result = BitConverter.ToUInt64(data.Span);
            reader.CommitReadData(data);
            return result;             
        }

        public static async ValueTask<string> ReadPascalStringAsync(this ITcpDataReader reader, CancellationToken token, Encoding encoding = null)
        {
            var strLen = await reader.ReadAndCommitByteAsync(token);
            var data = await reader.ReadAsyncAsync(strLen, token);
            encoding ??= Encoding.UTF8;
            var result = encoding.GetString(data.Span);
            reader.CommitReadData(data);
            return result;    
        }
        
        public static async ValueTask<string> ReadStringAsync(this ITcpDataReader reader, CancellationToken token, Encoding encoding = null)
        {
            var strLen = await reader.ReadIntAsync(token);
            var data = await reader.ReadAsyncAsync(strLen, token);
            encoding ??= Encoding.UTF8;
            var result = encoding.GetString(data.Span);
            reader.CommitReadData(data);
            return result;             
        }
        
        public static async ValueTask<byte[]> ReadByteArrayAsync(this ITcpDataReader reader, CancellationToken token)
        {
            var strLen = await reader.ReadIntAsync(token);
            var result= (await reader.ReadAsyncAsync(strLen, token)).ToArray();
            reader.CommitReadDataSize(strLen);
            return result;
        }
        
    }
}