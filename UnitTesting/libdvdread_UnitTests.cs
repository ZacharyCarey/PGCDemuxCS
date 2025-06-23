using PgcDemuxCS;
using PgcDemuxCS.DVD;
using PgcDemuxCS.DVD.IfoTypes.Common;
using PgcDemuxCS.DVD.IfoTypes.VMGI;
using PgcDemuxCS.DVD.IfoTypes.VTS;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        public void VerifyContents()
        {
            DiscReader discReader = new DiscReader(Path.Combine(DvdBackupPath, $"{DvdName}.iso"));
            VtsIfo[] ifo = new VtsIfo[100];
            VmgIfo vmg = VmgIfo.Open(discReader);
            Console.WriteLine(vmg?.TextData?.DiscName);
            Assert.IsNotNull(vmg, "Failed to load VMG IFO");
            for (int i = 1; i <= 99; i++)
            {
                ifo[i] = VtsIfo.Open(discReader, i);
            }

            libdvdread expected = libdvdread.Load(InfoFile);
            Assert.IsNotNull(expected, "Failed to load expected info");

            Assert.AreEqual(expected.VMG_ID, vmg.ID, "VMG Identifier");
            Assert.AreEqual(expected.ProviderID, vmg.ProviderID, "Provider ID");

            foreach(var track in expected.Tracks.Where(x => x != null))
            {
                VerifyTrack(track, vmg, ifo);
            }
        }

        private static void VerifyTrack(Track expected, VmgIfo vmg, VtsIfo[] ifo)
        {
            VtsIfo vts_ifo;
            ChapterInfo title;
            FindTrack(expected, vmg, ifo, out vts_ifo, out title);

            // Verify data
            ProgamChainInformationTable vts_pgcit = vts_ifo.TitleProgramChainTable;
            var video_attr = vts_ifo.TitlesVobVideoAttributes;
            var vts_id = vts_ifo.ID;
            
            var pgc = vts_pgcit[vts_ifo.TitlesAndChapters[title.ChapterNumber - 1][0].ProgramChainNumber - 1].Pgc;

            var chapter_count_reported = title.NumberOfChapters;
            if (pgc.CellPlayback == null || pgc.ProgramMap == null)
            {
                Assert.AreEqual(0, expected.Length);
                Assert.AreEqual(0, expected.Chapters.Count);
                Assert.AreEqual(0, expected.Cells.Count);
                Assert.AreEqual(0, expected.AudioTracks.Count);
                Assert.AreEqual(0, expected.Subpictures.Count);
                return;
            }
            else
            {
                Assert.AreEqual(expected.Length, pgc.PlaybackTime.TotalSeconds, 0.002, "Track length did not match");
                Assert.AreEqual(expected.VtsID, vts_id, "Vts ID did not match");
                Assert.AreEqual(expected.VTS, Array.IndexOf(ifo, vts_ifo), "VTS number did not match");
                Assert.AreEqual(expected.FPS, pgc.PlaybackFPS, 0.01, "FPS did not match");
                Assert.AreEqual(expected.Format, GetFormat(vts_ifo.TitlesVobVideoAttributes), "Video format did not match");
                Assert.AreEqual(expected.Aspect, GetAspect(vts_ifo.TitlesVobVideoAttributes), "Video aspect ratio did not match");
                Assert.AreEqual(expected.Width, vts_ifo.TitlesVobVideoAttributes.Size.Width, "Video width did not match");
                Assert.AreEqual(expected.Height, vts_ifo.TitlesVobVideoAttributes.Size.Height, "Video height did not match");
                //Assert.AreEqual(expected.DF, vts_ifo.vtsi_mat.vts_video_attr.);
                Assert.AreEqual(expected.Angles, title.NumberOfAngles, "Number of angles did not match");

                foreach(var audio in expected.AudioTracks)
                {
                    string streamID;
                    var stream = FindAudioStream(audio, vts_ifo, pgc, title, out streamID);
                    Assert.AreEqual(audio.LanguageCode, stream.LanguageCode, "Language did not match");
                    Assert.AreEqual(audio.Format, GetFormat(stream), "Audio format did not match");
                    Assert.AreEqual(audio.Frequency, (uint)GetFrequency(stream), "Audio frequency did not match");
                    Assert.AreEqual(audio.Quantization, GetQuantization(stream), "Audio quantization did not match");
                    Assert.AreEqual(audio.Channels, stream.Channels, "Audio channels did not match");
                    Assert.AreEqual(audio.ApMode, (int)stream.Mode, "Audio app mode did not match");
                    Assert.AreEqual(audio.StreamID, streamID, "Stream ID did not match");
                }

                // chapters
                int icell = 0;
                for (int i = 0; i < pgc.NumberOfPrograms; i++)
                {
                    TimeSpan duration = TimeSpan.Zero;
                    int next;
                    if (i == pgc.ProgramMap.Length - 1) next = pgc.CellPlayback.Length + 1;
                    else next = pgc.ProgramMap[i + 1];

                    playback_time_t time = new();
                    while (icell < next - 1)
                    {
                        duration += pgc.CellPlayback[icell].PlaybackTime;
                        //converttime(ref time, pgc.cell_playback[icell].playback_time);
                        icell++;
                    }

                    var expectedChapter = expected.Chapters.Where(x => x.Index - 1 == i).First();
                    Assert.AreEqual(expectedChapter.StartCell, pgc.ProgramMap[i]);
                    Assert.AreEqual(expectedChapter.Length, duration.TotalSeconds, 0.002);
                }

                // cells
                for (int i = 0; i < pgc.NumberOfCells; i++)
                {
                    var cell_length = pgc.CellPlayback[i].PlaybackTime;
                    //converttime(&dvd_info.titles[j].cells[i].playback_time, &pgc->cell_playback[i].playback_time);
                    /* added to get the start/end sectors */
                    var first_sector = pgc.CellPlayback[i].FirstSector;
                    var last_sector = pgc.CellPlayback[i].LastSector;
                    var expectedCell = expected.Cells.First(x => x.Index - 1 == i);
                    Assert.AreEqual(expectedCell.Length, cell_length.TotalSeconds, 0.002);
                    Assert.AreEqual((uint)expectedCell.FirstSector, first_sector);
                    Assert.AreEqual((uint)expectedCell.LastSector, last_sector);
                }

                // subtitles
                int subcount = pgc.SubpictureStreams.Length;
                for (int i = 0, k = 0; i < subcount; i++)
                {
                    if (!pgc.SubpictureStreams[k].Available)
                        continue;
                    var subp_attr = vts_ifo.TitlesVobSubpictureAttributes[i];

                    var subp = expected.Subpictures.First(x => x.Index - 1 == k);
                    Assert.AreEqual(subp.LanguageCode, string.IsNullOrWhiteSpace(subp_attr.LanguageCode) ? "xx" : subp_attr.LanguageCode);
                    Assert.AreEqual(subp.StreamID, $"0x{(0x20 + i):X2}");
                    k++;
                }
            }
        }

        private static void FindTrack(Track track, VmgIfo vmg, VtsIfo[] ifo, out VtsIfo vts, out ChapterInfo title)
        {
            var titles = vmg.Chapters.Count;

            for (int j = 0; j < titles; j++)
            {
                if (ifo[vmg.Chapters[j].TitleSetNumber] != null)
                {
                    var vtsi_mat = ifo[vmg.Chapters[j].TitleSetNumber];
                    var title_set_nr = vmg.Chapters[j].TitleSetNumber;
                    var vts_ttn = vmg.Chapters[j].ChapterNumber;

                    if (title_set_nr == track.VTS && vts_ttn == track.TTN)
                    {
                        vts = ifo[vmg.Chapters[j].TitleSetNumber];
                        title = vmg.Chapters[j];
                        return;
                    }
                }
            }

            Assert.Fail("Failed to find track.");
            throw new Exception();
        }

        private static double[] frames_per_s = { -1.0, 25.00, -1.0, 29.97 };
        private static string GetFormat(VideoAttributes attr)
        {
            switch (attr.Region)
            {
                case RegionStandard.NTSC: return "NTSC";
                case RegionStandard.PAL: return "PAL";
                default: return null;
            }
        }
        private static string GetAspect(VideoAttributes attr) {
            switch(attr.DisplayAspectRatio)
            {
                case AspectRatio.Fullscreen: return "4/3";
                case AspectRatio.WideScreen: return "16/9";
                default: return null;
            }
        }
        private static string GetFormat(AudioAttributes attr)
        {
            switch(attr.Format)
            {
                case AudioEncoding.AC3: return "ac3";
                case AudioEncoding.Mpeg1: return "mpeg1";
                case AudioEncoding.Mpeg2: return "mpeg2";
                case AudioEncoding.LPCM: return "lpcm";
                case AudioEncoding.SDDS: return "sdds"; // todo: confirm
                case AudioEncoding.DTS: return "dts";
                default:
                    Assert.Fail("Invalid audio format");
                    return null;
            }
        }
        private static int GetFrequency(AudioAttributes attr)
        {
            return attr.SampleFrequency;
        }
        private static string GetQuantization(AudioAttributes attr)
        {
            switch (attr.Quantization)
            {
                case QuantizationType._16bps: return "16bit";
                case QuantizationType._20bps: return "20bit";
                case QuantizationType._24bps: return "24bit";
                case QuantizationType.DRC: return "drc";
                default: 
                    Assert.Fail("Invalid quantization");
                    return null;
            }
        }
        private static uint[] audio_id = { 0x80, 0, 0xC0, 0xC0, 0xA0, 0, 0x88 };
        private static AudioAttributes FindAudioStream(AudioTrack track, VtsIfo vts, PGC pgc, ChapterInfo title, out string streamID)
        {
            for (int i = 0; i < pgc.AudioStreams.Length; i++)
            {
                if (!pgc.AudioStreams[i].Available)
                {
                    continue;
                }
                var attr = vts.TitlesVobAudioAttributes[i];
                streamID = $"0x{(audio_id[(byte)attr.Format] + i):X2}";
                if (streamID == track.StreamID)
                {
                    return attr;
                }
            }

            Assert.Fail("Could not find audio stream.");
            throw new Exception();
        }

        /*[TestMethod]
        public void InfoData()
        {
            var reader = new DiscReader(Path.Combine(DvdBackupPath, $"{DvdName}.iso"));
            var info = PgcDemux.GetIfoInfo(reader, 12);
            Console.WriteLine($"Menu PGCs: {string.Join(", ", info.MenuPGCs)}");
            var titlePGCs = info.TitlePGCs.Select(x => $"{x.PGC} ({x.NumberOfAngles} angles)");
            Console.WriteLine($"Title PGCs: {string.Join(", ", titlePGCs)}");
            Console.WriteLine($"Menu VIDs: {string.Join(", ", info.MenuVIDs)}");
            Console.WriteLine($"Title VIDs: {string.Join(", ", info.TitleVIDs)}");
            Console.WriteLine($"Menu Cells: {string.Join(", ", info.MenuCells)}");
            Console.WriteLine($"Title Cells: {string.Join(", ", info.TitleCells)}");
        }*/
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

    internal struct playback_time_t
    {
        public int hour;
        public int minute;
        public int second;
        public int usec;
    }
}
