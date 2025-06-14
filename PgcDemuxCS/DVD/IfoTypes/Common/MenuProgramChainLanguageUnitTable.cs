using System.Collections;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Menu PGCI Unit Table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#pgciut"/>
    /// </summary>
    public class MenuProgramChainLanguageUnitTable : IEnumerable<MenuProgramChainLanguageUnit>
    {
        private MenuProgramChainLanguageUnit[] lu = Array.Empty<MenuProgramChainLanguageUnit>();

        public int Count => lu.Length;

        public MenuProgramChainLanguageUnit this[int index] => lu[index];

        IEnumerator<MenuProgramChainLanguageUnit> IEnumerable<MenuProgramChainLanguageUnit>.GetEnumerator()
        {
            return ((IEnumerable<MenuProgramChainLanguageUnit>)lu).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return lu.GetEnumerator();
        }

        internal MenuProgramChainLanguageUnitTable(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            ushort nr_of_lus = file.Read<ushort>();
            file.ReadZeros(2);
            uint last_byte = file.Read<uint>();

            // verify
            DvdUtils.CHECK_VALUE(nr_of_lus != 0);
            DvdUtils.CHECK_VALUE(nr_of_lus < 100); /* ?? 3-4 ? */
            DvdUtils.CHECK_VALUE((uint)nr_of_lus * MenuProgramChainLanguageUnit.Size < last_byte);

            // Read table data
            lu = new MenuProgramChainLanguageUnit[nr_of_lus];
            file.Read<MenuProgramChainLanguageUnit>(lu);

            // Find duplicate entries
            for (int i = 0; i < nr_of_lus; i++) {
                int dup;
                if ((dup = find_dup_lut(lu[i].lang_start_byte, i)) >= 0) {
                    lu[i].PgcTable = lu[dup].PgcTable;
                    continue;
                }
                lu[i].PgcTable = new ProgamChainInformationTable(file, offset + lu[i].lang_start_byte, true);
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
    }
}