using System.Collections;

namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// PartOfTitle Unit Information <see cref="http://www.mpucoder.com/DVD/ifo_vts.html#ptt"/> 
    /// </summary>
    public class PartOfTitleTable : IEnumerable<PartOfTitleInfo>
    {
        private PartOfTitleInfo[] ptt;

        public int Count => ptt.Length;
        public PartOfTitleInfo this[int index] => ptt[index];

        IEnumerator<PartOfTitleInfo> IEnumerable<PartOfTitleInfo>.GetEnumerator()
        {
            return ((IEnumerable<PartOfTitleInfo>)ptt).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ptt.GetEnumerator();
        }

        internal PartOfTitleTable(Stream file, uint offset, uint numEntries)
        {
            DvdUtils.CHECK_VALUE(numEntries < 1000); // ??
            file.Seek(offset, SeekOrigin.Begin);
            ptt = new PartOfTitleInfo[numEntries];
            for (int j = 0; j < numEntries; j++)
            {
                ptt[j] = new PartOfTitleInfo(file);
            }
        }
    }
}