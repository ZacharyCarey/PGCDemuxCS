

namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Parental management information unit table
    /// </summary>
    public class ptl_mait_country_t
    {
        internal const uint Size = 8;

        public ushort country_code;
        public ushort zero_1;
        public ushort pf_ptl_mai_start_byte;
        public ushort zero_2;

        /// <summary>
        /// Parental Management Information Unit Table.
        /// Level 1 (US: G), ..., 7 (US: NC-17), 8
        /// </summary>
        public ushort[][] pf_ptl_mai;

        internal ptl_mait_country_t(Stream file, ushort nr_of_vtss, uint last_byte, uint offset)
        {
            // Read data
            country_code = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            pf_ptl_mai_start_byte = file.Read<ushort>();
            zero_2 = file.Read<ushort>();

            // Verify
            DvdUtils.CHECK_ZERO(zero_1);
            DvdUtils.CHECK_ZERO(zero_2);
            DvdUtils.CHECK_VALUE(pf_ptl_mai_start_byte + 2 * (nr_of_vtss + 1) <= last_byte + 1);
        }
    }
}