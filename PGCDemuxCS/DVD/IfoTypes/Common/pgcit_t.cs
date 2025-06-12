
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Program chain information table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#pgciut"/> 
    /// </summary>
    internal class pgcit_t
    {
        internal ushort nr_of_pgci_srp;
        internal ushort zero_1;
        internal uint last_byte;
        internal pgci_srp_t[] pgci_srp = Array.Empty<pgci_srp_t>();
        internal int ref_count;

        internal pgcit_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            nr_of_pgci_srp = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            last_byte = file.Read<uint>();

            // Fix endiness
            DvdUtils.B2N_16(ref nr_of_pgci_srp);
            DvdUtils.B2N_32(ref last_byte);

            // Verify 
            DvdUtils.CHECK_ZERO(zero_1);
            /* assert(pgcit->nr_of_pgci_srp != 0);
               Magic Knight Rayearth Daybreak is mastered very strange and has
               Titles with 0 PTTs. */
            DvdUtils.CHECK_VALUE(nr_of_pgci_srp < 10000); /* ?? seen max of 1338 */

            if (nr_of_pgci_srp == 0) return;

            pgci_srp = new pgci_srp_t[nr_of_pgci_srp];
            file.Read<pgci_srp_t>(pgci_srp);

            foreach (var pgci in pgci_srp)
            {
                DvdUtils.CHECK_VALUE(pgci.pgc_start_byte + pgc_t.Size <= last_byte + 1);
            }

            // Look for duplicates
            for (int i = 0; i < nr_of_pgci_srp; i++)
            {
                int dup;
                if ((dup = find_dup_pgc(pgci_srp[i].pgc_start_byte, i)) >= 0)
                {
                    pgci_srp[i].pgc = pgci_srp[dup].pgc;
                    pgci_srp[i].pgc.ref_count++;
                    continue;
                }
                pgci_srp[i].pgc = new pgc_t(file, offset + pgci_srp[i].pgc_start_byte);
                pgci_srp[i].pgc.ref_count = 1;
            }
        }

        private int find_dup_pgc(uint start_byte, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (pgci_srp[i].pgc_start_byte == start_byte)
                {
                    return i;
                }
            }
            return -1;
        }

        internal static bool ifoRead_PGCIT(Stream file, uint sector, out pgcit_t? result)
        {
            try
            {
                result = new pgcit_t(file, sector * DvdUtils.DVD_BLOCK_LEN);
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