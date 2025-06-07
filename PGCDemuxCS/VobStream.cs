using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PgcDemuxCS
{
    // NOTE!! Title VOBs (which is what this is used for) start at index "1". i.e. only VTS_xx_1.VOB through VTS_xx_9.VOB
    // are used, and VTS_xx_1.VOB contains position=0
    internal class VobStream : Stream
    {
        int currentVBO = -1;
        Stream? currentFile;
        IfoInfo ifo;
        IIfoFileReader FileReader;
        long totalLength;
        long position = 0;
        DomainType domain;

        public VobStream(IfoInfo ifo, IIfoFileReader fileReader, DomainType domain)
        {
            this.ifo = ifo;
            this.FileReader = fileReader;

            if (ifo.m_bVMGM) domain = DomainType.Menus;
            this.domain = domain;

            if (domain == DomainType.Menus)
            {
                this.totalLength = ifo.m_i64VOBSize[0];
                OpenVOB(0);
            } else
            {
                this.totalLength = ifo.m_i64VOBSize.Skip(1).Sum();
                OpenVOB(1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            currentFile?.Dispose();
        }

        public override bool CanRead => currentFile?.CanRead ?? false;

        public override bool CanSeek => currentFile?.CanSeek ?? false;

        public override bool CanWrite => false;

        public override long Length => totalLength;

        public override long Position
        {
            get => position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush()
        {
            currentFile?.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (count > 0 && currentFile != null)
            {
                int read = currentFile.Read(buffer, offset, count);
                if (read < count)
                {
                    OpenNextVOB();
                }
                offset += read;
                count -= read;
                totalRead += read;
            }

            position += totalRead;
            return totalRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Current)
            {
                offset = position + offset;
            }
            else if (origin == SeekOrigin.End)
            {
                offset = this.Length + offset; // offset will be a negative number here
            }
            if (offset < 0) throw new IOException("Offset went before the start of the stream.");

            if (offset >= this.Length)
            {
                this.position = offset;
                OpenVOB(10); // Set stream to EOF
                return this.position;
            }

            if (this.domain == DomainType.Menus)
            {
                // We are in menu mode, there is only one file to seek
                OpenVOB(0);
                if (currentFile != null)
                {
                    this.position = currentFile.Seek(offset, SeekOrigin.Begin);
                    return this.position;
                }
            }
            else
            {
                long pos = offset;
                for (int i = 1; i < 10; i++)
                {
                    long size = ifo.m_i64VOBSize[i];
                    if (pos < size)
                    {
                        OpenVOB(i);
                        if (currentFile == null) break;
                        currentFile.Seek(pos, SeekOrigin.Begin);
                        position = offset;
                        return offset;
                    }
                    else
                    {
                        pos -= size;
                    }
                }
            }

            // EOF was reached
            position = this.Length;
            OpenVOB(10);
            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Will find the next valid VOB and attempt to open it
        /// </summary>
        private void OpenNextVOB()
        {
            if (currentVBO > 9)
            {
                return;
            }

            if (domain == DomainType.Menus)
            {
                // Opened in menu mode, there are no VOB's remaining
                OpenVOB(10); // Open a non-existent VOB to set the internal state. VOB=10 will never exist
                return;
            }

            for (int i = currentVBO + 1; i < 10; i++)
            {
                OpenVOB(i);
                if (currentFile != null)
                {
                    return;
                }
            }

            OpenVOB(10); // Open a non-existent VOB to set the internal state. VOB=10 will never exist
        }

        private void OpenVOB(int id)
        {
            if (currentVBO == id && currentFile != null)
            {
                currentFile.Seek(0, SeekOrigin.Begin);
                return;
            }

            if (currentFile != null)
            {
                currentFile.Close();
                currentFile.Dispose();
            }

            currentVBO = id;
            if (currentVBO < 0 || currentVBO > 9)
            {
                currentFile = null;
                return;
            }

            if (ifo.m_i64VOBSize[currentVBO] <= 0)
            {
                currentFile = null;
                return;
            }

            if (this.domain == DomainType.Menus && currentVBO != 0)
            {
                throw new ArgumentException("Menu IFO can only open VBO 0."); // Should not happen
            }

            string fileName;
            if (ifo.m_bVMGM)
            {
                fileName = $"VIDEO_TS.VOB";
            }
            else
            {
                fileName = $"VTS_{ifo.VtsNumber:00}_{currentVBO:0}.VOB";
            }

            try
            {
                currentFile = FileReader.OpenFile(fileName);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to open next VOB file.", ex);
            }
        }
    }
}