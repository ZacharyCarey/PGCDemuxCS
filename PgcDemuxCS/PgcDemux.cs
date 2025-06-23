using PgcDemuxCS;
using PgcDemuxCS.DVD.IfoTypes.Common;
using System;

namespace PgcDemuxCS
{
    public class PgcDemux
    {
        public static bool ExtractPgc(string dvdRootPath, int title, bool menu, int pgc, int angle, string outputPath) => ExtractPgc(new SimpleIfoReader(dvdRootPath), title, menu, pgc, angle, outputPath);
        public static bool ExtractPgc(IIfoFileReader reader, int title, bool menu, int pgc, int angle, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.PGC;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedPGC = pgc;
            options.SelectedAngle = angle;
            options.ExportVOB = true;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractPgcVideo(string dvdRootPath, int title, bool menu, int pgc, int angle, string outputPath) => ExtractPgcVideo(new SimpleIfoReader(dvdRootPath), title, menu, pgc, angle, outputPath);
        public static bool ExtractPgcVideo(IIfoFileReader reader, int title, bool menu, int pgc, int angle, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.PGC;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedPGC = pgc;
            options.SelectedAngle = angle;
            options.ExportVOB = false;
            options.ExtractVideoStream = true;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractPgcAudio(string dvdRootPath, int title, bool menu, int pgc, int angle, int streamID, string outputPath) => ExtractPgcAudio(new SimpleIfoReader(dvdRootPath), title, menu, pgc, angle, streamID, outputPath);
        public static bool ExtractPgcAudio(IIfoFileReader reader, int title, bool menu, int pgc, int angle, int streamID, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.PGC;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedPGC = pgc;
            options.SelectedAngle = angle;
            options.ExportVOB = false;
            options.ExtractAudioStream = streamID;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractPgcSubpicture(string dvdRootPath, int title, bool menu, int pgc, int angle, int streamID, string outputPath) => ExtractPgcSubpicture(new SimpleIfoReader(dvdRootPath), title, menu, pgc, angle, streamID, outputPath);
        public static bool ExtractPgcSubpicture(IIfoFileReader reader, int title, bool menu, int pgc, int angle, int streamID, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.PGC;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedPGC = pgc;
            options.SelectedAngle = angle;
            options.ExportVOB = false;
            options.ExtractSubtitleStream = streamID;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractVid(string dvdRootPath, int title, bool menu, int vid, string outputPath) => ExtractVid(new SimpleIfoReader(dvdRootPath), title, menu, vid, outputPath);
        public static bool ExtractVid(IIfoFileReader reader, int title, bool menu, int vid, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.VID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.ExportVOB = true;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractVidVideo(string dvdRootPath, int title, bool menu, int vid, string outputPath) => ExtractVidVideo(new SimpleIfoReader(dvdRootPath), title, menu, vid, outputPath);
        public static bool ExtractVidVideo(IIfoFileReader reader, int title, bool menu, int vid, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.VID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.ExportVOB = false;
            options.ExtractVideoStream = true;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractVidAudio(string dvdRootPath, int title, bool menu, int vid, int streamID, string outputPath) => ExtractVidAudio(new SimpleIfoReader(dvdRootPath), title, menu, vid, streamID, outputPath);
        public static bool ExtractVidAudio(IIfoFileReader reader, int title, bool menu, int vid, int streamID, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.VID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.ExportVOB = false;
            options.ExtractAudioStream = streamID;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractVidSubpicture(string dvdRootPath, int title, bool menu, int vid, int streamID, string outputPath) => ExtractVidSubpicture(new SimpleIfoReader(dvdRootPath), title, menu, vid, streamID, outputPath);
        public static bool ExtractVidSubpicture(IIfoFileReader reader, int title, bool menu, int vid, int streamID, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.VID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.ExportVOB = false;
            options.ExtractSubtitleStream = streamID;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractCid(string dvdRootPath, int title, bool menu, int vid, int cid, string outputPath) => ExtractCid(new SimpleIfoReader(dvdRootPath), title, menu, vid, cid, outputPath);
        public static bool ExtractCid(IIfoFileReader reader, int title, bool menu, int vid, int cid, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.CID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.SelectedCID = cid;
            options.ExportVOB = true;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractCidVideo(string dvdRootPath, int title, bool menu, int vid, int cid, string outputPath) => ExtractCidVideo(new SimpleIfoReader(dvdRootPath), title, menu, vid, cid, outputPath);
        public static bool ExtractCidVideo(IIfoFileReader reader, int title, bool menu, int vid, int cid, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.CID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.SelectedCID = cid;
            options.ExportVOB = false;
            options.ExtractVideoStream = true;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractCidAudio(string dvdRootPath, int title, bool menu, int vid, int cid, int streamID, string outputPath) => ExtractCidAudio(new SimpleIfoReader(dvdRootPath), title, menu, vid, cid, streamID, outputPath);
        public static bool ExtractCidAudio(IIfoFileReader reader, int title, bool menu, int vid, int cid, int streamID, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.CID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.SelectedCID = cid;
            options.ExportVOB = false;
            options.ExtractAudioStream = streamID;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static bool ExtractCidSubpicture(string dvdRootPath, int title, bool menu, int vid, int cid, int streamID, string outputPath) => ExtractCidSubpicture(new SimpleIfoReader(dvdRootPath), title, menu, vid, cid, streamID, outputPath);
        public static bool ExtractCidSubpicture(IIfoFileReader reader, int title, bool menu, int vid, int cid, int streamID, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.CID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.SelectedCID = cid;
            options.ExportVOB = false;
            options.ExtractSubtitleStream = streamID;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run();
        }

        public static IfoInfo GetIfoInfo(IIfoFileReader fileReader, int title)
        {
            // Temporary options
            var options = new PgcDemuxOptions((title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO", null);
            var data = new IfoData(fileReader, options);

            //Save relevant info
            return new IfoInfo(data);
        }
    }

    public class IfoInfo
    {
        public List<int> MenuPGCs = new();
        public List<(int PGC, int NumberOfAngles)> TitlePGCs = new();
        public List<int> MenuVIDs = new();
        public List<int> TitleVIDs = new();
        public List<(int VID, int CID)> MenuCells = new();
        public List<(int VID, int CID)> TitleCells = new();

        internal IfoInfo(IfoData data)
        {
            for (int i = 0; i < data.MenuInfo.m_nPGCs; i++)
            {
                var nLU = data.m_nLU_MPGC[i];
                var nPGC = i - data.m_nIniPGCinLU[nLU];
                var nCells = data.MenuInfo.m_nCells[i];
                if (nCells > 0)
                {
                    MenuPGCs.Add(i + 1);
                }
            }

            for (int i = 0; i < data.TitleInfo.m_nPGCs; i++)
            {
                var nCells = data.TitleInfo.m_nCells[i];
                if (nCells > 0)
                {
                    TitlePGCs.Add((i + 1, data.m_nAngles[i]));
                }
            }

            for (int i = 0; i < data.MenuInfo.m_AADT_Vid_list.GetSize(); i++)
            {
                var nCells = data.MenuInfo.m_AADT_Vid_list[i].nCells;
                if (nCells > 0)
                {
                    MenuVIDs.Add(data.MenuInfo.m_AADT_Vid_list[i].VID);
                }
            }

            for (int i = 0; i < data.TitleInfo.m_AADT_Vid_list.GetSize(); i++)
            {
                var nCells = data.TitleInfo.m_AADT_Vid_list[i].nCells;
                if (nCells > 0)
                {
                    TitleVIDs.Add(data.TitleInfo.m_AADT_Vid_list[i].VID);
                }
            }

            for (int i = 0; i < data.MenuInfo.m_AADT_Cell_list.GetSize(); i++)
            {
                int vid = data.MenuInfo.m_AADT_Cell_list[i].VID;
                int cid = data.MenuInfo.m_AADT_Cell_list[i].CID;
                MenuCells.Add((vid, cid));
            }

            for (int i = 0; i < data.TitleInfo.m_AADT_Cell_list.GetSize(); i++)
            {
                int vid = data.TitleInfo.m_AADT_Cell_list[i].VID;
                int cid = data.TitleInfo.m_AADT_Cell_list[i].CID;
                TitleCells.Add((vid, cid));
            }
        }

    }
}