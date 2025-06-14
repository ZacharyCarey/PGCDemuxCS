
namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Text data manager information (Incomplete)
    /// </summary>
    public class TextDataManagerInfo
    {
        /// <summary>
        /// 12 chars
        /// </summary>
        public string DiscName;
        public ushort NumberOfLanguageUnits;
        //public txtdt_lu_t? lu = null;

        internal TextDataManagerInfo(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            DiscName = file.ReadString(12);
            ushort unknown1 = file.Read<ushort>();
            NumberOfLanguageUnits = file.Read<ushort>();
            uint last_byte = file.Read<uint>();

            // TODO: What about lu???
        }
    }
}