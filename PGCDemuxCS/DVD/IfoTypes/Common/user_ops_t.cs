
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// User operations
    /// <see cref="http://www.mpucoder.com/DVD/uops.html"/>
    /// </summary>
    public class user_ops_t
    {
        public byte zero; //7 bits. 25-31
        public bool video_pres_mode_change; // 1 bit. 24

        public bool karaoke_audio_pres_mode_change; // 1 bit. 23
        public bool angle_change; // 1 bit
        public bool subpic_stream_change; // 1 bit
        public bool audio_stream_change; // 1 bit
        public bool pause_on; // 1 bit
        public bool still_off; // 1 bit
        public bool button_select_or_activate; // 1 bit
        public bool resume; // 1 bit. 16

        public bool chapter_menu_call; // 1 bit. 15
        public bool angle_menu_call; // 1 bit
        public bool audio_menu_call; // 1 bit
        public bool subpic_menu_call; // 1 bit
        public bool root_menu_call; // 1 bit
        public bool title_menu_call; // 1 bit
        public bool backward_scan; // 1 bit
        public bool forward_scan; // 1 bit. 8

        public bool next_pg_search; // 1 bit. 7
        public bool prev_or_top_pg_search; // 1 bit
        public bool time_or_chapter_search; // 1 bit
        public bool go_up; // 1 bit
        public bool stop; // 1 bit
        public bool title_play; // 1 bit
        public bool chapter_search_or_play; // 1 bit
        public bool title_or_time_play; // 1 bit. 0s

        internal user_ops_t(Stream file)
        {
            BitStream bits = new BitStream(file);

            zero = bits.ReadBits<byte>(7);
            video_pres_mode_change = bits.ReadBit();
            karaoke_audio_pres_mode_change = bits.ReadBit();
            angle_change = bits.ReadBit();
            subpic_stream_change = bits.ReadBit();
            audio_stream_change = bits.ReadBit();
            pause_on = bits.ReadBit();
            still_off = bits.ReadBit();
            button_select_or_activate = bits.ReadBit();
            resume = bits.ReadBit();
            chapter_menu_call = bits.ReadBit();
            angle_menu_call = bits.ReadBit();
            audio_menu_call = bits.ReadBit();
            subpic_menu_call = bits.ReadBit();
            root_menu_call = bits.ReadBit();
            title_menu_call = bits.ReadBit();
            backward_scan = bits.ReadBit();
            forward_scan = bits.ReadBit();
            next_pg_search = bits.ReadBit();
            prev_or_top_pg_search = bits.ReadBit();
            time_or_chapter_search = bits.ReadBit();
            go_up = bits.ReadBit();
            stop = bits.ReadBit();
            title_play = bits.ReadBit();
            chapter_search_or_play = bits.ReadBit();
            title_or_time_play = bits.ReadBit();
        }
    }
}