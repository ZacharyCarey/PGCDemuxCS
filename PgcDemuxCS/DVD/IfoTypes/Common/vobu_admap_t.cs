
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// VOBU address map
    /// </summary>
    public class vobu_admap_t
    {
        public const uint Size = 4;

        public uint last_byte;
        public uint[] vobu_start_sectors;

        internal vobu_admap_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            last_byte = file.Read<uint>();

            uint info_length = last_byte + 1 - vobu_admap_t.Size;
            /* assert(info_length > 0);
               Magic Knight Rayearth Daybreak is mastered very strange and has
               Titles with a VOBS that has no VOBUs. */
            DvdUtils.CHECK_VALUE(info_length % sizeof(uint) == 0);

            vobu_start_sectors = new uint[info_length / sizeof(uint)];
            file.Read<uint>(vobu_start_sectors);
        }
    }
}