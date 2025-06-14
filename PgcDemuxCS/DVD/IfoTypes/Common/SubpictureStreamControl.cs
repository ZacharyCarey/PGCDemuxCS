using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    public readonly struct SubpictureStreamControl : IStreamReadable<SubpictureStreamControl>
    {
        public readonly bool Available;

        /// <summary>
        /// Standard = 4:3 ratio
        /// </summary>
        public readonly int StreamNumberForStandard;
        public readonly int StreamNumberForWide;
        public readonly int StreamNumberForLetterbox;
        public readonly int StreamNumberForPanAndScan;

        internal SubpictureStreamControl(Stream file)
        {
            BitStream bits = new BitStream(file);

            Available = bits.ReadBit();
            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(2));
            StreamNumberForStandard = bits.ReadBits<int>(5);

            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(3));
            StreamNumberForWide = bits.ReadBits<int>(5);

            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(3));
            StreamNumberForLetterbox = bits.ReadBits<int>(5);

            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(3));
            StreamNumberForPanAndScan = bits.ReadBits<int>(5);

            if (!Available)
            {
                DvdUtils.CHECK_ZERO(StreamNumberForStandard);
                DvdUtils.CHECK_ZERO(StreamNumberForWide);
                DvdUtils.CHECK_ZERO(StreamNumberForLetterbox);
                DvdUtils.CHECK_ZERO(StreamNumberForPanAndScan);
            }
        }

        static SubpictureStreamControl IStreamReadable<SubpictureStreamControl>.Read(Stream file, int index)
        {
            return new SubpictureStreamControl(file);
        }
    }
}
