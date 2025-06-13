
namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Video Title Set Attribute Table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#atrt"/>
    /// </summary>
    public class vts_atrt_t
    {
        public const uint Size = 8;

        public ushort nr_of_vtss;
        public ushort zero_1;
        public uint last_byte;
        //public vts_attributes_t[] vts;

        /// <summary>
        /// Offsets table for each vts_attributes
        /// </summary>
        uint[] vts_atrt_offsets;

        private vts_atrt_t(Stream file, uint sector)
        {
            file.Seek(sector * DvdUtils.DVD_BLOCK_LEN, SeekOrigin.Begin);

            // Read data
            nr_of_vtss = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            last_byte = file.Read<uint>();

            // Fix endiness
            DvdUtils.B2N_16(ref nr_of_vtss);
            DvdUtils.B2N_32(ref last_byte);

            // Verify
            DvdUtils.CHECK_ZERO(zero_1);
            DvdUtils.CHECK_VALUE(nr_of_vtss != 0);
            DvdUtils.CHECK_VALUE(nr_of_vtss < 100); /* ?? */
            DvdUtils.CHECK_VALUE((uint)nr_of_vtss * (4 + vts_attributes_t.MIN_SIZE) + vts_atrt_t.Size < last_byte + 1);

            // Read additional data
            vts_atrt_offsets = new uint[nr_of_vtss];
            file.Read(vts_atrt_offsets);

            for (int i = 0; i < nr_of_vtss; i++)
            {
                DvdUtils.B2N_32(ref vts_atrt_offsets[i]); // Fix endiness
                DvdUtils.CHECK_VALUE(vts_atrt_offsets[i] + vts_attributes_t.MIN_SIZE < last_byte + 1); // Verify
            }

            /*vts = new vts_attributes_t[nr_of_vtss];
            for (int i = 0; i < nr_of_vtss; i++) {
                uint offset = vts_atrt_offsets[i];
                vts[i] = new vts_attributes_t(file, (sector * DvdUtils.DVD_BLOCK_LEN) + offset);

                // This assert can't be in ifoRead_VTS_ATTRIBUTES 
                DvdUtils.CHECK_VALUE(offset + vts[i].last_byte <= last_byte + 1);
                // Is this check correct? 
            }*/
        }

        internal static bool ifoRead_VTS_ATRT(Stream file, uint sector, out vts_atrt_t? result)
        {
            try
            {
                result = new vts_atrt_t(file, sector);
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