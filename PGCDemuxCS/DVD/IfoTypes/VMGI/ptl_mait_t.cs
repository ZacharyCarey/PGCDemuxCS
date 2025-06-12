
namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Parental management information table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#mait"/>
    /// </summary>
    internal class ptl_mait_t
    {
        internal const uint Size = 8;
        internal const uint PTL_MAIT_NUM_LEVEL = 8;

        internal ushort nr_of_countries;
        internal ushort nr_of_vtss;
        internal uint last_byte;
        internal ptl_mait_country_t[] countries = Array.Empty<ptl_mait_country_t>();

        private ptl_mait_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            nr_of_countries = file.Read<ushort>();
            nr_of_vtss = file.Read<ushort>();
            last_byte = file.Read<uint>();

            // Fix endiness
            DvdUtils.B2N_16(ref nr_of_countries);
            DvdUtils.B2N_16(ref nr_of_vtss);
            DvdUtils.B2N_32(ref last_byte);

            // Verify
            DvdUtils.CHECK_VALUE(nr_of_countries != 0);
            DvdUtils.CHECK_VALUE(nr_of_countries < 100); /* ?? */
            DvdUtils.CHECK_VALUE(nr_of_vtss != 0);
            DvdUtils.CHECK_VALUE(nr_of_vtss < 100); /* ?? */
            DvdUtils.CHECK_VALUE(nr_of_countries * ptl_mait_country_t.Size <= last_byte + 1 - ptl_mait_t.Size);

            // Parse countries
            countries = new ptl_mait_country_t[nr_of_countries];
            for (int i = 0; i < countries.Length; i++)
            {
                countries[i] = new ptl_mait_country_t(file, nr_of_vtss, last_byte, offset);
            }

            // Parse pf_ptl_mai data
            Parse_pf_ptl_mai(file, offset);
        }

        private void Parse_pf_ptl_mai(Stream file, uint offset)
        {
            for (int i = 0; i < nr_of_countries; i++)
            {
                file.Seek(offset + countries[i].pf_ptl_mai_start_byte, SeekOrigin.Begin);
                ushort[] pf_temp = new ushort[(nr_of_vtss + 1) * 8];
                file.Read(pf_temp); ;

                for (int j = 0; j < ((nr_of_vtss + 1U) * 8U); j++)
                {
                    DvdUtils.B2N_16(ref pf_temp[j]);
                }
                countries[i].pf_ptl_mai = new ushort[nr_of_vtss + 1U][]; // Create all PTL_MAIT's
                for (int j = 0; j < countries[i].pf_ptl_mai.Length; j++)
                    countries[i].pf_ptl_mai[j] = new ushort[8]; // one ushort for each level of the PTL_MAIT

                // Transpose the array so we can use C indexing. 
                int level, vts;
                for (level = 0; level < PTL_MAIT_NUM_LEVEL; level++)
                {
                    for (vts = 0; vts <= nr_of_vtss; vts++)
                    {
                        countries[i].pf_ptl_mai[vts][level] =
                            pf_temp[(7 - level) * (nr_of_vtss + 1) + vts];
                    }
                }
                
            }
        }

        internal static bool ifoRead_PTL_MAIT(ifo_handle_t ifo, Stream file, out ptl_mait_t? result)
        {
            try
            {
                result = new ptl_mait_t(file, ifo.vmgi_mat.ptl_mait * DvdUtils.DVD_BLOCK_LEN);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

    }
}