
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Cell address table <see cref="http://www.mpucoder.com/DVD/ifo.html#c_adt"/>
    /// </summary>
    internal class c_adt_t
    {
        private const uint Size = 8;

        internal ushort nr_of_vobs; // VOBs
        internal ushort zero_1;
        internal uint last_byte;
        internal cell_adr_t[] cell_adr_table; // No explicit size given

        private c_adt_t(Stream file, uint sector)
        {
            file.Seek(sector * DvdUtils.DVD_BLOCK_LEN, SeekOrigin.Begin);

            // Read data
            nr_of_vobs = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            last_byte = file.Read<uint>();

            // Fix endiness
            DvdUtils.B2N_16(ref nr_of_vobs);
            DvdUtils.B2N_32(ref last_byte);

            // Verify
            if (last_byte + 1 < c_adt_t.Size)
                throw new IOException();

            uint info_length = last_byte + 1 - c_adt_t.Size;

            DvdUtils.CHECK_ZERO(zero_1);
            /* assert(c_adt->nr_of_vobs > 0);
               Magic Knight Rayearth Daybreak is mastered very strange and has
               Titles with a VOBS that has no cells. */
            DvdUtils.CHECK_VALUE(info_length % cell_adr_t.Size == 0);

            /* assert(info_length / sizeof(cell_adr_t) >= c_adt->nr_of_vobs);
               Enemy of the State region 2 (de) has Titles where nr_of_vobs field
               is to high, they high ones are never referenced though. */
            if (info_length / cell_adr_t.Size < nr_of_vobs)
            {
                Console.WriteLine("C_ADT nr_of_vobs > available info entries");
                nr_of_vobs = (ushort)(info_length / cell_adr_t.Size);
            }

            cell_adr_table = new cell_adr_t[nr_of_vobs];
            file.Read<cell_adr_t>(cell_adr_table);

            // Verify
            foreach (var cell in cell_adr_table)
            {
                DvdUtils.CHECK_VALUE(cell.vob_id <= nr_of_vobs);
            }
        }

        internal static bool ifoRead_C_ADT(Stream file, uint sector, out c_adt_t? result)
        {
            try
            {
                result = new c_adt_t(file, sector);
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