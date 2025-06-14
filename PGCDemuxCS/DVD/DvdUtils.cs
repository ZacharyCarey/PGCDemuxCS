
using System.Numerics;
using System.Text;

namespace PgcDemuxCS.DVD
{
    internal static class DvdUtils
    {
        public const int DVD_BLOCK_LEN = 2048;

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

        public static void ReadZeros(this Stream stream, int count)
        {
            byte[] temp = new byte[count];
            stream.Read(temp);
            foreach(byte b in temp)
            {
                if (b != 0)
                {
                    throw new IOException($"Expected byte to be zero, but instead read '{b}'.");
                }
            }
        }
    }

    internal interface IStreamReadable<T> {
        internal static abstract T? Read(Stream file);
    }
}