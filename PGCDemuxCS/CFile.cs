using System.Text;

namespace PgcDemuxCS
{
    internal class CFILE
    {
        FileStream Handle;
        public bool EOF { get; private set; } = false;

        private CFILE(string path, FileMode mode, FileAccess access)
        {
            this.Handle = File.Open(path, mode, access);
        }

        public static CFILE fopen(string path, string mode)
        {
            switch (mode)
            {
                case "r":
                case "rb":
                    return new CFILE(path, FileMode.Open, FileAccess.Read);
                case "r+":
                    return new CFILE(path, FileMode.Open, FileAccess.ReadWrite);
                case "w":
                case "wb":
                    return new CFILE(path, FileMode.OpenOrCreate, FileAccess.Write);
                case "w+":
                    return new CFILE(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                case "a":
                case "ab":
                    return new CFILE(path, FileMode.Append, FileAccess.Write);
                case "a+":
                    return new CFILE(path, FileMode.Append, FileAccess.ReadWrite);
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer">pointer to the array where the read objects are stored</param>
        /// <param name="count">the number of the objects to be read</param>
        /// <returns></returns>
        public int fread(Ref<byte> buffer, int size, int count)
        {
            int byteCount = size * count;
            int result = buffer.ReadFromStream(this.Handle, byteCount);
            if (result < byteCount) this.EOF = true;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer">pointer to the first object in the array to be written</param>
        /// <param name="size">size of each object</param>
        /// <param name="count">the number of the objects to be written</param>
        /// <returns></returns>
        public int fwrite(Ref<byte> buffer, int size, int count)
        {
            return buffer.WriteToStream(this.Handle, size * count);
        }

        public int fseek(long offset, SeekOrigin origin)
        {
            try
            {
                this.Handle.Seek(offset, origin);
                return 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public void fclose()
        {
            this.Handle.Close();
        }

        public void fprintf(string str)
        {
            var buffer = Encoding.ASCII.GetBytes(str);
            this.Handle.Write(buffer, 0, buffer.Length);
        }

        public void fputc(byte c)
        {
            this.Handle.WriteByte(c);
        }

        public bool feof()
        {
            return this.EOF;
        }

        public int fgetc()
        {
            int c = this.Handle.ReadByte();
            if (c < 0) this.EOF = true;
            return c;
        }
    }
}