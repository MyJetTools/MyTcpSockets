using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public static class StreamReaders
    {
        public static async ValueTask ReadUntilSpecificSymbolAsync(this Stream stream, List<byte> list, byte symbol)
        {
            byte b;
            do
            {
                b = await stream.ReadByteAsync();
                list.Add(b);

            } while (b != symbol);
        }

        public static async Task<List<byte>> ReadUntilSpecificSymbolAsync(this Stream stream, byte symbol)
        {
            var result = new List<byte>();
            await stream.ReadUntilSpecificSymbolAsync(result, symbol);
            return result;
        }

        public static async Task<byte[]> CreateArrayAndReaderItFromSocketAsync(this Stream stream, int size)
        {
            var result = SocketMemoryUtils.AllocateByteArray(size);
            var read = 0;

            while (read < size)
            {
                var readChunkSize = await stream.ReadAsync(result, read, result.Length - read);

                if (readChunkSize == 0)
                    throw new Exception("Disconnected");

                read += readChunkSize;

            }

            return result;
        }

        public static async ValueTask<ReadOnlyMemory<byte>> ReadAsMuchAsPossibleAsync(this Stream stream, int bufferSize)
        {
            var buffer = SocketMemoryUtils.AllocateByteArray(bufferSize);

            var readSize = await stream.ReadAsync(buffer, 0, bufferSize);

            if (readSize == 0)
                throw new Exception("Disconnected");

            return new ReadOnlyMemory<byte>(buffer, 0, readSize);

        }

        public static async ValueTask<ReadOnlyMemory<byte>> ReadAsMuchAsPossibleAsync(this Stream stream, byte[] buffer)
        {
            var readSize = await stream.ReadAsync(buffer, 0, buffer.Length);

            if (readSize == 0)
                throw new Exception("Disconnected");

            return new ReadOnlyMemory<byte>(buffer, 0, readSize);
        }


        public static IReadOnlyList<byte> ToPascalStringArray(this string data)
        {
            var arrayToAdd = Encoding.UTF8.GetBytes(data);
            var list = new List<byte>(arrayToAdd.Length + 1) { (byte)arrayToAdd.Length };
            list.AddRange(arrayToAdd);
            return list;
        }

        public static async ValueTask<byte> ReadByteAsync(this Stream stream)
        {
            var result = await stream.CreateArrayAndReaderItFromSocketAsync(1);
            return result[0];
        }

        public static async ValueTask<ushort> ReadUshortAsync(this Stream stream)
        {
            var bytes = await CreateArrayAndReaderItFromSocketAsync(stream, 2);
            return BitConverter.ToUInt16(bytes);
        }

        public static async ValueTask<ushort> ReadShortAsync(this Stream stream)
        {
            var bytes = await CreateArrayAndReaderItFromSocketAsync(stream, 2);
            return BitConverter.ToUInt16(bytes);
        }


        public static async ValueTask<uint> ReadUintAsync(this Stream stream)
        {
            var bytes = await CreateArrayAndReaderItFromSocketAsync(stream, 4);
            return BitConverter.ToUInt32(bytes);
        }

        public static async ValueTask<int> ReadIntAsync(this Stream stream)
        {
            var bytes = await CreateArrayAndReaderItFromSocketAsync(stream, 4);
            return BitConverter.ToInt32(bytes);
        }

        public static async ValueTask<ulong> ReadUlongAsync(this Stream stream)
        {
            var bytes = await CreateArrayAndReaderItFromSocketAsync(stream, 8);
            return BitConverter.ToUInt64(bytes);
        }

        public static async ValueTask<long> ReadLongAsync(this Stream stream)
        {
            var bytes = await CreateArrayAndReaderItFromSocketAsync(stream, 8);
            return BitConverter.ToInt64(bytes);
        }


        /// <summary>
        /// Read string in PascalFormat. 1 byte - length. and the string
        /// </summary>
        public static async Task<(string result, int dataSize)> ReadPascalStringAsync(this Stream stream, Encoding encoding = null)
        {
            var strLen = await stream.ReadByteAsync();

            if (strLen == 0)
                return (string.Empty, 1);

            var data = await stream.CreateArrayAndReaderItFromSocketAsync(strLen);

            return (Encoding.UTF8.GetString(data), data.Length + 1);
        }

        /// <summary>
        /// Read string with 4 bytes - length and the rest is the string
        /// </summary>
        public static async ValueTask<(string result, int dataSize)> ReadStringAsync(this Stream stream, Encoding encoding = null)
        {
            var strLen = await stream.ReadIntAsync();

            if (strLen == 0)
                return (string.Empty, 4);

            var data = await stream.CreateArrayAndReaderItFromSocketAsync(strLen);

            encoding ??= Encoding.UTF8;

            return (encoding.GetString(data), data.Length + 4);
        }


        public static async ValueTask<(byte[] data, int dataSize)> ReadByteArrayAsync(this Stream stream)
        {
            var arrayLen = await stream.ReadIntAsync();
            if (arrayLen == 0)
                return (Array.Empty<byte>(), 4);

            var result = await stream.CreateArrayAndReaderItFromSocketAsync(arrayLen);

            return (result, result.Length + 4);
        }
    }
}