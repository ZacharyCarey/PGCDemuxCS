
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Menu PGCI Language Unit <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#pgciut"/>
    /// </summary>
    public class MenuProgramChainLanguageUnit : IStreamReadable<MenuProgramChainLanguageUnit>
    {
        internal const uint Size = 8;

        /// <summary>
        /// ISO639 language code
        /// </summary>
        public readonly string LanguageCode;
        public readonly byte LanguageExtension;
        public readonly byte MenuExistenceFlag;
        public ProgamChainInformationTable PgcTable { get; internal set; } // Gets set by pgci_ut_t
        internal readonly uint lang_start_byte;

        private MenuProgramChainLanguageUnit(Stream file)
        {
            // Read data
            LanguageCode = file.ReadString(2);
            LanguageExtension = file.Read<byte>();
            MenuExistenceFlag = file.Read<byte>();
            lang_start_byte = file.Read<uint>();

            /* Maybe this is only defined for v1.1 and later titles? */
            /* If the bits in 'lu[i].exists' are enumerated abcd efgh then:
               VTS_x_yy.IFO        VIDEO_TS.IFO
               a == 0x83 "Root"         0x82 "Title"
               b == 0x84 "Subpicture"
               c == 0x85 "Audio"
               d == 0x86 "Angle"
               e == 0x87 "PTT"
            */
            DvdUtils.CHECK_VALUE((MenuExistenceFlag & 0x07) == 0);
        }

        static MenuProgramChainLanguageUnit? IStreamReadable<MenuProgramChainLanguageUnit>.Read(Stream file)
        {
            return new MenuProgramChainLanguageUnit(file);
        }
    }
}