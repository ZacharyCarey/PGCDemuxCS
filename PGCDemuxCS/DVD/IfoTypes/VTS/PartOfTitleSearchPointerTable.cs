using System.Collections;

namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// PartOfTitle Search Pointer Table <see cref="http://www.mpucoder.com/DVD/ifo_vts.html#ptt"/>
    /// Same as VMG?
    /// </summary>
    public class PartOfTitleSearchPointerTable : IEnumerable<PartOfTitleTable>
    {
        internal const uint Size = 8;

        public int Count => titles.Length;
        public PartOfTitleTable this[int index] => titles[index];

        private PartOfTitleTable[] titles;

        IEnumerator<PartOfTitleTable> IEnumerable<PartOfTitleTable>.GetEnumerator()
        {
            return ((IEnumerable<PartOfTitleTable>)titles).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return titles.GetEnumerator();
        }

        internal PartOfTitleSearchPointerTable(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            ushort nr_of_srpts = file.Read<ushort>();
            file.ReadZeros(2);
            uint last_byte = file.Read<uint>();

            // Verify
            DvdUtils.CHECK_VALUE(nr_of_srpts != 0);
            DvdUtils.CHECK_VALUE(nr_of_srpts < 100); /* ?? */

            /* E-One releases don't fill this field */
            if (last_byte == 0)
            {
                last_byte = nr_of_srpts * (uint)sizeof(uint) + PartOfTitleSearchPointerTable.Size - 1;
            }

            // Read offsets
            uint info_length = last_byte - PartOfTitleSearchPointerTable.Size + 1;
            if (nr_of_srpts > info_length / sizeof(uint)) throw new IOException("PTT search table too small");
            if (nr_of_srpts == 0) throw new IOException("Zero entries in PTT search table.");

            uint[] ttu_offset = new uint[nr_of_srpts];
            file.Read(ttu_offset);

            for (int i = 0; i < nr_of_srpts; i++)
            {

                /* Transformers 3 has PTT start bytes that point outside the SRPT PTT */
                /* assert(data[i] + sizeof(ptt_info_t) <= vts_ptt_srpt->last_byte + 1);
                   Magic Knight Rayearth Daybreak is mastered very strange and has
                   Titles with 0 PTTs. They all have a data[i] offsets beyond the end of
                   of the vts_ptt_srpt structure. */
                DvdUtils.CHECK_VALUE(ttu_offset[i] + PartOfTitleInfo.Size <= last_byte + 1 + 4);
            }

            titles = new PartOfTitleTable[nr_of_srpts];
            for (int i = 0; i < nr_of_srpts; i++)
            {
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
                if (n / 4 > 0)
                {
                    /* The assert placed here because of Magic Knight Rayearth Daybreak */
                    DvdUtils.CHECK_VALUE(ttu_offset[i] + PartOfTitleInfo.Size <= last_byte + 1);
                }
                titles[i] = new PartOfTitleTable(file, offset + ttu_offset[i], n / 4);
            }
        }
    }
}