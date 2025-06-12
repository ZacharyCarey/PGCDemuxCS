
namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Text data manager information (Incomplete)
    /// </summary>
    internal class txtdt_mgi_t
    {
        /// <summary>
        /// 12 chars
        /// </summary>
        internal string disc_name;

        internal ushort unknown1;
        internal ushort nr_of_language_units;
        internal uint last_byte;
        //internal txtdt_lu_t? lu = null;

        private txtdt_mgi_t(Stream file, uint sector)
        {
            file.Seek(sector * DvdUtils.DVD_BLOCK_LEN, SeekOrigin.Begin);

            // Read data
            disc_name = file.ReadString(12);
            unknown1 = file.Read<ushort>();
            nr_of_language_units = file.Read<ushort>();
            last_byte = file.Read<uint>();

            // TODO: What about lu???
        }

        internal static bool ifoRead_TXTDT_MGI(Stream file, uint sector, out txtdt_mgi_t? result)
        {
            try
            {
                result = new txtdt_mgi_t(file, sector);
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