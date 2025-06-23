using PgcDemuxCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgcDemuxCS
{
    internal class PgcDemuxOptions
    {
        /// <summary>
        /// The selected PGC to demux. Only works in PGC mode.
        /// </summary>
        public int SelectedPGC = 1;
        internal int m_nSelPGC => SelectedPGC - 1; // Internally from 0 to nPGCs-1

        /// <summary>
        /// The selected angle to demux. Only works in PGC title mode.
        /// </summary>
        public int SelectedAngle = 1;
        internal int m_nSelAng => SelectedAngle - 1; // internally from 0 to nAngs-1.

        /// <summary>
        /// How the files should be demuxed.
        /// A PGC is equivalent to a playlist.
        /// A VID is equivalent to individual video clips.
        /// in CID mode, both VID and CID are used.
        /// </summary>
        public ModeType Mode = ModeType.PGC;
        internal ModeType m_iMode => Mode;

        /// <summary>
        /// Which files to demux. Menu and title files are
        /// stored separately.
        /// </summary>
        public DomainType Domain = DomainType.Titles;
        internal DomainType m_iDomain => Domain;

        /// <summary>
        /// The selected VID to demux. Only works in VID mode or CID mode.
        /// </summary>
        public int SelectedVID = 1;
        internal int m_nVid => SelectedVID;

        /// <summary>
        /// The selected CID to demux, according to the selected VID.
        /// Only works in CID mode.
        /// </summary>
        public int SelectedCID = 1;
        internal int m_nCid => SelectedCID;

        /// <summary>
        /// Extract the selected video section into a complete VOB file.
        /// This would be a file that can be read by ffmpeg or similar.
        /// </summary>
        public bool ExportVOB = false;
        internal bool m_bCheckVob => ExportVOB;
        internal bool m_bCheckVob2 => false; // Split VOB to limit file size to 1GB
        internal bool m_bCheckVideoPack => true; // Write video packs to VOB
        internal bool m_bCheckAudioPack => true; // Write audio packs to VOB
        internal bool m_bCheckNavPack => true; // Write nav packs to VOB
        internal bool m_bCheckSubPack => true; // Write subtitle packs to VOB
        internal bool m_bCheckIFrame => false; // Write only the first I frame
        internal bool m_bCheckLBA => true; // Patch LBA number

        /// <summary>
        /// Extracts the video stream to it's own file
        /// </summary>
        public bool ExtractVideoStream = false;
        internal bool m_bCheckVid => ExtractVideoStream;

        /// <summary>
        /// Extracts the audio stream to it's own file
        /// </summary>
        public int ExtractAudioStream = -1;
        internal int m_bCheckAud => ExtractAudioStream;

        /// <summary>
        /// Extracts the subtitle stream to it's own file
        /// </summary>
        public int ExtractSubtitleStream = -1;
        internal int m_bCheckSub => ExtractSubtitleStream;



        internal bool m_bCheckCellt => false; // Exports cell times to a file
        internal bool m_bCheckEndTime => false; // Includes end time in Cellt file
        internal bool m_bCheckLog => false; // Creates a log file

        internal readonly string m_csInputIFO;
        internal string m_csOutputPath; // TODO make readonly

        // TODO try to remove and fix in tests
        public string Destination { set => m_csOutputPath = value; }

        public PgcDemuxOptions(string ifoName, string outputPath) {
            m_csInputIFO = ifoName;
            m_csOutputPath = outputPath;
        }

        internal void VerifyInputs()
        {
            if (SelectedPGC < 1 || SelectedPGC > 255) throw new ArgumentException("Invalid pgc number.");
            if (SelectedAngle < 1 || SelectedAngle > 9) throw new ArgumentException("Invalid angle number.");
            if (SelectedVID < 1 || SelectedVID > 32768) throw new ArgumentException("Invalid VID number.");
            if (SelectedCID < 1 || SelectedCID > 255) throw new ArgumentException("Invalid CID number.");
        }
    }
}
