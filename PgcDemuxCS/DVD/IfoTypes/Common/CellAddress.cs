

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Cell address information <see cref="http://www.mpucoder.com/DVD/ifo.html#c_adt"/>
    /// </summary>
    public class CellAddress : IStreamReadable<CellAddress>
    {
        internal const uint Size = 12;

        public readonly ushort VobID;
        public readonly byte CellID;
        public readonly uint StartSector;
        public readonly uint LastSector;

        private CellAddress(Stream file)
        {
            // Read data
            VobID = file.Read<ushort>();
            CellID = file.Read<byte>();
            file.ReadZeros(1);
            StartSector = file.Read<uint>();
            LastSector = file.Read<uint>();

            // Verify
            DvdUtils.CHECK_VALUE(VobID > 0);
            DvdUtils.CHECK_VALUE(CellID > 0);
            DvdUtils.CHECK_VALUE(StartSector < LastSector);
        }

        static CellAddress? IStreamReadable<CellAddress>.Read(Stream file)
        {
            try
            {
                return new CellAddress(file);
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}