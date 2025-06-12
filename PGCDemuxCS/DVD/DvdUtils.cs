
using System.Numerics;
using System.Text;

namespace PgcDemuxCS.DVD
{
    internal static class DvdUtils
    {
        public const int DVD_BLOCK_LEN = 2048;

        public static void B2N_16(ref ushort x)
        {
            //x = (ushort)(((x & 0xFF00) >> 8) | ((x & 0x00FF) << 8));
        }

        public static void B2N_32(ref uint x)
        {
            /*x = (uint)(
                ((x & 0xFF000000) >> 24) |
                ((x & 0x00FF0000) >> 8) |
                ((x & 0x0000FF00) << 8) |
                ((x & 0x000000FF) << 24)
            );*/
        }

        public static void B2N_64(ref ulong x)
        {
            /*x = (uint)(
                ((x & 0xFF00000000000000) >> 56) |
                ((x & 0x00FF000000000000) >> 40) |
                ((x & 0x0000FF0000000000) >> 24) |
                ((x & 0x000000FF00000000) >> 8) |
                ((x & 0x00000000FF000000) << 8) |
                ((x & 0x0000000000FF0000) << 24) |
                ((x & 0x000000000000FF00) << 40) |
                ((x & 0x00000000000000FF) << 56)
            );*/
        }

        public static void CHECK_VALUE(bool value)
        {
            if (value == false)
            {
                throw new IOException("CHECK_VALUE failed.");
            }
        }

        public static void CHECK_VALUE<T>(T value) where T : INumberBase<T>
        {
            CHECK_VALUE(value != T.Zero);
        }

        public static void CHECK_ZERO(bool value)
        {
            if (value == true)
            {
                throw new IOException("CHECK_ZERO failed.");
            }
        }

        public static void CHECK_ZERO<T>(T value) where T : INumberBase<T>
        {
            CHECK_ZERO(value != T.Zero);
        }

        public static void CHECK_ZERO<T>(T[] values) where T : INumberBase<T>
        {
            foreach (T val in values)
            {
                CHECK_ZERO(val);
            }
        }

        public static string ReadString(this Stream stream, int length)
        {
            byte[] data = new byte[length];
            stream.Read(data);
            return Encoding.ASCII.GetString(data).TrimEnd('\0');
        }

        public static T Read<T>(this Stream stream) where T : IBinaryInteger<T>, IShiftOperators<T, int, T>, IBitwiseOperators<T, T, T>
        {
            T result = T.Zero;
            for (int i = 0; i < result.GetByteCount(); i++)
            {
                result = (result << 8) | ((T)Convert.ChangeType(stream.ReadByte(), typeof(T)));
            }
            return result;
        }

        public static void Read<T>(this Stream stream, Span<T> span) where T : IStreamReadable<T>
        {
            for (int i = 0; i < span.Length; i++)
            {
                var item = T.Read(stream);
                if (item == null) throw new IOException();
                span[i] = item;
            }
        }

        public static void Read<T>(this Stream stream, T[] array) where T : IBinaryInteger<T>, IShiftOperators<T, int, T>, IBitwiseOperators<T, T, T>
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = stream.Read<T>();
            }
        }
    }

    interface IStreamReadable<T> {
        public static abstract T? Read(Stream file);
    }
}