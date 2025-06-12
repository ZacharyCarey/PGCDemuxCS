
namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// Time Map Table <see cref="http://www.mpucoder.com/DVD/ifo_vts.html#tmap"/>
    /// </summary>
    internal class vts_tmapt_t
    {
        internal ushort nr_of_tmaps;
        internal ushort zero_1;
        internal uint last_byte;
        internal vts_tmap_t[] tmap;

        /// <summary>
        /// Offset table for each tmap
        /// </summary>
        uint[] tmap_offset;

        private vts_tmapt_t(Stream file, uint sector)
        {
            file.Seek(sector * DvdUtils.DVD_BLOCK_LEN, SeekOrigin.Begin);

            // Read data
            nr_of_tmaps = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            last_byte = file.Read<uint>();

            DvdUtils.B2N_16(ref nr_of_tmaps);
            DvdUtils.B2N_32(ref last_byte);

            DvdUtils.CHECK_ZERO(zero_1);

            tmap_offset = new uint[nr_of_tmaps];
            file.Read(tmap_offset);
            for (int i = 0; i < nr_of_tmaps; i++)
            {
                DvdUtils.B2N_32(ref tmap_offset[i]);
            }

            tmap = new vts_tmap_t[nr_of_tmaps];
            for (int i = 0; i < nr_of_tmaps; i++)
            {
                tmap[i] = new vts_tmap_t(file, (sector * DvdUtils.DVD_BLOCK_LEN) + tmap_offset[i]);
            }
        }

        internal static bool ifoRead_VTS_TMAPT(Stream file, uint sector, out vts_tmapt_t? result)
        {
            try
            {
                result = new vts_tmapt_t(file, sector);
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