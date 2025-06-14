using System.Collections;

namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// Time Map Table <see cref="http://www.mpucoder.com/DVD/ifo_vts.html#tmap"/>
    /// </summary>
    public class TimeMapTable : IEnumerable<TimeMap>
    {
        private TimeMap[] tmap;
        public int Count => tmap.Length;
        public TimeMap this[int index] => tmap[index];

        IEnumerator<TimeMap> IEnumerable<TimeMap>.GetEnumerator()
        {
            return ((IEnumerable<TimeMap>)tmap).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return tmap.GetEnumerator();
        }

        internal TimeMapTable(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            ushort nr_of_tmaps = file.Read<ushort>();
            file.ReadZeros(2);
            uint last_byte = file.Read<uint>();

            uint[] tmap_offset = new uint[nr_of_tmaps];
            file.Read(tmap_offset);

            tmap = new TimeMap[nr_of_tmaps];
            for (int i = 0; i < nr_of_tmaps; i++)
            {
                tmap[i] = new TimeMap(file, offset + tmap_offset[i]);
            }
        }
    }
}