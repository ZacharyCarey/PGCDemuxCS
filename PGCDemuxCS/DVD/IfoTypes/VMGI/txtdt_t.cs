
namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Text data (incomplete)
    /// </summary>
    internal class txtdt_t
    {
        /// <summary>
        /// offsets are relative here
        /// </summary>
        uint last_byte;

        /// <summary>
        /// == nr_of_srpts + 1 (first is disc title)
        /// </summary>
        ushort[] offsets = new ushort[100];
    }
}