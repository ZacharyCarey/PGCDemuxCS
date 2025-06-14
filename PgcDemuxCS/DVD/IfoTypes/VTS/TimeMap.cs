
namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// Time map
    /// </summary>
    public class TimeMap
    {
        /// <summary>
        /// Time unit, in seconds
        /// </summary>
        public byte TimeUnit;

        /// <summary>
        /// bit 31 is set if next time entry is for a different cell
        /// 
        /// sector offset within VOBS of vobu which begins on or before the time for this entry and ends after the time for this entry.
        /// bit 31 is set if the next time entry is for a different cell
        /// </summary>
        public uint[] SectorOffsets; // TODO: should this be bit field at all or just the uint32_t?

        internal TimeMap(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            TimeUnit = file.Read<byte>();
            file.ReadZeros(1);
            ushort nr_of_entries = file.Read<ushort>();

            if (nr_of_entries == 0)
            {
                SectorOffsets = Array.Empty<uint>();
                return;
            }

            SectorOffsets = new uint[nr_of_entries];
            file.Read(SectorOffsets);
        }
    }
}