
namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// PartOfTitle Unit Information <see cref="http://www.mpucoder.com/DVD/ifo_vts.html#ptt"/> 
    /// </summary>
    internal class vts_ptt_t
    {
        internal ptt_info_t[] ptt;

        internal vts_ptt_t(Stream file, uint offset, uint numEntries)
        {
            DvdUtils.CHECK_VALUE(numEntries < 1000); // ??
            file.Seek(offset, SeekOrigin.Begin);
            ptt = new ptt_info_t[numEntries];
            for (int j = 0; j < numEntries; j++)
            {
                ptt[j] = new ptt_info_t(file);
            }
        }
    }
}