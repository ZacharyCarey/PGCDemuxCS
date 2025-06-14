using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    public readonly struct AudioStreamControl : IStreamReadable<AudioStreamControl>
    {
        public readonly bool Available;

        /// <summary>
        /// Stream Number for MPEG audio, or Substream Number for all others.
        /// </summary>
        public readonly int StreamNumber;

        internal AudioStreamControl(Stream file)
        {
            BitStream bits = new BitStream(file);
            Available = bits.ReadBit();
            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(4));
            StreamNumber = bits.ReadBits<int>(3);

            DvdUtils.CHECK_ZERO(file.Read<byte>());

            if (!Available) DvdUtils.CHECK_ZERO(StreamNumber);
        }

        static AudioStreamControl IStreamReadable<AudioStreamControl>.Read(Stream file)
        {
            return new AudioStreamControl(file);
        }
    }
}
