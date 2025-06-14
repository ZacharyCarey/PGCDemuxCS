

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Cell address information <see cref="http://www.mpucoder.com/DVD/ifo.html#c_adt"/>
    /// </summary>
    public class cell_adr_t : IStreamReadable<cell_adr_t>
    {
        public const uint Size = 12;

        public ushort vob_id;
        public byte cell_id;
        public byte zero_1;
        public uint start_sector;
        public uint last_sector;

        private cell_adr_t(Stream file)
        {
            // Read data
            vob_id = file.Read<ushort>();
            cell_id = file.Read<byte>();
            zero_1 = file.Read<byte>();
            start_sector = file.Read<uint>();
            last_sector = file.Read<uint>();

            // Verify
            DvdUtils.CHECK_ZERO(zero_1);
            DvdUtils.CHECK_VALUE(vob_id > 0);
            DvdUtils.CHECK_VALUE(cell_id > 0);
            DvdUtils.CHECK_VALUE(start_sector < last_sector);
        }

        public static cell_adr_t? Read(Stream file)
        {
            try
            {
                return new cell_adr_t(file);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}