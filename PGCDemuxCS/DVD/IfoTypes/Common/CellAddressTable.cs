using System.Collections;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Cell address table <see cref="http://www.mpucoder.com/DVD/ifo.html#c_adt"/>
    /// </summary>
    public class CellAddressTable : IEnumerable<CellAddress>
    {
        private const uint Size = 8;

        /// <summary>
        /// This value has been found to be inconsistant across discs.
        /// The <see cref="Count"/> array length should be used intead, but
        /// this field is still provided in case it's needed.
        /// </summary>
        [Obsolete]
        public ushort NumberOfVOBs;

        /// <summary>
        /// A list of cells in this table
        /// </summary>
        private CellAddress[] Cells;

        public int Count => Cells.Length;
        public CellAddress this[int index]
        {
            get => Cells[index];
        }

        IEnumerator<CellAddress> IEnumerable<CellAddress>.GetEnumerator()
        {
            return ((IEnumerable<CellAddress>)Cells).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Cells.GetEnumerator();
        }

        internal CellAddressTable(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            NumberOfVOBs = file.Read<ushort>();
            file.ReadZeros(2);
            uint last_byte = file.Read<uint>();

            // Verify
            if (last_byte + 1 < CellAddressTable.Size)
                throw new IOException();

            uint info_length = last_byte + 1 - CellAddressTable.Size;

            /* assert(c_adt->nr_of_vobs > 0);
               Magic Knight Rayearth Daybreak is mastered very strange and has
               Titles with a VOBS that has no cells. */
            DvdUtils.CHECK_VALUE(info_length % CellAddress.Size == 0);

            /* assert(info_length / sizeof(cell_adr_t) >= c_adt->nr_of_vobs);
               Enemy of the State region 2 (de) has Titles where nr_of_vobs field
               is to high, they high ones are never referenced though. */

            // nr_of_vobs field appears to be unreliable, so calculate the actual value from the size of the table
            int nr_of_vobs = (ushort)(info_length / CellAddress.Size);

            Cells = new CellAddress[nr_of_vobs];
            file.Read<CellAddress>(Cells);

            // Verify
            foreach (var cell in Cells)
            {
                DvdUtils.CHECK_VALUE(cell.VobID <= nr_of_vobs);
            }
        }
    }
}