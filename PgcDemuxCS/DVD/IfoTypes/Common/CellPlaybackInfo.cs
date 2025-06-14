
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Used to distinguish normal cells from various cell angles.
    /// </summary>
    public enum AngleBlockType : byte
    {
        Normal = 0,
        First = 1,
        Middle = 2,
        Last = 3
    }

    /// <summary>
    /// Distinguish between normal cells and angle cells
    /// </summary>
    public enum BlockType : byte
    {
        Normal = 0,
        Angle = 1
    }

    public enum KaraokeCellType : byte
    {
        None = 0,
        TitlePicture = 1,
        Introduction = 2,

        /// <summary>
        /// Bridge
        /// </summary>
        SongPartOtherThanAClimax = 3,
        SongPartOfTheFirstClimax = 4,
        SongPartOfTheSecondClimax = 5,
        SongPartForMaleVocal = 6,
        SongPartForFemaleVocal = 7,
        SongPartForMixedVoices = 8,

        /// <summary>
        /// Instrumental
        /// </summary>
        InterludePart = 9,
        FadeInOfTheInterlude = 10,
        FadeOutOfTheInterlude = 11,
        FirstEnding = 12,
        SecondEnding = 13,
        Undefined = 14
    }

    /// <summary>
    /// Cell playback information <see cref="http://www.mpucoder.com/DVD/pgc.html#play"/>
    /// </summary>
    public class CellPlaybackInfo
    {
        /// <summary>
        /// Used to separate angle blocks
        /// </summary>
        public readonly AngleBlockType CellType; // 2 bits
        public readonly BlockType BlockType; // 2 bits
        public readonly bool SeamlessPlay; // 1 bit
        public readonly bool Interleaved; // 1 bit
        public readonly bool StcDiscontinuity; // 1 bit
        public readonly bool SeamlessAngle; // 1 bit
        public readonly bool VobuStillMode;
        public readonly bool Restricted; // 1 bit. Stops trick play
        public readonly KaraokeCellType KaraokeType; // 5 bits. 
        public readonly byte StillTime;
        public readonly byte CellCommandNumber;
        public readonly TimeSpan PlaybackTime;
        public readonly double PlaybackFPS;

        /// <summary>
        /// First sector in the first VOBU (first VOB file).
        /// Cells can cross over multiple VOB files.
        /// </summary>
        public readonly uint FirstSector;
        public readonly uint FirstILVUEndSector;

        /// <summary>
        /// First sector in the last VOBU file.
        /// </summary>
        public readonly uint LastVobuStartSector;

        /// <summary>
        /// Last sector in the last VOBU (last VOB file)
        /// Cells can cross over multiple VOB files.
        /// </summary>
        public readonly uint LastSector;

        private CellPlaybackInfo(Stream file)
        {
            BitStream bits = new BitStream(file);

            // Read data
            CellType = (AngleBlockType)bits.ReadBits<byte>(2);
            BlockType = (BlockType)bits.ReadBits<byte>(2);
            SeamlessPlay = bits.ReadBit();
            Interleaved = bits.ReadBit();
            StcDiscontinuity = bits.ReadBit();
            SeamlessAngle = bits.ReadBit();
            DvdUtils.CHECK_ZERO(bits.ReadBit());
            VobuStillMode = bits.ReadBit();
            Restricted = bits.ReadBit();
            KaraokeType = (KaraokeCellType)bits.ReadBits<byte>(5);
            if ((int)KaraokeType >= 14) KaraokeType = KaraokeCellType.Undefined;
            StillTime = file.Read<byte>();
            CellCommandNumber = file.Read<byte>();
            PlaybackTime = file.ReadDuration(out PlaybackFPS);
            FirstSector = file.Read<uint>();
            FirstILVUEndSector = file.Read<uint>();
            LastVobuStartSector = file.Read<uint>();
            LastSector = file.Read<uint>();

            /* Changed < to <= because this was false in the movie 'Pi'. */
            DvdUtils.CHECK_VALUE(LastVobuStartSector <= LastSector);
            DvdUtils.CHECK_VALUE(FirstSector <= LastVobuStartSector);
        }

        internal static void ifoRead_CELL_PLAYBACK_TBL(Stream file, CellPlaybackInfo[] cell_playback, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < cell_playback.Length; i++)
            {
                cell_playback[i] = new CellPlaybackInfo(file);
            }
        }
    }
}