

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Multichannel Extension <see cref="http://www.mpucoder.com/DVD/ifo.html#mcext"/> 
    /// </summary>
    public class MultichannelExtension : IStreamReadable<MultichannelExtension>
    {
        public bool ach0_gme; // 1 bit

        public bool ach1_gme; // 1 bit

        public bool ach2_gv1e; // 1 bit
        public bool ach2_gv2e; // 1 bit
        public bool ach2_gm1e; // 1 bit
        public bool ach2_gm2e; // 1 bit

        public bool ach3_gv1e; // 1 bit
        public bool ach3_gv2e; // 1 bit
        public bool ach3_gmAe; // 1 bit
        public bool ach3_se2e; // 1 bit

        public bool ach4_gv1e; // 1 bit
        public bool ach4_gv2e; // 1 bit
        public bool ach4_gmBe; // 1 bit
        public bool ach4_seBe; // 1 bit

        private MultichannelExtension(Stream file)
        {
            BitStream bits = new BitStream(file);

            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(7));
            ach0_gme = bits.ReadBit();

            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(7));
            ach1_gme = bits.ReadBit();

            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(4));
            ach2_gv1e = bits.ReadBit();
            ach2_gv2e = bits.ReadBit();
            ach2_gm1e = bits.ReadBit();
            ach2_gm2e = bits.ReadBit();

            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(4));
            ach3_gv1e = bits.ReadBit();
            ach3_gv2e = bits.ReadBit();
            ach3_gmAe = bits.ReadBit();
            ach3_se2e = bits.ReadBit();

            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(4));
            ach4_gv1e = bits.ReadBit();
            ach4_gv2e = bits.ReadBit();
            ach4_gmBe = bits.ReadBit();
            ach4_seBe = bits.ReadBit();

            file.ReadZeros(19);
        }

        static MultichannelExtension? IStreamReadable<MultichannelExtension>.Read(Stream file, int index)
        {

            return new MultichannelExtension(file);
        }
    }
}