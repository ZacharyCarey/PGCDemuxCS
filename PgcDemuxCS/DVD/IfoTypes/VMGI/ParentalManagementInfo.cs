

namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Parental management information unit table
    /// </summary>
    public class ParentalManagementInfo
    {
        internal const uint Size = 8;

        public ushort CountryCode;
        internal ushort pf_ptl_mai_start_byte;

        /// <summary>
        /// Parental Management Information Unit Table.
        /// Level 1 (US: G), ..., 7 (US: NC-17), 8
        /// </summary>
        public ushort[][] pf_ptl_mai;

        internal ParentalManagementInfo(Stream file, ushort nr_of_vtss, uint last_byte, uint offset)
        {
            // Read data
            CountryCode = file.Read<ushort>();
            file.ReadZeros(2);
            pf_ptl_mai_start_byte = file.Read<ushort>();
            file.ReadZeros(2);

            // Verify
            DvdUtils.CHECK_VALUE(pf_ptl_mai_start_byte + 2 * (nr_of_vtss + 1) <= last_byte + 1);
        }
    }
}