

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Multichannel Extension <see cref="http://www.mpucoder.com/DVD/ifo.html#mcext"/> 
    /// </summary>
    public class multichannel_ext_t : IStreamReadable<multichannel_ext_t>
    {
        public byte zero1; // 7 bits
        public bool ach0_gme; // 1 bit

        public byte zero2; // 7 bits
        public bool ach1_gme; // 1 bit

        public byte zero3; // 4 bits
        public bool ach2_gv1e; // 1 bit
        public bool ach2_gv2e; // 1 bit
        public bool ach2_gm1e; // 1 bit
        public bool ach2_gm2e; // 1 bit

        public byte zero4; // 4 bits
        public bool ach3_gv1e; // 1 bit
        public bool ach3_gv2e; // 1 bit
        public bool ach3_gmAe; // 1 bit
        public bool ach3_se2e; // 1 bit

        public byte zero5; // 4 bits
        public bool ach4_gv1e; // 1 bit
        public bool ach4_gv2e; // 1 bit
        public bool ach4_gmBe; // 1 bit
        public bool ach4_seBe; // 1 bit

        public byte[] zero6 = new byte[19];

        private multichannel_ext_t(Stream file)
        {
            BitStream bits = new BitStream(file);

            zero1 = bits.ReadBits<byte>(7);
            ach0_gme = bits.ReadBit();

            zero2 = bits.ReadBits<byte>(7);
            ach1_gme = bits.ReadBit();

            zero3 = bits.ReadBits<byte>(4);
            ach2_gv1e = bits.ReadBit();
            ach2_gv2e = bits.ReadBit();
            ach2_gm1e = bits.ReadBit();
            ach2_gm2e = bits.ReadBit();

            zero4 = bits.ReadBits<byte>(4);
            ach3_gv1e = bits.ReadBit();
            ach3_gv2e = bits.ReadBit();
            ach3_gmAe = bits.ReadBit();
            ach3_se2e = bits.ReadBit();

            zero5 = bits.ReadBits<byte>(4);
            ach4_gv1e = bits.ReadBit();
            ach4_gv2e = bits.ReadBit();
            ach4_gmBe = bits.ReadBit();
            ach4_seBe = bits.ReadBit();

            file.Read(zero6);

            DvdUtils.CHECK_ZERO(zero1);
            DvdUtils.CHECK_ZERO(zero2);
            DvdUtils.CHECK_ZERO(zero3);
            DvdUtils.CHECK_ZERO(zero4);
            DvdUtils.CHECK_ZERO(zero5);
            DvdUtils.CHECK_ZERO(zero6);
        }

        static multichannel_ext_t? IStreamReadable<multichannel_ext_t>.Read(Stream file, int index)
        {

            return new multichannel_ext_t(file);
        }
    }
}