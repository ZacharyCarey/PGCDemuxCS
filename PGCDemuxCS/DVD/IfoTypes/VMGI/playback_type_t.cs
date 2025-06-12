

namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    internal class playback_type_t
    {
        internal bool zero_1; // 1 bit
        internal bool multi_or_random_pgc_title; // 1 bit. 0 = one sequential pgc title
        internal bool jlc_exists_in_cell_cmd; // 1 bit
        internal bool jlc_exists_in_prepost_cmd; // 1 bit
        internal bool jlc_exists_in_button_cmd; // 1 bit
        internal bool jlc_exists_in_tt_dom; // 1 bit
        internal bool chapter_search_or_play; // 1 bit. UOP 1
        internal bool title_or_time_play; // 1 bit. UOP 0

        internal playback_type_t(Stream file)
        {
            BitStream bits = new BitStream(file);

            zero_1 = bits.ReadBit();
            multi_or_random_pgc_title = bits.ReadBit();
            jlc_exists_in_cell_cmd = bits.ReadBit();
            jlc_exists_in_prepost_cmd = bits.ReadBit();
            jlc_exists_in_button_cmd = bits.ReadBit();
            jlc_exists_in_tt_dom = bits.ReadBit();
            chapter_search_or_play = bits.ReadBit();
            title_or_time_play = bits.ReadBit();
        }
    }
}