
namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// Time Map Table <see cref="http://www.mpucoder.com/DVD/ifo_vts.html#tmap"/>
    /// </summary>
    public class vts_tmapt_t
    {
        public ushort nr_of_tmaps;
        public ushort zero_1;
        public uint last_byte;
        public vts_tmap_t[] tmap;

        /// <summary>
        /// Offset table for each tmap
        /// </summary>
        uint[] tmap_offset;

        internal vts_tmapt_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            nr_of_tmaps = file.Read<ushort>();
            zero_1 = file.Read<ushort>();
            last_byte = file.Read<uint>();

            DvdUtils.CHECK_ZERO(zero_1);

            tmap_offset = new uint[nr_of_tmaps];
            file.Read(tmap_offset);

            tmap = new vts_tmap_t[nr_of_tmaps];
            for (int i = 0; i < nr_of_tmaps; i++)
            {
                tmap[i] = new vts_tmap_t(file, offset + tmap_offset[i]);
            }
        }
    }
}