
namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// PartOfTitle Search Pointer Table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#tt"/>
    /// </summary>
    public class tt_srpt_t
    {
        public const uint Size = 8; // Only the header, doesnt include table data

        public ushort nr_of_srpts;
        public ushort zero_1;
        public uint last_byte;
        public title_info_t[] title;

        internal tt_srpt_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read Data
            nr_of_srpts = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            last_byte = file.Read<uint>();

            /* E-One releases don't fill this field */
            if (last_byte == 0)
            {
                last_byte = nr_of_srpts * title_info_t.Size + tt_srpt_t.Size - 1;
            }

            // Read table data
            var info_length = last_byte + 1 - tt_srpt_t.Size;
            if (nr_of_srpts > info_length / title_info_t.Size)
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"data mismatch: info length ({info_length / title_info_t.Size}) != nr_of_srpts ({nr_of_srpts}). Truncating.");
                Console.ResetColor();
                nr_of_srpts = (ushort)(info_length / title_info_t.Size);
            }

            title = new title_info_t[nr_of_srpts];
            file.Read<title_info_t>(title);

            DvdUtils.CHECK_ZERO(zero_1);
            DvdUtils.CHECK_VALUE(nr_of_srpts != 0);
            DvdUtils.CHECK_VALUE(nr_of_srpts < 100); /* ?? */
            DvdUtils.CHECK_VALUE(nr_of_srpts * title_info_t.Size <= info_length);
        }
    }
}