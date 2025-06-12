
namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// PartOfTitle Search Pointer Table <see cref="http://www.mpucoder.com/DVD/ifo_vts.html#ptt"/>
    /// Same as VMG?
    /// </summary>
    internal class vts_ptt_srpt_t
    {
        internal const uint Size = 8;

        internal ushort nr_of_srpts;
        internal ushort zero_1;
        internal uint last_byte;
        internal vts_ptt_t[] titles;

        /// <summary>
        /// offset table for each ttu
        /// </summary>
        internal uint[] ttu_offset;

        private vts_ptt_srpt_t(Stream file, uint sector)
        {
            file.Seek(sector * DvdUtils.DVD_BLOCK_LEN, SeekOrigin.Begin);

            // Read data
            nr_of_srpts = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            last_byte = file.Read<uint>();

            // Fix endiness
            DvdUtils.B2N_16(ref nr_of_srpts);
            DvdUtils.B2N_32(ref last_byte);

            // Verify
            DvdUtils.CHECK_ZERO(zero_1);
            DvdUtils.CHECK_VALUE(nr_of_srpts != 0);
            DvdUtils.CHECK_VALUE(nr_of_srpts < 100); /* ?? */

            /* E-One releases don't fill this field */
            if (last_byte == 0)
            {
                last_byte = nr_of_srpts * (uint)sizeof(uint) + vts_ptt_srpt_t.Size - 1;
            }

            // Read offsets
            uint info_length = last_byte - vts_ptt_srpt_t.Size + 1;
            if (nr_of_srpts > info_length / sizeof(uint)) throw new IOException("PTT search table too small");
            if (nr_of_srpts == 0) throw new IOException("Zero entries in PTT search table.");    

            ttu_offset = new uint[nr_of_srpts];
            file.Read(ttu_offset);

            for (int i = 0; i < nr_of_srpts; i++) {

                /* Transformers 3 has PTT start bytes that point outside the SRPT PTT */
                DvdUtils.B2N_32(ref ttu_offset[i]);
                /* assert(data[i] + sizeof(ptt_info_t) <= vts_ptt_srpt->last_byte + 1);
                   Magic Knight Rayearth Daybreak is mastered very strange and has
                   Titles with 0 PTTs. They all have a data[i] offsets beyond the end of
                   of the vts_ptt_srpt structure. */
                DvdUtils.CHECK_VALUE(ttu_offset[i] + ptt_info_t.Size <= last_byte + 1 + 4);
            }

            titles = new vts_ptt_t[nr_of_srpts];
            for (int i = 0; i < nr_of_srpts; i++) {
                uint n; // The number of PTT entries in this PTT table
                if (i < nr_of_srpts - 1)
                    n = (ttu_offset[i + 1] - ttu_offset[i]);
                else
                    n = (last_byte + 1 - ttu_offset[i]);

                /* assert(n > 0 && (n % 4) == 0);
                   Magic Knight Rayearth Daybreak is mastered very strange and has
                   Titles with 0 PTTs. */
                if (n < 0) n = 0;

                /* DVDs created by the VDR-to-DVD device LG RC590M violate the following requirement */
                DvdUtils.CHECK_VALUE(n % 4 == 0);
                if (n/4 > 0)
                {
                    /* The assert placed here because of Magic Knight Rayearth Daybreak */
                    DvdUtils.CHECK_VALUE(ttu_offset[i] + ptt_info_t.Size <= last_byte + 1);                
                }
                titles[i] = new vts_ptt_t(file, (sector * DvdUtils.DVD_BLOCK_LEN) + ttu_offset[i], n / 4);
            }
        }

        internal static bool ifoRead_VTS_PTT_SRPT(Stream file, uint sector, out vts_ptt_srpt_t? result)
        {
            try
            {
                result = new vts_ptt_srpt_t(file, sector);
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