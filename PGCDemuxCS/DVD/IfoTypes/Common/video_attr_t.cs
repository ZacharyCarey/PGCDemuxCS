
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common {
    /// <summary>
    /// Video attributes
    /// </summary>
    internal class video_attr_t
    {
        internal byte mpeg_version; // 2 bits
        internal byte video_format; // 2 bits
        internal byte display_aspect_ratio; // 2 bits
        internal byte permitted_df; // 2 bits

        internal bool line21_cc_1; // 1 bit
        internal bool line21_cc_2; // 1 bit
        internal bool unknown1; // 1 bit
        internal bool bit_rate; // 1 bit

        internal byte picture_size; // 2 bits
        internal bool letterboxed; // 1 bit
        internal bool film_mode; // 1 bit

        internal video_attr_t(Stream file)
        {
            BitStream bits = new BitStream(file);

            mpeg_version = bits.ReadBits<byte>(2);
            video_format = bits.ReadBits<byte>(2);
            display_aspect_ratio = bits.ReadBits<byte>(2);
            permitted_df = bits.ReadBits<byte>(2);
            line21_cc_1 = bits.ReadBit();
            line21_cc_2 = bits.ReadBit();
            unknown1 = bits.ReadBit();
            bit_rate = bits.ReadBit();
            picture_size = bits.ReadBits<byte>(2);
            letterboxed = bits.ReadBit();
            film_mode = bits.ReadBit();
        }
    }
}