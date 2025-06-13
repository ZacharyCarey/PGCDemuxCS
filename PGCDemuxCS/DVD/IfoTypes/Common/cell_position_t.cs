
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Cell position information <see cref="http://www.mpucoder.com/DVD/pgc.html#pos"/>
    /// </summary>
    public class cell_position_t
    {
        public ushort vob_id_nr;
        public byte zero_1;
        public byte cell_nr;

        private cell_position_t(Stream file)
        {
            // Read data
            vob_id_nr = file.Read<ushort>();
            zero_1 = file.Read<byte>();
            cell_nr = file.Read<byte>();

            // Verify
            DvdUtils.B2N_16(ref vob_id_nr);
            DvdUtils.CHECK_ZERO(zero_1);
        }

        internal static void ifoRead_CELL_POSITION_TBL(Stream file, cell_position_t[] cells, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new cell_position_t(file);
            }
        }
    }
}