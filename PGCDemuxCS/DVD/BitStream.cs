using System.Numerics;
using System.Text;

namespace PgcDemuxCS.DVD
{


    internal class BitStream : Stream
    {
        Stream Stream;
        byte buffer;
        int bits = 0;

        public BitStream(Stream stream)
        {
            this.Stream = stream;
        }

        public override bool CanRead => Stream.CanRead;

        public override bool CanSeek => Stream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => Stream.Length;

        public override long Position
        {
            get => Stream.Position;
            set => Stream.Position = value;
        }

        public override void Flush()
        {
            Stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            bits = 0;
            return Stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            bits = 0;
            return Stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override int ReadByte()
        {
            return ReadBits<byte>(8);
        }

        private void FillBuffer()
        {
            int n = Stream.ReadByte();
            if (n < 0) throw new IOException();
            buffer = (byte)n;
        }

        public bool ReadBit()
        {
            if (bits <= 0) FillBuffer();
            bits -= 1;
            return ((this.buffer >> bits) & 0b1) > 0;
        }

        public T ReadBits<T>(int bitCount) where T : INumberBase<T>, IShiftOperators<T, int, T>, IBitwiseOperators<T, T, T>
        {
            T result = T.Zero;
            while (bitCount > 0)
            {
                result = (result << 1) | (ReadBit() ? T.One : T.Zero);
            }
            return result;
        }

    }
}