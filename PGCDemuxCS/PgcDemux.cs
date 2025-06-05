namespace PgcDemuxCS
{
    public class PgcDemux
    {
        public static void Run(string cmd, string dvdRootPath)
        {
            PgcDemux.Run(cmd, new SimpleIfoReader(dvdRootPath));
        }

        public static void Run(string cmd, IIfoFileReader fileReader)
        {
            PgcDemuxApp.Run(cmd, fileReader);
        }
    }
}