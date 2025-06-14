

namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Title information <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#tt"/>
    /// </summary>
    public class ChapterInfo : IStreamReadable<ChapterInfo>
    {
        internal const uint Size = 12;

        public PlaybackType ChapterType;
        public byte NumberOfAngles;
        public ushort NumberOfChapters;
        public ushort ParentalManagementMask;
        public byte TitleSetNumber;
        public byte ChapterNumber;
        public uint TitleSetSector;

        private ChapterInfo(Stream file)
        {
            // Read data
            ChapterType = new PlaybackType(file);
            NumberOfAngles = file.Read<byte>();
            NumberOfChapters = file.Read<ushort>();
            ParentalManagementMask = file.Read<ushort>();
            TitleSetNumber = file.Read<byte>();
            ChapterNumber = file.Read<byte>();
            TitleSetSector = file.Read<uint>();

            DvdUtils.CHECK_VALUE(NumberOfAngles != 0);
            DvdUtils.CHECK_VALUE(NumberOfAngles < 10);
            // CHECK_VALUE(tt_srpt->title[i].nr_of_ptts != 0); 
            // XXX: this assertion breaks Ghostbusters: 
            DvdUtils.CHECK_VALUE(NumberOfChapters < 1000); // ?? 
            DvdUtils.CHECK_VALUE(TitleSetNumber != 0);
            DvdUtils.CHECK_VALUE(TitleSetNumber < 100); // ?? 
            DvdUtils.CHECK_VALUE(ChapterNumber != 0);
            DvdUtils.CHECK_VALUE(ChapterNumber < 100); // ?? 
            // CHECK_VALUE(tt_srpt->title[i].title_set_sector != 0); 
        }

        static ChapterInfo? IStreamReadable<ChapterInfo>.Read(Stream file, int index)
        {
            return new ChapterInfo(file);
        }
    }
}