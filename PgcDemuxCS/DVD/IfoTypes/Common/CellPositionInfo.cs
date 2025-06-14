
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Cell position information <see cref="http://www.mpucoder.com/DVD/pgc.html#pos"/>
    /// </summary>
    public class CellPositionInfo
    {
        public readonly ushort VobID;
        public readonly byte CellID;

        private CellPositionInfo(Stream file)
        {
            // Read data
            VobID = file.Read<ushort>();
            DvdUtils.CHECK_ZERO(file.Read<byte>());
            CellID = file.Read<byte>();
        }

        internal static void ifoRead_CELL_POSITION_TBL(Stream file, CellPositionInfo[] cells, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new CellPositionInfo(file);
            }
        }
    }
}