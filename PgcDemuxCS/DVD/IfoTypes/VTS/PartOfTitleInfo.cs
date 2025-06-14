
namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// PartOfTitle Unit Information <see cref="http://www.mpucoder.com/DVD/ifo_vts.html#ptt"/> 
    /// </summary>
    public class PartOfTitleInfo
    {
        internal const uint Size = 4;

        public readonly ushort ProgramChainNumber;
        public readonly ushort ProgramNumber;

        internal PartOfTitleInfo(Stream file)
        {
            ProgramChainNumber = file.Read<ushort>();
            ProgramNumber = file.Read<ushort>();

            // ??
            DvdUtils.CHECK_VALUE(ProgramChainNumber != 0);
            DvdUtils.CHECK_VALUE(ProgramChainNumber < 1000); /* ?? */
            DvdUtils.CHECK_VALUE(ProgramNumber != 0);
            DvdUtils.CHECK_VALUE(ProgramNumber < 100); /* ?? */
            //don't abort here. E-One DVDs contain PTT with pgcn or pgn == 0
        }
    }
}