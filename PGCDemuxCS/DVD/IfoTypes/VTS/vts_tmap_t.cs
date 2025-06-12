
namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// Time map
    /// </summary>
    internal class vts_tmap_t
    {
        /// <summary>
        /// Time unit, in secons
        /// </summary>
        internal byte tmu;
        internal byte zero_1;
        internal ushort nr_of_entries;

        /// <summary>
        /// bit 31 is set if next time entry is for a different cell
        /// </summary>
        internal uint[] map_ent; // TODO: should this be bit field at all or just the uint32_t?

        internal vts_tmap_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            tmu = file.Read<byte>();
            zero_1 = file.Read<byte>();
            nr_of_entries = file.Read<ushort>();

            DvdUtils.B2N_16(ref nr_of_entries);
            DvdUtils.CHECK_ZERO(zero_1);

            if (nr_of_entries == 0)
            {
                map_ent = Array.Empty<uint>();
                return;
            }

            map_ent = new uint[nr_of_entries];
            file.Read(map_ent);

            for (int i = 0; i < nr_of_entries; i++)
            {
                DvdUtils.B2N_32(ref map_ent[i]);
            }
        }
    }
}