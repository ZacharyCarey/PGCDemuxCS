
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Audio attributes
    /// </summary>
    public class audio_attr_t : IStreamReadable<audio_attr_t>
    {
        public const uint Size = 8;

        public byte audio_format; // 3 bits
        public bool multichannel_extension; // 1 bit
        public byte lang_type; // 2 bits
        public byte application_mode; // 2 bits

        public byte quantization; // 2 bits
        public byte sample_frequency; // 2 bits
        public bool unknown1; // 1 bit
        public byte channels; // 3 bits

        /// <summary>
        /// 2 bytes
        /// </summary>
        public string lang_code;
        public byte lang_extension;
        public byte code_extension;
        public byte unknown3;

        private byte app_info;
        public KaraokeData karaoke => new KaraokeData(this);
        public SurroundData surround => new SurroundData(this);

        internal audio_attr_t(Stream file)
        {
            BitStream bits = new BitStream(file);

            audio_format = bits.ReadBits<byte>(3);
            multichannel_extension = bits.ReadBit();
            lang_type = bits.ReadBits<byte>(2);
            application_mode = bits.ReadBits<byte>(2);
            quantization = bits.ReadBits<byte>(2);
            sample_frequency = bits.ReadBits<byte>(2);
            unknown1 = bits.ReadBit();
            channels = bits.ReadBits<byte>(3);
            lang_code = file.ReadString(2);
            lang_extension = bits.ReadBits<byte>(8);
            code_extension = bits.ReadBits<byte>(8);
            unknown3 = bits.ReadBits<byte>(8);
            app_info = bits.ReadBits<byte>(8);
        }

        public class KaraokeData
        {
            private audio_attr_t source;
            public bool unknown4 => ((source.app_info >> 7) & 0b1) != 0;
            public byte channel_assignment => (byte)((source.app_info >> 4) & 0b111);
            public byte version => (byte)((source.app_info >> 2) & 0b11);

            /// <summary>
            /// probably 0: true, 1:false
            /// </summary>
            public bool mc_intro => ((source.app_info >> 1) & 0b1) != 0;

            /// <summary>
            /// 0=solo, 1=duet
            /// </summary>
            public bool mode => (source.app_info & 0b1) != 0; // 
            internal KaraokeData(audio_attr_t data)
            {
                this.source = data;
            }
        }
        public class SurroundData
        {
            private audio_attr_t source;
            public byte unknown5 => (byte)((source.app_info >> 4) & 0b1111);
            public bool dolby_encoded => ((source.app_info >> 3) & 0b1) != 0; // Suitable for surround decoding
            public byte unknown6 => (byte)(source.app_info & 0b111);
            internal SurroundData(audio_attr_t data)
            {
                this.source = data;
            }
        }

        public static audio_attr_t? Read(Stream file)
        {
            try
            {
                return new audio_attr_t(file);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}