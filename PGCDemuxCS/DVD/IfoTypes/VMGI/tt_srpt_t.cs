
namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// PartOfTitle Search Pointer Table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#tt"/>
    /// </summary>
    internal class tt_srpt_t
    {
        internal const uint Size = 8; // Only the header, doesnt include table data

        internal ushort nr_of_srpts;
        internal ushort zero_1;
        internal uint last_byte;
        internal title_info_t[] title;

        private tt_srpt_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read Data
            nr_of_srpts = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            last_byte = file.Read<uint>();

            // Fix endiness
            DvdUtils.B2N_16(ref nr_of_srpts);
            DvdUtils.B2N_32(ref last_byte);

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

        internal static bool ifoRead_TT_SRPT(ifo_handle_t ifofile, Stream file, out tt_srpt_t result)
        {
            try
            {
                result = new tt_srpt_t(file, ifofile.vmgi_mat.tt_srpt * DvdUtils.DVD_BLOCK_LEN);
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