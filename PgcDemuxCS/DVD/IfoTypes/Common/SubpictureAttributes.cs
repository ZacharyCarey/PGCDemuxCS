
using Iso639;
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    public enum SubpictureCodeMode
    {
        RunLength = 0,
        Extended = 1,
        Other = 2
    }

    /// <summary>
    /// Subpicture attributes
    /// </summary>
    public class SubpictureAttributes : IStreamReadable<SubpictureAttributes>
    {
        internal const uint Size = 6;

        public SubpictureCodeMode Mode; // 3 bits

        /// <summary>
        /// only if type == 1/language
        /// 2 bytes
        /// </summary>
        public Language? Language;

        /// <summary>
        /// only if type == 1/language
        /// </summary>
        public byte LanguageExtension;

        public byte CodeExtension;

        internal SubpictureAttributes(Stream file)
        {
            BitStream bits = new BitStream(file);

            Mode = (SubpictureCodeMode)bits.ReadBits<byte>(3);
            DvdUtils.CHECK_ZERO(bits.ReadBits<byte>(3));
            byte type = bits.ReadBits<byte>(2);
            file.ReadZeros(1);
            string lang = file.ReadString(2);
            if (string.IsNullOrWhiteSpace(lang)) this.Language = null;
            else
            {
                try
                {
                    this.Language = Iso639.Language.FromPart1(lang);
                }catch(Exception)
                {
                    this.Language = null;
                }
            }
            LanguageExtension = file.Read<byte>();
            CodeExtension = file.Read<byte>();
        }

        static SubpictureAttributes? IStreamReadable<SubpictureAttributes>.Read(Stream file, int index)
        {
            return new SubpictureAttributes(file);
        }
    }
}