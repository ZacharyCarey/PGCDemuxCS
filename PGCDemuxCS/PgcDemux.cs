using PGCDemuxCS;

namespace PgcDemuxCS
{
    public class PgcDemux
    {
        public static void Run(PgcDemuxOptions options, string dvdRootPath)
        {
            PgcDemux.Run(options, new SimpleIfoReader(dvdRootPath));
        }

        public static void Run(PgcDemuxOptions options, IIfoFileReader fileReader)
        {
            PgcDemuxApp.Run(options, fileReader);
        }
    }
}