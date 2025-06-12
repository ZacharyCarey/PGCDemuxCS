
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// DVD Time information
    /// </summary>
    internal class dvd_time_t
    {
        internal byte hour;
        internal byte minute;
        internal byte second;

        /// <summary>
        /// The two high bits are the frame rate
        /// 11 = 30 fps
        /// 10 = illegal
        /// 01 = 25 fps
        /// 00 = illegal
        /// </summary>
        internal byte frame_u;

        internal dvd_time_t(Stream file)
        {
            hour = file.Read<byte>();
            minute = file.Read<byte>();
            second = file.Read<byte>();
            frame_u = file.Read<byte>();
        }
    }
}