namespace PgcDemuxCS
{
    public interface IIfoFileReader
    {
        /// <summary>
        /// Open a specific DVD file for reading, given its file name.
        /// Example file names that could be given:
        /// "VIDEO_TS.IFO"
        /// "VTS_01_0.BUP"
        /// VTS_01_1.VOB
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>A stream for the requested file.</returns>
        public Stream OpenFile(string fileName);
    }

    public class SimpleIfoReader : IIfoFileReader
    {
        private string RootPath;

        public SimpleIfoReader(string dvdRoot)
        {
            this.RootPath = dvdRoot;
        }

        public Stream OpenFile(string fileName)
        {
            return File.OpenRead(Path.Combine(this.RootPath, "VIDEO_TS", fileName));
        }
    }
}