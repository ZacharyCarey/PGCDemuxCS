
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Subpicture attributes
    /// </summary>
    internal class subp_attr_t : IStreamReadable<subp_attr_t>
    {
        internal const uint Size = 6;

        /// <summary>
        /// 0 = run length
        /// 1 = extended
        /// 2 = other
        /// </summary>
        internal byte code_mode; // 3 bits
        internal byte zero1; // 3 bits

        /// <summary>
        /// 0 = not specified
        /// 1 = language
        /// 2 = other
        /// </summary>
        internal byte type; // 2 bits
        internal byte zero2;

        /// <summary>
        /// only if type == 1/language
        /// </summary>
        internal ushort lang_code;

        /// <summary>
        /// only if type == 1/language
        /// </summary>
        internal byte lang_extension;

        internal byte code_extension;

        internal subp_attr_t(Stream file)
        {
            BitStream bits = new BitStream(file);

            code_mode = bits.ReadBits<byte>(3);
            zero1 = bits.ReadBits<byte>(3);
            type = bits.ReadBits<byte>(2);
            zero2 = bits.ReadBits<byte>(8);
            lang_code = bits.ReadBits<ushort>(16);
            lang_extension = bits.ReadBits<byte>(8);
            code_extension = bits.ReadBits<byte>(8);
        }

        public static subp_attr_t? Read(Stream file)
        {
            try
            {
                return new subp_attr_t(file);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}