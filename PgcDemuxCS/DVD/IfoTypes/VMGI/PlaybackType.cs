

namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    public class PlaybackType
    {
        public readonly bool IsSequential; // 1 bit. 0 = one sequential pgc title
        public readonly bool CommandsValidInCell; // 1 bit
        public readonly bool CommandsValidInPrePost; // 1 bit
        public readonly bool CommandsValidInButton; // 1 bit
        public readonly bool jlc_exists_in_tt_dom; // 1 bit
        public readonly bool ChapterSearchOrPlay; // 1 bit. UOP 1
        public readonly bool ChapterOrTimePlay; // 1 bit. UOP 0

        internal PlaybackType(Stream file)
        {
            BitStream bits = new BitStream(file);

            DvdUtils.CHECK_ZERO(bits.ReadBit());
            IsSequential = bits.ReadBit();
            CommandsValidInCell = bits.ReadBit();
            CommandsValidInPrePost = bits.ReadBit();
            CommandsValidInButton = bits.ReadBit();
            jlc_exists_in_tt_dom = bits.ReadBit();
            ChapterSearchOrPlay = bits.ReadBit();
            ChapterOrTimePlay = bits.ReadBit();
        }
    }
}