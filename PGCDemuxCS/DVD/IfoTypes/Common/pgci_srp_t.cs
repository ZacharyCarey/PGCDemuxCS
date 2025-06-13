
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Program chain information search pointer <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#pgciut"/>
    /// </summary>
    public class pgci_srp_t : IStreamReadable<pgci_srp_t>
    {
        public byte entry_id;
        public byte block_mode; // 2 bits
        public byte block_type; // 2 bits
        public byte zero_1; // 4 bits
        public ushort ptl_id_mask;
        public uint pgc_start_byte;
        public pgc_t? pgc = null;

        private pgci_srp_t(Stream file)
        {
            BitStream bits = new BitStream(file);

            // Read data
            entry_id = bits.Read<byte>();
            block_mode = bits.ReadBits<byte>(2);
            block_type = bits.ReadBits<byte>(2);
            zero_1 = bits.ReadBits<byte>(4);
            ptl_id_mask = bits.Read<ushort>();
            pgc_start_byte = bits.Read<uint>();

            DvdUtils.CHECK_VALUE(zero_1 == 0);
        }

        public static pgci_srp_t? Read(Stream file)
        {
            try
            {
                return new pgci_srp_t(file);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}