
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Subpicture attributes
    /// </summary>
    public class subp_attr_t : IStreamReadable<subp_attr_t>
    {
        public const uint Size = 6;

        /// <summary>
        /// 0 = run length
        /// 1 = extended
        /// 2 = other
        /// </summary>
        public byte code_mode; // 3 bits
        public byte zero1; // 3 bits

        /// <summary>
        /// 0 = not specified
        /// 1 = language
        /// 2 = other
        /// </summary>
        public byte type; // 2 bits
        public byte zero2;

        /// <summary>
        /// only if type == 1/language
        /// 2 bytes
        /// </summary>
        public string lang_code;

        /// <summary>
        /// only if type == 1/language
        /// </summary>
        public byte lang_extension;

        public byte code_extension;

        internal subp_attr_t(Stream file)
        {
            BitStream bits = new BitStream(file);

            code_mode = bits.ReadBits<byte>(3);
            zero1 = bits.ReadBits<byte>(3);
            type = bits.ReadBits<byte>(2);
            zero2 = bits.ReadBits<byte>(8);
            lang_code = file.ReadString(2);
            lang_extension = bits.ReadBits<byte>(8);
            code_extension = bits.ReadBits<byte>(8);
        }

        static subp_attr_t? IStreamReadable<subp_attr_t>.Read(Stream file, int index)
        {
            return new subp_attr_t(file);
        }
    }
}