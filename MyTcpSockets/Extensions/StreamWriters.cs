using System;
using System.IO;
using System.Text;

namespace MyTcpSockets.Extensions
{
    public static class StreamWriters
    {

        public static void WritePascalString(this Stream stream, string data, Encoding encoding = null)
        {
            data ??= string.Empty;

            encoding ??= Encoding.UTF8;

            var dataBin = encoding.GetBytes(data);

            var stringLength = (byte)dataBin.Length;
            stream.WriteByteFromStack(stringLength);

            if (stringLength > 0)
                stream.Write(dataBin, 0, dataBin.Length);

        }

        public static void WriteString(this Stream stream, string data, Encoding encoding = null)
        {
            data ??= string.Empty;

            encoding ??= Encoding.UTF8;

            var dataToSend = encoding.GetBytes(data);

            var stringLength = dataToSend.Length;

            stream.WriteInt(stringLength);

            if (stringLength > 0)
                stream.Write(dataToSend);
        }

        public static void WriteByteFromStack(this Stream stream, byte data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            if (BitConverter.TryWriteBytes(buffer, data))
                stream.Write(buffer.Slice(0,1));
        }

        public static void WriteUshort(this Stream stream, ushort data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            if (BitConverter.TryWriteBytes(buffer, data))
                stream.Write(buffer);
        }

        public static void WriteShort(this Stream stream, short data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            if (BitConverter.TryWriteBytes(buffer, data))
                stream.Write(buffer);
        }


        public static void WriteUint(this Stream stream, uint data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            if (BitConverter.TryWriteBytes(buffer, data))
                stream.Write(buffer);
        }

        public static void WriteInt(this Stream stream, int data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            if (BitConverter.TryWriteBytes(buffer, data))
                stream.Write(buffer);
        }

        public static void WriteUlong(this Stream stream, ulong data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            if (BitConverter.TryWriteBytes(buffer, data))
                stream.Write(buffer);
        }

        public static void WriteLong(this Stream stream, long data)
        {
            var memStream = new MemoryStream();
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            if (BitConverter.TryWriteBytes(buffer, data))
                stream.Write(buffer);    
        }

        public static void WriteByteArray(this Stream stream, ReadOnlySpan<byte> byteArray)
        {
            stream.WriteInt(byteArray.Length);
            stream.Write(byteArray);
        }

    }
}