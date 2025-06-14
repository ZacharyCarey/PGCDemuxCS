using System.Collections;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// VOBU address map.
    /// Contains the start sectors for VOBUs
    /// </summary>
    public class VobuAddressMap : IEnumerable<uint>
    {
        internal const uint Size = 4;

        private uint[] vobu_start_sectors;
        public int Count => vobu_start_sectors.Length;
        public uint this[int index] => vobu_start_sectors[index];

        IEnumerator<uint> IEnumerable<uint>.GetEnumerator()
        {
            return ((IEnumerable<uint>)vobu_start_sectors).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return vobu_start_sectors.GetEnumerator();
        }

        internal VobuAddressMap(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            uint last_byte = file.Read<uint>();

            uint info_length = last_byte + 1 - VobuAddressMap.Size;
            /* assert(info_length > 0);
               Magic Knight Rayearth Daybreak is mastered very strange and has
               Titles with a VOBS that has no VOBUs. */
            DvdUtils.CHECK_VALUE(info_length % sizeof(uint) == 0);

            vobu_start_sectors = new uint[info_length / sizeof(uint)];
            file.Read<uint>(vobu_start_sectors);
        }
    }
}