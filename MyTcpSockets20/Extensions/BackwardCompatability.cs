using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly:CLSCompliant(false)]
namespace MyTcpSockets.Extensions
{
    public static class BackwardCompatability
    {

        public static void Write(this Stream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }
        
        public static void Write(this Stream stream, ReadOnlySpan<byte> data)
        {
            var array = data.ToArray();
            stream.Write(array, 0, array.Length);
        }
        
    }


    public static class BitConverter20
    {
        [CLSCompliant(false)]
        public static bool TryWriteBytes(Span<byte> destination, short value)
        {
            if (destination.Length < sizeof(short))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
        [CLSCompliant(false)]
        public static bool TryWriteBytes(Span<byte> destination, char value)
        {
            if (destination.Length < sizeof(char))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
        [CLSCompliant(false)]
        public static bool TryWriteBytes(Span<byte> destination, int value)
        {
            if (destination.Length < sizeof(int))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
        [CLSCompliant(false)]
        public static bool TryWriteBytes(Span<byte> destination, long value)
        {
            if (destination.Length < sizeof(long))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
        
        public static bool TryWriteBytes(Span<byte> destination, ushort value)
        {
            if (destination.Length < sizeof(ushort))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
        
        [CLSCompliant(false)]
        public static bool TryWriteBytes(Span<byte> destination, uint value)
        {
            if (destination.Length < sizeof(uint))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
        
        [CLSCompliant(false)]
        public static bool TryWriteBytes(Span<byte> destination, ulong value)
        {
            if (destination.Length < sizeof(ulong))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
        
        
        // Converts a Span into a ushort
        [CLSCompliant(false)]
        public static ushort ToUInt16(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(ushort))
                throw new ArgumentOutOfRangeException();
            return Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(value));
        }
        
        // Converts a Span into a ushort
        [CLSCompliant(false)]
        public static short ToInt16(ReadOnlySpan<byte> value)
        {
            return (short) ToUInt16(value);
        }
        
        [CLSCompliant(false)]
        public static uint ToUInt32(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(uint))
                throw new ArgumentOutOfRangeException();
            return Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(value));
        }
        
        [CLSCompliant(false)]
        public static int ToInt32(ReadOnlySpan<byte> value)
        {
            return (int) ToUInt32(value);
        }
        
        [CLSCompliant(false)]
        public static ulong ToUInt64(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(ulong))
                throw new ArgumentOutOfRangeException();
            
            return Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(value));
        }
        
        [CLSCompliant(false)]
        public static long ToInt64(ReadOnlySpan<byte> value)
        {
            return (long) ToUInt64(value);
        }
        
    }

}