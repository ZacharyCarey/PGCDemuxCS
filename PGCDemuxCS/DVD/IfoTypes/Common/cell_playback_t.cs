
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Cell playback information <see cref="http://www.mpucoder.com/DVD/pgc.html#play"/>
    /// </summary>
    public class cell_playback_t
    {
        public byte block_mode; // 2 bits
        public byte block_type; // 2 bits
        public bool seamless_play; // 1 bit
        public bool interleaved; // 1 bit
        public bool stc_discontinuity; // 1 bit
        public bool seamless_angle; // 1 bit
        public bool zero_1; // 1 bit
        public bool playback_mode;  // 1 bit. When set, enter StillMode after each VOBU
        public bool restricted; // 1 bit. drop out of fastforward? 
        public byte cell_type; // 5 bits. for karaoke, reserved otherwise 
        public byte still_time;
        public byte cell_cmd_nr;
        public dvd_time_t playback_time;
        public uint first_sector;
        public uint first_ilvu_end_sector;
        public uint last_vobu_start_sector;
        public uint last_sector;

        // TODO TEMP remove
        public byte iCat => (byte)((block_mode << 6) | (block_type << 4) | ((seamless_play ? 1 : 0) << 3) | ((interleaved ? 1 : 0) << 2) | ((stc_discontinuity ? 1 : 0) << 1) | (seamless_angle ? 1 : 0));

        private cell_playback_t(Stream file)
        {
            BitStream bits = new BitStream(file);

            // Read data
            block_mode = bits.ReadBits<byte>(2);
            block_type = bits.ReadBits<byte>(2);
            seamless_play = bits.ReadBit();
            interleaved = bits.ReadBit();
            stc_discontinuity = bits.ReadBit();
            seamless_angle = bits.ReadBit();
            zero_1 = bits.ReadBit();
            playback_mode = bits.ReadBit();
            restricted = bits.ReadBit();
            cell_type = bits.ReadBits<byte>(5);
            still_time = bits.Read<byte>();
            cell_cmd_nr = bits.Read<byte>();
            playback_time = new dvd_time_t(bits);
            first_sector = bits.Read<uint>();
            first_ilvu_end_sector = bits.Read<uint>();
            last_vobu_start_sector = bits.Read<uint>();
            last_sector = bits.Read<uint>();

            /* Changed < to <= because this was false in the movie 'Pi'. */
            DvdUtils.CHECK_VALUE(last_vobu_start_sector <= last_sector);
            DvdUtils.CHECK_VALUE(first_sector <= last_vobu_start_sector);
        }

        internal static void ifoRead_CELL_PLAYBACK_TBL(Stream file, cell_playback_t[] cell_playback, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < cell_playback.Length; i++)
            {
                cell_playback[i] = new cell_playback_t(file);
            }
        }
    }
}