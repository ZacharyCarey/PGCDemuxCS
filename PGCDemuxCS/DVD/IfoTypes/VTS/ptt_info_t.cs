
namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// PartOfTitle Unit Information <see cref="http://www.mpucoder.com/DVD/ifo_vts.html#ptt"/> 
    /// </summary>
    public class ptt_info_t
    {
        public const uint Size = 4;

        public ushort pgcn;
        public ushort pgn;

        internal ptt_info_t(Stream file)
        {
            pgcn = file.Read<ushort>();
            pgn = file.Read<ushort>();

            // ??
            DvdUtils.CHECK_VALUE(pgcn != 0);
            DvdUtils.CHECK_VALUE(pgcn < 1000); /* ?? */
            DvdUtils.CHECK_VALUE(pgn != 0);
            DvdUtils.CHECK_VALUE(pgn < 100); /* ?? */
            //don't abort here. E-One DVDs contain PTT with pgcn or pgn == 0
        }
    }
}