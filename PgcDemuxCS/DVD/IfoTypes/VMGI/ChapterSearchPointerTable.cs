using System.Collections;

namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// PartOfTitle Search Pointer Table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#tt"/>
    /// </summary>
    public class ChapterSearchPointerTable : IEnumerable<ChapterInfo>
    {
        internal const uint Size = 8; // Only the header, doesnt include table data

        private ChapterInfo[] title;

        public int Count => title.Length;

        public ChapterInfo this[int index] => title[index];

        IEnumerator<ChapterInfo> IEnumerable<ChapterInfo>.GetEnumerator()
        {
            return ((IEnumerable<ChapterInfo>)title).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return title.GetEnumerator();
        }

        internal ChapterSearchPointerTable(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read Data
            ushort nr_of_srpts = file.Read<ushort>();
            file.ReadZeros(2);
            uint last_byte = file.Read<uint>();

            /* E-One releases don't fill this field */
            if (last_byte == 0)
            {
                last_byte = nr_of_srpts * ChapterInfo.Size + ChapterSearchPointerTable.Size - 1;
            }

            // Read table data
            var info_length = last_byte + 1 - ChapterSearchPointerTable.Size;
            if (nr_of_srpts > info_length / ChapterInfo.Size)
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"data mismatch: info length ({info_length / ChapterInfo.Size}) != nr_of_srpts ({nr_of_srpts}). Truncating.");
                Console.ResetColor();
                nr_of_srpts = (ushort)(info_length / ChapterInfo.Size);
            }

            title = new ChapterInfo[nr_of_srpts];
            file.Read<ChapterInfo>(title);

            DvdUtils.CHECK_VALUE(nr_of_srpts != 0);
            DvdUtils.CHECK_VALUE(nr_of_srpts < 100); /* ?? */
            DvdUtils.CHECK_VALUE(nr_of_srpts * ChapterInfo.Size <= info_length);
        }
    }
}