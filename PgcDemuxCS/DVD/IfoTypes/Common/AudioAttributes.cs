
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    public enum AudioEncoding : byte
    {
        AC3 = 0,
        Unknown = 1,
        Mpeg1 = 2,
        Mpeg2 = 3,
        LPCM = 4,
        SDDS = 5,
        DTS = 6,
        Invalid = 7
    }

    public enum AudioMode : byte
    {
        Unspecified = 0,
        Karaoke = 1,
        Surround = 2
    }

    public enum QuantizationType : byte
    {
        _16bps = 0,
        _20bps = 1,
        _24bps = 2,
        DRC = 3
    }

    public enum AudioContentType : byte
    {
        None = 0,
        Normal = 1,
        VisuallyImpaired = 2,
        DirectorsCommentary = 3,
        AlternateCommentary = 4
    }

    /// <summary>
    /// Audio attributes
    /// </summary>
    public class AudioAttributes : IStreamReadable<AudioAttributes>
    {
        internal const uint Size = 8;

        public readonly AudioEncoding Format; // 3 bits
        public readonly bool MultichannelExtensionPresent; // 1 bit
        public readonly AudioMode Mode; // 2 bits

        public readonly QuantizationType Quantization; // 2 bits
        public readonly int SampleFrequency; // 2 bits
        //public bool unknown1; // 1 bit
        public readonly byte Channels; // 3 bits

        /// <summary>
        /// 2 bytes
        /// </summary>
        public readonly string LanguageCode;
        public readonly byte LanguageExtension;
        public readonly AudioContentType ContentType;
        //public byte unknown3;

        internal byte app_info;
        public KaraokeData Karaoke => new KaraokeData(this);
        public SurroundData Surround => new SurroundData(this);

        private static uint[] StreamIDBase = { 0x80, 0, 0xC0, 0xC0, 0xA0, 0, 0x88 };
        public readonly uint StreamID;

        internal AudioAttributes(Stream file, int index)
        {
            BitStream bits = new BitStream(file);

            Format = (AudioEncoding)bits.ReadBits<byte>(3);
            DvdUtils.CHECK_VALUE((byte)Format < 7);

            MultichannelExtensionPresent = bits.ReadBit();
            byte lang_type = bits.ReadBits<byte>(2);
            Mode = (AudioMode)bits.ReadBits<byte>(2);
            Quantization = (QuantizationType)bits.ReadBits<byte>(2);
            SampleFrequency = GetFrequency(bits.ReadBits<byte>(2));
            bool unknown1 = bits.ReadBit();
            Channels = (byte)(bits.ReadBits<byte>(3) + 1);
            LanguageCode = file.ReadString(2);
            LanguageExtension = bits.ReadBits<byte>(8);
            ContentType = (AudioContentType)bits.ReadBits<byte>(8);
            DvdUtils.CHECK_VALUE((byte)ContentType <= 4);

            byte unknown3 = bits.ReadBits<byte>(8);
            app_info = bits.ReadBits<byte>(8);

            StreamID = StreamIDBase[(byte)Format] + (uint)index;
        }

        private int GetFrequency(byte val)
        {
            switch(val)
            {
                case 0: return 48000;
                case 1: return 96000;
                default: throw new Exception("Invalid sample frequency.");
            }
        }

        public class KaraokeData
        {
            private AudioAttributes source;
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
            internal KaraokeData(AudioAttributes data)
            {
                this.source = data;
            }
        }
        public class SurroundData
        {
            private AudioAttributes source;
            public byte unknown5 => (byte)((source.app_info >> 4) & 0b1111);
            public bool dolby_encoded => ((source.app_info >> 3) & 0b1) != 0; // Suitable for surround decoding
            public byte unknown6 => (byte)(source.app_info & 0b111);
            internal SurroundData(AudioAttributes data)
            {
                this.source = data;
            }
        }

        static AudioAttributes? IStreamReadable<AudioAttributes>.Read(Stream file, int index)
        {
            return new AudioAttributes(file, index);
        }
    }
}