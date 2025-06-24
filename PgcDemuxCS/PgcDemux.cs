using PgcDemuxCS;
using PgcDemuxCS.DVD.IfoTypes.Common;
using System;

namespace PgcDemuxCS
{
    public class PgcDemux
    {
        private static PgcDemuxApp GetPgcApp(IIfoFileReader reader, int title, bool menu, int pgc, int angle, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.PGC;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedPGC = pgc;
            options.SelectedAngle = angle;
            options.ExportVOB = true;
            return new PgcDemuxApp(reader, options);
        }
        /// <inheritdoc cref="ExtractPgc(IIfoFileReader, int, bool, int, int, string, Action{double}?)"/>
        public static bool ExtractPgc(string dvdRootPath, int title, bool menu, int pgc, int angle, string outputPath, Action<double>? progressCallback = null) => ExtractPgc(new SimpleIfoReader(dvdRootPath), title, menu, pgc, angle, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractPgc(IIfoFileReader reader, int title, bool menu, int pgc, int angle, string outputPath, Action<double>? progressCallback = null)
        {
            return GetPgcApp(reader, title, menu, pgc, angle, outputPath).Run(progressCallback);
        }
        public static long GetPgcBytes(IIfoFileReader reader, int title, bool menu, int pgc, int angle)
        {
            return GetPgcApp(reader, title, menu, pgc, angle, null).GetPgcBytes();
        }

        /// <inheritdoc cref="ExtractPgcVideo(IIfoFileReader, int, bool, int, int, string, Action{double}?)"/>
        public static bool ExtractPgcVideo(string dvdRootPath, int title, bool menu, int pgc, int angle, string outputPath, Action<double>? progressCallback = null) => ExtractPgcVideo(new SimpleIfoReader(dvdRootPath), title, menu, pgc, angle, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractPgcVideo(IIfoFileReader reader, int title, bool menu, int pgc, int angle, string outputPath, Action<double>? progressCallback = null)
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
            return demux.Run(progressCallback);
        }

        /// <inheritdoc cref="ExtractPgcAudio(IIfoFileReader, int, bool, int, int, int, string, Action{double}?)"/>
        public static bool ExtractPgcAudio(string dvdRootPath, int title, bool menu, int pgc, int angle, int streamID, string outputPath, Action<double>? progressCallback = null) => ExtractPgcAudio(new SimpleIfoReader(dvdRootPath), title, menu, pgc, angle, streamID, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractPgcAudio(IIfoFileReader reader, int title, bool menu, int pgc, int angle, int streamID, string outputPath, Action<double>? progressCallback = null)
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
            return demux.Run(progressCallback);
        }

        /// <inheritdoc cref="ExtractPgcSubpicture(IIfoFileReader, int, bool, int, int, int, string, Action{double}?)"/>
        public static bool ExtractPgcSubpicture(string dvdRootPath, int title, bool menu, int pgc, int angle, int streamID, string outputPath, Action<double>? progressCallback = null) => ExtractPgcSubpicture(new SimpleIfoReader(dvdRootPath), title, menu, pgc, angle, streamID, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractPgcSubpicture(IIfoFileReader reader, int title, bool menu, int pgc, int angle, int streamID, string outputPath, Action<double>? progressCallback = null)
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
            return demux.Run(progressCallback);
        }

        private static PgcDemuxApp GetVidApp(IIfoFileReader reader, int title, bool menu, int vid, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.VID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.ExportVOB = true;

            return new PgcDemuxApp(reader, options);
        }
        /// <inheritdoc cref="ExtractVid(IIfoFileReader, int, bool, int, string, Action{double}?)"/>
        public static bool ExtractVid(string dvdRootPath, int title, bool menu, int vid, string outputPath, Action<double>? progressCallback = null) => ExtractVid(new SimpleIfoReader(dvdRootPath), title, menu, vid, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractVid(IIfoFileReader reader, int title, bool menu, int vid, string outputPath, Action<double>? progressCallback = null)
        {
            return GetVidApp(reader, title, menu, vid, outputPath).Run(progressCallback);
        }
        public static long GetVidBytes(IIfoFileReader reader, int title, bool menu, int vid)
        {
            return GetVidApp(reader, title, menu, vid, null).GetVidBytes();
        }

        /// <inheritdoc cref="ExtractVidVideo(IIfoFileReader, int, bool, int, string, Action{double}?)"/>
        public static bool ExtractVidVideo(string dvdRootPath, int title, bool menu, int vid, string outputPath, Action<double>? progressCallback = null) => ExtractVidVideo(new SimpleIfoReader(dvdRootPath), title, menu, vid, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractVidVideo(IIfoFileReader reader, int title, bool menu, int vid, string outputPath, Action<double>? progressCallback = null)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.VID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.ExportVOB = false;
            options.ExtractVideoStream = true;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run(progressCallback);
        }

        /// <inheritdoc cref="ExtractVidAudio(IIfoFileReader, int, bool, int, int, string, Action{double}?)"/>
        public static bool ExtractVidAudio(string dvdRootPath, int title, bool menu, int vid, int streamID, string outputPath, Action<double>? progressCallback = null) => ExtractVidAudio(new SimpleIfoReader(dvdRootPath), title, menu, vid, streamID, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractVidAudio(IIfoFileReader reader, int title, bool menu, int vid, int streamID, string outputPath, Action<double>? progressCallback = null)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.VID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.ExportVOB = false;
            options.ExtractAudioStream = streamID;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run(progressCallback);
        }

        /// <inheritdoc cref="ExtractVidSubpicture(IIfoFileReader, int, bool, int, int, string, Action{double}?)"/>
        public static bool ExtractVidSubpicture(string dvdRootPath, int title, bool menu, int vid, int streamID, string outputPath, Action<double>? progressCallback = null) => ExtractVidSubpicture(new SimpleIfoReader(dvdRootPath), title, menu, vid, streamID, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractVidSubpicture(IIfoFileReader reader, int title, bool menu, int vid, int streamID, string outputPath, Action<double>? progressCallback = null)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.VID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.ExportVOB = false;
            options.ExtractSubtitleStream = streamID;

            var demux = new PgcDemuxApp(reader, options);
            return demux.Run(progressCallback);
        }

        private static PgcDemuxApp GetCidApp(IIfoFileReader reader, int title, bool menu, int vid, int cid, string outputPath)
        {
            string input = (title == 0) ? "VIDEO_TS.IFO" : $"VTS_{title:00}_0.IFO";
            var options = new PgcDemuxOptions(input, outputPath);
            options.Mode = ModeType.CID;
            options.Domain = menu ? DomainType.Menus : DomainType.Titles;
            options.SelectedVID = vid;
            options.SelectedCID = cid;
            options.ExportVOB = true;

            return new PgcDemuxApp(reader, options);
        }
        /// <inheritdoc cref="ExtractCid(IIfoFileReader, int, bool, int, int, string, Action{double}?)"/>
        public static bool ExtractCid(string dvdRootPath, int title, bool menu, int vid, int cid, string outputPath, Action<double>? progressCallback = null) => ExtractCid(new SimpleIfoReader(dvdRootPath), title, menu, vid, cid, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractCid(IIfoFileReader reader, int title, bool menu, int vid, int cid, string outputPath, Action<double>? progressCallback = null)
        {
            return GetCidApp(reader, title, menu, vid, cid, outputPath).Run(progressCallback);
        }
        public static long GetCidBytes(IIfoFileReader reader, int title, bool menu, int vid, int cid)
        {
            return GetCidApp(reader, title, menu, vid, cid, null).GetCidBytes();
        }

        /// <inheritdoc cref="ExtractCidVideo(IIfoFileReader, int, bool, int, int, string, Action{double}?)"/>
        public static bool ExtractCidVideo(string dvdRootPath, int title, bool menu, int vid, int cid, string outputPath, Action<double>? progressCallback = null) => ExtractCidVideo(new SimpleIfoReader(dvdRootPath), title, menu, vid, cid, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractCidVideo(IIfoFileReader reader, int title, bool menu, int vid, int cid, string outputPath, Action<double>? progressCallback = null)
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
            return demux.Run(progressCallback);
        }

        /// <inheritdoc cref="ExtractCidAudio(IIfoFileReader, int, bool, int, int, int, string, Action{double}?)"/>
        public static bool ExtractCidAudio(string dvdRootPath, int title, bool menu, int vid, int cid, int streamID, string outputPath, Action<double>? progressCallback = null) => ExtractCidAudio(new SimpleIfoReader(dvdRootPath), title, menu, vid, cid, streamID, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractCidAudio(IIfoFileReader reader, int title, bool menu, int vid, int cid, int streamID, string outputPath, Action<double>? progressCallback = null)
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
            return demux.Run(progressCallback);
        }

        /// <inheritdoc cref="ExtractCidSubpicture(IIfoFileReader, int, bool, int, int, int, string, Action{double}?)"/>
        public static bool ExtractCidSubpicture(string dvdRootPath, int title, bool menu, int vid, int cid, int streamID, string outputPath, Action<double>? progressCallback = null) => ExtractCidSubpicture(new SimpleIfoReader(dvdRootPath), title, menu, vid, cid, streamID, outputPath, progressCallback);
        /// <summary>
        /// Progress is reported as a float between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static bool ExtractCidSubpicture(IIfoFileReader reader, int title, bool menu, int vid, int cid, int streamID, string outputPath, Action<double>? progressCallback = null)
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
            return demux.Run(progressCallback);
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