
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Menu PGCI Unit Table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#pgciut"/>
    /// </summary>
    public class pgci_ut_t
    {
        public ushort nr_of_lus;
        public ushort zero_1;
        public uint last_byte;
        public pgci_lu_t[] lu = Array.Empty<pgci_lu_t>();

        private pgci_ut_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            nr_of_lus = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            last_byte = file.Read<uint>();

            // fix endiness
            DvdUtils.B2N_16(ref nr_of_lus);
            DvdUtils.B2N_32(ref last_byte);

            // verify
            DvdUtils.CHECK_ZERO(zero_1);
            DvdUtils.CHECK_VALUE(nr_of_lus != 0);
            DvdUtils.CHECK_VALUE(nr_of_lus < 100); /* ?? 3-4 ? */
            DvdUtils.CHECK_VALUE((uint)nr_of_lus * pgci_lu_t.Size < last_byte);

            // Read table data
            lu = new pgci_lu_t[nr_of_lus];
            file.Read<pgci_lu_t>(lu);

            // Find duplicate entries
            for (int i = 0; i < nr_of_lus; i++) {
                int dup;
                if ((dup = find_dup_lut(lu[i].lang_start_byte, i)) >= 0) {
                    lu[i].pgcit = lu[dup].pgcit;
                    lu[i].pgcit.ref_count++;
                    continue;
                }
                lu[i].pgcit = new pgcit_t(file, offset + lu[i].lang_start_byte);
                /* FIXME: Iterate and verify that all menus that should exists accordingly
                 * to pgci_ut->lu[i].exists really do? */
            }
        }

        private int find_dup_lut(uint start_byte, int count) {
            for (int i = 0; i < count; i++)
            {
                if (this.lu[i].lang_start_byte == start_byte)
                {
                    return i;
                }
            }
            return -1;
        }

        internal static bool ifoRead_PGCI_UT(ifo_handle_t ifo, Stream file, out pgci_ut_t? result)
        {
            uint sector;

            result = null;
            if (ifo.vmgi_mat != null)
            {
                if (ifo.vmgi_mat.vmgm_pgci_ut == 0) return true;
                else sector = ifo.vmgi_mat.vmgm_pgci_ut;
            }
            else if (ifo.vtsi_mat != null)
            {
                if (ifo.vtsi_mat.vtsm_pgci_ut == 0) return true;
                else sector = ifo.vtsi_mat.vtsm_pgci_ut;
            }
            else
            {
                return false;
            }

            try
            {
                result = new pgci_ut_t(file, sector * DvdUtils.DVD_BLOCK_LEN);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}