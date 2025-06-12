
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Menu PGCI Language Unit <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#pgciut"/>
    /// </summary>
    internal class pgci_lu_t : IStreamReadable<pgci_lu_t>
    {
        internal const uint Size = 8;

        /// <summary>
        /// ISO639 language code
        /// </summary>
        internal ushort lang_code;
        internal byte lang_extension;
        internal byte exists;
        internal uint lang_start_byte;
        internal pgcit_t? pgcit = null;

        private pgci_lu_t(Stream file)
        {
            // Read data
            lang_code = file.Read<ushort>();
            lang_extension = file.Read<byte>();
            exists = file.Read<byte>();
            lang_start_byte = file.Read<uint>();

            // Verify
            DvdUtils.B2N_16(ref lang_code);
            DvdUtils.B2N_32(ref lang_start_byte);

            /* Maybe this is only defined for v1.1 and later titles? */
            /* If the bits in 'lu[i].exists' are enumerated abcd efgh then:
               VTS_x_yy.IFO        VIDEO_TS.IFO
               a == 0x83 "Root"         0x82 "Title"
               b == 0x84 "Subpicture"
               c == 0x85 "Audio"
               d == 0x86 "Angle"
               e == 0x87 "PTT"
            */
            DvdUtils.CHECK_VALUE((exists & 0x07) == 0);
        }

        public static pgci_lu_t? Read(Stream file)
        {
            try
            {
                return new pgci_lu_t(file);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}