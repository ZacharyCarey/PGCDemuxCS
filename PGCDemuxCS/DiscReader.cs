using DiscUtils.Iso9660;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgcDemuxCS
{
    public class DiscReader : IIfoFileReader, IDisposable
    {
        private FileStream BaseStream;
        private CDReader CD;

        public DiscReader(string isoPath)
        {
            BaseStream = File.OpenRead(isoPath);
            this.CD = new CDReader(BaseStream, true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CD.Dispose();
                BaseStream.Dispose();
            }
        }

        public Stream OpenFile(string file)
        {
            return CD.OpenFile($"VIDEO_TS\\{file}", FileMode.Open);
        }
    }
}
