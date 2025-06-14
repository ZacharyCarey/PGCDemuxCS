
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Program chain information <see cref="http://www.mpucoder.com/DVD/pgc.html"/>
    /// </summary>
    public class pgc_t
    {
        public const uint Size = 236;

        public ushort zero_1;
        public byte nr_of_programs;
        public byte nr_of_cells;
        public dvd_time_t playback_time;
        public user_ops_t prohibited_ops;
        public ushort[] audio_control = new ushort[8]; // TODO: New type? 
        public uint[] subp_control = new uint[32]; // TODO: New type? 
        public ushort next_pgc_nr;
        public ushort prev_pgc_nr;
        public ushort goup_pgc_nr;
        public byte pg_playback_mode;
        public byte still_time;
        public uint[] palette = new uint[16]; // TODO: New type struct {zero_1, Y, Cr, Cb} ?
        public ushort command_tbl_offset;
        public ushort program_map_offset;
        public ushort cell_playback_offset;
        public ushort cell_position_offset;
        public pgc_command_tbl_t? command_tbl = null;

        /// <summary>
        /// PGC Program map
        /// </summary>
        public byte[] program_map = Array.Empty<byte>();
        public cell_playback_t[] cell_playback = Array.Empty<cell_playback_t>();
        public cell_position_t[] cell_position = Array.Empty<cell_position_t>();
        public int ref_count;

        internal pgc_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            zero_1 = file.Read<ushort>();
            nr_of_programs = file.Read<byte>();
            nr_of_cells = file.Read<byte>();
            playback_time = new dvd_time_t(file);
            prohibited_ops = new user_ops_t(file);
            file.Read(audio_control);
            file.Read(subp_control);
            next_pgc_nr = file.Read<ushort>();
            prev_pgc_nr = file.Read<ushort>();
            goup_pgc_nr = file.Read<ushort>();
            pg_playback_mode = file.Read<byte>();
            still_time = file.Read<byte>();
            file.Read(palette);
            command_tbl_offset = file.Read<ushort>();
            program_map_offset = file.Read<ushort>();
            cell_playback_offset = file.Read<ushort>();
            cell_position_offset = file.Read<ushort>();

            DvdUtils.CHECK_ZERO(zero_1);
            DvdUtils.CHECK_VALUE(nr_of_programs <= nr_of_cells);

            /* verify time (look at print_time) */
            for (int i = 0; i < 8; i++)
                if ((audio_control[i] & 0x8000) == 0) /* The 'is present' bit */
                    DvdUtils.CHECK_ZERO(audio_control[i]);
            for (int i = 0; i < 32; i++)
                if ((subp_control[i] & 0x80000000) == 0) /* The 'is present' bit */
                    DvdUtils.CHECK_ZERO(subp_control[i]);

            /* Check that time is 0:0:0:0 also if nr_of_programs == 0 */
            if (nr_of_programs == 0)
            {
                DvdUtils.CHECK_ZERO(still_time);
                DvdUtils.CHECK_ZERO(pg_playback_mode); /* ?? */
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
                command_tbl = new pgc_command_tbl_t(file, offset + command_tbl_offset);
            }

            if (program_map_offset != 0 && nr_of_programs > 0)
            {
                program_map = new byte[nr_of_programs];
                file.Seek(offset + program_map_offset, SeekOrigin.Begin);
                file.Read(program_map);
            }

            if (cell_playback_offset != 0 && nr_of_cells > 0)
            {
                cell_playback = new cell_playback_t[nr_of_cells];
                cell_playback_t.ifoRead_CELL_PLAYBACK_TBL(file, cell_playback, offset + cell_playback_offset);
            }

            if (cell_position_offset != 0 && nr_of_cells > 0)
            {
                cell_position = new cell_position_t[nr_of_cells];
                cell_position_t.ifoRead_CELL_POSITION_TBL(file, cell_position, offset + cell_position_offset);
            }
        }
    }
}