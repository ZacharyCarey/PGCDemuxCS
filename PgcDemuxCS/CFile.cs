using System.Reflection.PortableExecutable;
using System.Text;

namespace PgcDemuxCS
{
    internal class CFILE
    {
        Stream Handle;
        public bool EOF { get; private set; } = false;
        public long Size { get => Handle.Length; }

        internal CFILE(Stream stream)
        {
            this.Handle = stream;
        }

        public static CFILE? OpenRead(IIfoFileReader reader, string filename)
        {
            try
            {
                return new CFILE(reader.OpenFile(filename));
            }catch(Exception e)
            {
                return null;
            }
        }

        public static CFILE? OpenWrite(string path, bool append = false)
        {
            try
            {
                return new CFILE(File.Open(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static CFILE? OpenReadWrite(string path, bool append = false)
        {
            try
            {
                return new CFILE(File.Open(path, append ? FileMode.Append : FileMode.Create, FileAccess.ReadWrite));
            }
            catch (Exception)
            {
                return null;
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