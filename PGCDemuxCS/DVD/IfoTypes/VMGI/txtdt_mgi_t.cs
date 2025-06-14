
namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Text data manager information (Incomplete)
    /// </summary>
    public class txtdt_mgi_t
    {
        /// <summary>
        /// 12 chars
        /// </summary>
        public string disc_name;

        public ushort unknown1;
        public ushort nr_of_language_units;
        public uint last_byte;
        //public txtdt_lu_t? lu = null;

        internal txtdt_mgi_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            disc_name = file.ReadString(12);
            unknown1 = file.Read<ushort>();
            nr_of_language_units = file.Read<ushort>();
            last_byte = file.Read<uint>();

            // TODO: What about lu???
        }
    }
}