
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Program chain information <see cref="http://www.mpucoder.com/DVD/pgc.html"/>
    /// </summary>
    public class PGC
    {
        public const uint Size = 236;

        public readonly byte NumberOfPrograms;
        public readonly byte NumberOfCells;
        public readonly TimeSpan PlaybackTime;
        public readonly double PlaybackFPS;
        public readonly UserOperations ProhibitedOperations;
        public readonly AudioStreamControl[] AudioStreams = new AudioStreamControl[8];
        public readonly SubpictureStreamControl[] SubpictureStreams = new SubpictureStreamControl[32];
        public readonly ushort NextPgcNumber;
        public readonly ushort PreviousPgcNumber;
        public readonly ushort GroupPgcNumber;
        
        /// <summary>
        /// 0 = Sequential, otherwise bit7 indicated random (0) or shuffle (1), and 
        /// the program count-1 is in bits 6-0 (minimum program count is 2)
        /// </summary>
        public readonly byte PGPlaybackMode;

        /// <summary>
        /// 255 = infinite
        /// </summary>
        public readonly byte StillTime;
        public readonly uint[] palette = new uint[16]; // TODO: New type struct {zero_1, Y, Cr, Cb} ?
        public readonly PgcCommandTable? command_tbl = null;

        /// <summary>
        /// PGC Program map
        /// </summary>
        public readonly byte[] ProgramMap;
        public readonly CellPlaybackInfo[] CellPlayback;
        public readonly CellPositionInfo[] CellPosition;

        internal PGC(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            file.ReadZeros(2);
            NumberOfPrograms = file.Read<byte>();
            NumberOfCells = file.Read<byte>();
            PlaybackTime = file.ReadDuration(out PlaybackFPS);
            ProhibitedOperations = new UserOperations(file);
            file.Read<AudioStreamControl>(AudioStreams);
            file.Read<SubpictureStreamControl>(SubpictureStreams);
            NextPgcNumber = file.Read<ushort>();
            PreviousPgcNumber = file.Read<ushort>();
            GroupPgcNumber = file.Read<ushort>();
            PGPlaybackMode = file.Read<byte>();
            StillTime = file.Read<byte>();
            file.Read(palette);
            ushort command_tbl_offset = file.Read<ushort>();
            ushort program_map_offset = file.Read<ushort>();
            ushort cell_playback_offset = file.Read<ushort>();
            ushort cell_position_offset = file.Read<ushort>();

            DvdUtils.CHECK_VALUE(NumberOfPrograms <= NumberOfCells);

            /* Check that time is 0:0:0:0 also if nr_of_programs == 0 */
            if (NumberOfPrograms == 0)
            {
                DvdUtils.CHECK_ZERO(StillTime);
                DvdUtils.CHECK_ZERO(PGPlaybackMode); /* ?? */
                DvdUtils.CHECK_VALUE(program_map_offset == 0);
                DvdUtils.CHECK_VALUE(cell_playback_offset == 0);
                DvdUtils.CHECK_VALUE(cell_position_offset == 0);
            }
            else
            {
                DvdUtils.CHECK_VALUE(program_map_offset != 0);
                DvdUtils.CHECK_VALUE(cell_playback_offset != 0);
                DvdUtils.CHECK_VALUE(cell_position_offset != 0);
            }

            if (command_tbl_offset != 0)
            {
                command_tbl = new PgcCommandTable(file, offset + command_tbl_offset);
            }

            
            if (program_map_offset != 0 && NumberOfPrograms > 0)
            {
                file.Seek(offset + program_map_offset, SeekOrigin.Begin);
                ProgramMap = new byte[NumberOfPrograms];
                file.Read(ProgramMap);
            } else
            {
                ProgramMap = Array.Empty<byte>();
            }

            if (cell_playback_offset != 0 && NumberOfCells > 0)
            {
                CellPlayback = new CellPlaybackInfo[NumberOfCells];
                CellPlaybackInfo.ifoRead_CELL_PLAYBACK_TBL(file, CellPlayback, offset + cell_playback_offset);
            } else
            {
                CellPlayback = Array.Empty<CellPlaybackInfo>();
            }

            if (cell_position_offset != 0 && NumberOfCells > 0)
            {
                CellPosition = new CellPositionInfo[NumberOfCells];
                CellPositionInfo.ifoRead_CELL_POSITION_TBL(file, CellPosition, offset + cell_position_offset);
            } else
            {
                CellPosition = Array.Empty<CellPositionInfo>();
            }
        }
    }
}