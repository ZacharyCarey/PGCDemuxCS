
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

        private vobu_admap_t(Stream file, uint sector)
        {
            file.Seek(sector * DvdUtils.DVD_BLOCK_LEN, SeekOrigin.Begin);

            // Read data
            last_byte = file.Read<uint>();

            // Fix endiness
            DvdUtils.B2N_32(ref last_byte);

            uint info_length = last_byte + 1 - vobu_admap_t.Size;
            /* assert(info_length > 0);
               Magic Knight Rayearth Daybreak is mastered very strange and has
               Titles with a VOBS that has no VOBUs. */
            DvdUtils.CHECK_VALUE(info_length % sizeof(uint) == 0);

            vobu_start_sectors = new uint[info_length / sizeof(uint)];
            file.Read<uint>(vobu_start_sectors);

            // Fix endiness
            for (int i = 0; i < vobu_start_sectors.Length; i++)
                DvdUtils.B2N_32(ref vobu_start_sectors[i]);
        }

        internal static bool ifoRead_VOBU_ADMAP(Stream file, uint sector, out vobu_admap_t? result)
        {
            try
            {
                result = new vobu_admap_t(file, sector);
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