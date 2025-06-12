using PgcDemuxCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting
{
    [TestClass]
    public abstract class libdvdread_UnitTests
    {
        private const string DvdBackupPath = "C:\\Users\\Zack\\Videos";
        private readonly string DvdName;
        private readonly string InfoFile;

        protected libdvdread_UnitTests(string dvdName, string ifoFileName)
        {
            DvdName = dvdName;
            InfoFile = ifoFileName;
        }

        [TestMethod]
        public void OpenIFO()
        {
            DiscReader discReader = new DiscReader(Path.Combine(DvdBackupPath, $"{DvdName}.iso"));
            ifo_handle_t? ifo = ifo_handle_t.Open(discReader, "VTS_02_0");
            Assert.IsNotNull(ifo);
        }
    }

    [TestClass]
    public class WillyWonkaIfoTest : libdvdread_UnitTests
    {
        public WillyWonkaIfoTest() : base("WILLY_WONKA", "willy_wonka-info.json")
        { }
    }

    [TestClass]
    public class AnimusicIfoTest : libdvdread_UnitTests
    {
        public AnimusicIfoTest() : base("ANIMUSIC", "animusic-info.json")
        { }
    }
}
