
namespace UnitTesting { 

    internal class MultiFileStream : Stream
    {
        string[] paths;
        long[] fileSizes;

        int currentFileIndex = -1;
        Stream? currentFile;
        long totalLength;
        long position = 0;

        public MultiFileStream(params string[] filePaths)
        {
            this.paths = filePaths;
            this.fileSizes = new long[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                FileInfo fileInfo = new FileInfo(filePaths[i]);
                fileSizes[i] = fileInfo.Length;
            }
            totalLength = fileSizes.Sum();

            OpenFile(0);
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
                    OpenNextFile();
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
                OpenFile(paths.Length); // Set stream to EOF
                return this.position;
            }

            long pos = offset;
            for (int i = 0; i < paths.Length; i++)
            {
                long size = fileSizes[i];
                if (pos < size)
                {
                    OpenFile(i);
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
            

            // EOF was reached
            position = this.Length;
            OpenFile(10);
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
        private void OpenNextFile()
        {
            if (currentFileIndex >= paths.Length)
            {
                return;
            }

            for (int i = currentFileIndex + 1; i < paths.Length; i++)
            {
                OpenFile(i);
                if (currentFile != null)
                {
                    return;
                }
            }

            OpenFile(paths.Length);
        }

        private void OpenFile(int id)
        {
            if (currentFileIndex == id && currentFile != null)
            {
                currentFile.Seek(0, SeekOrigin.Begin);
                return;
            }

            if (currentFile != null)
            {
                currentFile.Close();
                currentFile.Dispose();
                currentFile = null;
            }

            currentFileIndex = id;
            if (currentFileIndex < 0 || currentFileIndex >= paths.Length)
            {
                currentFile = null;
                return;
            }

            if (fileSizes[currentFileIndex] <= 0)
            {
                currentFile = null;
                return;
            }

            currentFile = File.OpenRead(paths[currentFileIndex]);
        }
    }
}