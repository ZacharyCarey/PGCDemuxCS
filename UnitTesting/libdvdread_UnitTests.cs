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
            title_info_t title;
            FindTrack(expected, vmg, ifo, out vts_ifo, out title);

            // Verify data
            pgcit_t vts_pgcit = vts_ifo.TitleProgramChainTable;
            var video_attr = vts_ifo.TitlesVobVideoAttributes;
            var vts_id = vts_ifo.ID;
            
            var pgc = vts_pgcit.pgci_srp[vts_ifo.TitlesAndChapters.titles[title.vts_ttn - 1].ptt[0].pgcn - 1].pgc;

            var chapter_count_reported = title.nr_of_ptts;
            if (pgc.cell_playback == null || pgc.program_map == null)
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
                Assert.AreEqual(expected.Length, dvdtime2msec(pgc.playback_time) / 1000.0, 0.001, "Track length did not match");
                Assert.AreEqual(expected.VtsID, vts_id, "Vts ID did not match");
                Assert.AreEqual(expected.VTS, Array.IndexOf(ifo, vts_ifo), "VTS number did not match");
                Assert.AreEqual(expected.FPS, frames_per_s[(pgc.playback_time.frame_u & 0xC0) >> 6], 0.01, "FPS did not match");
                Assert.AreEqual(expected.Format, GetFormat(vts_ifo.TitlesVobVideoAttributes), "Video format did not match");
                Assert.AreEqual(expected.Aspect, GetAspect(vts_ifo.TitlesVobVideoAttributes), "Video aspect ratio did not match");
                Assert.AreEqual(expected.Width, GetSize(vts_ifo.TitlesVobVideoAttributes).Width, "Video width did not match");
                Assert.AreEqual(expected.Height, GetSize(vts_ifo.TitlesVobVideoAttributes).Height, "Video height did not match");
                //Assert.AreEqual(expected.DF, vts_ifo.vtsi_mat.vts_video_attr.);
                Assert.AreEqual(expected.Angles, title.nr_of_angles, "Number of angles did not match");

                foreach(var audio in expected.AudioTracks)
                {
                    string streamID;
                    var stream = FindAudioStream(audio, vts_ifo, pgc, title, out streamID);
                    Assert.AreEqual(audio.LanguageCode, (stream.lang_type == 1) ? stream.lang_code : "un", "Language did not match");
                    Assert.AreEqual(audio.Format, GetFormat(stream), "Audio format did not match");
                    Assert.AreEqual(audio.Frequency, (uint)GetFrequency(stream), "Audio frequency did not match");
                    Assert.AreEqual(audio.Quantization, GetQuantization(stream), "Audio quantization did not match");
                    Assert.AreEqual(audio.Channels, stream.channels + 1, "Audio channels did not match");
                    Assert.AreEqual(audio.ApMode, stream.application_mode, "Audio app mode did not match");
                    Assert.AreEqual(audio.StreamID, streamID, "Stream ID did not match");
                }

                // chapters
                int icell = 0;
                for (int i = 0; i < pgc.nr_of_programs; i++)
                {
                    int ms = 0;
                    int next;
                    if (i == pgc.nr_of_programs - 1) next = pgc.nr_of_cells + 1;
                    else next = pgc.program_map[i + 1];

                    playback_time_t time = new();
                    while (icell < next - 1)
                    {
                        ms += (int)dvdtime2msec(pgc.cell_playback[icell].playback_time);
                        //converttime(ref time, pgc.cell_playback[icell].playback_time);
                        icell++;
                    }

                    var expectedChapter = expected.Chapters.Where(x => x.Index - 1 == i).First();
                    Assert.AreEqual(expectedChapter.StartCell, pgc.program_map[i]);
                    Assert.AreEqual(expectedChapter.Length, ms * 0.001, 0.001);
                }

                // cells
                for (int i = 0; i < pgc.nr_of_cells; i++)
                {
                    var cell_length = dvdtime2msec(pgc.cell_playback[i].playback_time) / 1000.0;
                    //converttime(&dvd_info.titles[j].cells[i].playback_time, &pgc->cell_playback[i].playback_time);
                    /* added to get the start/end sectors */
                    var first_sector = pgc.cell_playback[i].first_sector;
                    var last_sector = pgc.cell_playback[i].last_sector;
                    var expectedCell = expected.Cells.First(x => x.Index - 1 == i);
                    Assert.AreEqual(expectedCell.Length, cell_length, 0.001);
                    Assert.AreEqual((uint)expectedCell.FirstSector, first_sector);
                    Assert.AreEqual((uint)expectedCell.LastSector, last_sector);
                }

                // subtitles
                int subcount = pgc.subp_control.Length;
                for (int i = 0, k = 0; i < subcount; i++)
                {
                    if ((pgc.subp_control[k] & 0x80000000) == 0)
                        continue;
                    var subp_attr = vts_ifo.TitlesVobSubpictureAttributes[i];

                    var subp = expected.Subpictures.First(x => x.Index - 1 == k);
                    Assert.AreEqual(subp.LanguageCode, string.IsNullOrWhiteSpace(subp_attr.lang_code) ? "xx" : subp_attr.lang_code);
                    Assert.AreEqual(subp.StreamID, $"0x{(0x20 + i):X2}");
                    k++;
                }
            }
        }

        private static void FindTrack(Track track, VmgIfo vmg, VtsIfo[] ifo, out VtsIfo vts, out title_info_t title)
        {
            var titles = vmg.Titles.nr_of_srpts;

            for (int j = 0; j < titles; j++)
            {
                if (ifo[vmg.Titles.title[j].title_set_nr] != null)
                {
                    var vtsi_mat = ifo[vmg.Titles.title[j].title_set_nr];
                    var title_set_nr = vmg.Titles.title[j].title_set_nr;
                    var vts_ttn = vmg.Titles.title[j].vts_ttn;

                    if (title_set_nr == track.VTS && vts_ttn == track.TTN)
                    {
                        vts = ifo[vmg.Titles.title[j].title_set_nr];
                        title = vmg.Titles.title[j];
                        return;
                    }
                }
            }

            Assert.Fail("Failed to find track.");
            throw new Exception();
        }

        private static double[] frames_per_s = { -1.0, 25.00, -1.0, 29.97 };
        private static double dvdtime2msec(dvd_time_t dt)
        {
            double fps = frames_per_s[(dt.frame_u & 0xC0) >> 6];
            long ms = (((dt.hour & 0xF0) >> 3) * 5 + (dt.hour & 0x0F)) * 3600000;
            ms += (((dt.minute & 0xF0) >> 3) * 5 + (dt.minute & 0x0F)) * 60000;
            ms += (((dt.second & 0xF0) >> 3) * 5 + (dt.second & 0x0F)) * 1000;

            if (fps > 0)
                ms += (long)((((dt.frame_u & 0x30) >> 3) * 5 + (dt.frame_u & 0x0F)) * 1000.0 / fps);

            return ms;
        }
        private static string GetFormat(video_attr_t attr)
        {
            switch(attr.video_format)
            {
                case 0: return "NTSC";
                case 1: return "PAL";
                default: return null;
            }
        }
        private static string GetAspect(video_attr_t attr) {
            switch(attr.display_aspect_ratio)
            {
                case 0: return "4/3";
                case 3: return "16/9";
                default: return null;
            }
        }
        private static Size GetSize(video_attr_t attr) {
            if (GetFormat(attr) == "NTSC")
            {
                switch(attr.picture_size)
                {
                    case 0: return new Size(720, 480);
                    case 1: return new Size(704, 480);
                    case 2: return new Size(352, 480);
                    case 3: return new Size(352, 240);
                    default: 
                        Assert.Fail("Invalid picture size.");
                        return new();
                }
            }else if (GetFormat(attr) == "PAL")
            {
                switch (attr.picture_size)
                {
                    case 0: return new Size(720, 576);
                    case 1: return new Size(704, 576);
                    case 2: return new Size(352, 576);
                    case 3: return new Size(352, 288);
                    default:
                        Assert.Fail("Invalid picture size.");
                        return new();
                }
            } else
            {
                Assert.Fail("Invalid region");
                return new();
            }
        }
        private static string GetFormat(audio_attr_t attr)
        {
            switch(attr.audio_format)
            {
                case 0: return "ac3";
                case 2: return "mpeg1";
                case 3: return "mpeg2";
                case 4: return "lpcm";
                case 5: return "sdds"; // todo: confirm
                case 6: return "dts";
                default:
                    Assert.Fail("Invalid audio format");
                    return null;
            }
        }
        private static int GetFrequency(audio_attr_t attr)
        {
            switch(attr.sample_frequency)
            {
                case 0: return 48000;
                case 1: return 96000;
                default:
                    Assert.Fail("Invalid sample frequency");
                    return 0;
            }
        }
        private static string GetQuantization(audio_attr_t attr)
        {
            switch (attr.quantization)
            {
                case 0: return "16bit";
                case 1: return "20bit";
                case 2: return "24bit";
                case 3: return "drc";
                default: 
                    Assert.Fail("Invalid quantization");
                    return null;
            }
        }
        private static uint[] audio_id = { 0x80, 0, 0xC0, 0xC0, 0xA0, 0, 0x88 };
        private static audio_attr_t FindAudioStream(AudioTrack track, VtsIfo vts, pgc_t pgc, title_info_t title, out string streamID)
        {
            for (int i = 0; i < pgc.audio_control.Length; i++)
            {
                if ((pgc.audio_control[i] & 0x8000) == 0)
                {
                    continue;
                }
                var attr = vts.TitlesVobAudioAttributes[i];
                streamID = $"0x{(audio_id[attr.audio_format] + i):X2}";
                if (streamID == track.StreamID)
                {
                    return attr;
                }
            }

            Assert.Fail("Could not find audio stream.");
            throw new Exception();
        }
        private static void converttime(ref playback_time_t pt, dvd_time_t dt)
        {
            double fps = frames_per_s[(dt.frame_u & 0xC0) >> 6];

            pt.usec += (((dt.frame_u & 0x30) >> 3) * 5 + (dt.frame_u & 0x0f)) * (int)(1000.0 / fps);
            pt.second += ((dt.second & 0xf0) >> 3) * 5 + (dt.second & 0x0f);
            pt.minute += ((dt.minute & 0xf0) >> 3) * 5 + (dt.minute & 0x0f);
            pt.hour += ((dt.hour & 0xf0) >> 3) * 5 + (dt.hour & 0x0f);

            if (pt.usec >= 1000) { pt.usec -= 1000; pt.second++; }
            if (pt.second >= 60) { pt.second -= 60; pt.minute++; }
            if (pt.minute > 59) { pt.minute -= 60; pt.hour++; }
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

    internal struct playback_time_t
    {
        public int hour;
        public int minute;
        public int second;
        public int usec;
    }
}
