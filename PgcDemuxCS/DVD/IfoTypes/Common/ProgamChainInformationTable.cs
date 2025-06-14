
using System.Collections;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Program chain information table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#pgciut"/> 
    /// </summary>
    public class ProgamChainInformationTable : IEnumerable<ProgramChainInfoSearchPointer>
    {
        private ProgramChainInfoSearchPointer[] pgci_srp;

        public int Count => pgci_srp.Length;
        public ProgramChainInfoSearchPointer this[int index] => pgci_srp[index];

        IEnumerator<ProgramChainInfoSearchPointer> IEnumerable<ProgramChainInfoSearchPointer>.GetEnumerator()
        {
            return ((IEnumerable<ProgramChainInfoSearchPointer>)pgci_srp).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return pgci_srp.GetEnumerator();
        }

        internal ProgamChainInformationTable(Stream file, uint offset, bool isVMG)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            ushort nr_of_pgci_srp = file.Read<ushort>();
            file.ReadZeros(2);
            uint last_byte = file.Read<uint>();

            // Verify 
            /* assert(pgcit->nr_of_pgci_srp != 0);
               Magic Knight Rayearth Daybreak is mastered very strange and has
               Titles with 0 PTTs. */
            DvdUtils.CHECK_VALUE(nr_of_pgci_srp < 10000); /* ?? seen max of 1338 */

            if (nr_of_pgci_srp == 0)
            {
                pgci_srp = Array.Empty<ProgramChainInfoSearchPointer>();
                return;
            }

            pgci_srp = new ProgramChainInfoSearchPointer[nr_of_pgci_srp];
            for (int i = 0; i < pgci_srp.Length; i++)
            {
                pgci_srp[i] = new ProgramChainInfoSearchPointer(file, isVMG);
                DvdUtils.CHECK_VALUE(pgci_srp[i].Offset + PGC.Size <= last_byte + 1);
            }

            // Look for duplicates and create PGCs
            for (int i = 0; i < nr_of_pgci_srp; i++)
            {
                int dup;
                if ((dup = find_dup_pgc(pgci_srp[i].Offset, i)) >= 0)
                {
                    pgci_srp[i].Pgc = pgci_srp[dup].Pgc;
                    continue;
                }
                pgci_srp[i].Pgc = new PGC(file, offset + pgci_srp[i].Offset);
            }

            foreach (var pgci in pgci_srp)
            {
                DvdUtils.CHECK_VALUE(pgci != null);
            }
        }

        private int find_dup_pgc(uint start_byte, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (pgci_srp[i].Offset == start_byte)
                {
                    return i;
                }
            }
            return -1;
        }

    }
}