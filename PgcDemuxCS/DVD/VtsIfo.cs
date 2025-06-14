using PgcDemuxCS.DVD.IfoTypes.Common;
using PgcDemuxCS.DVD.IfoTypes.VTS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgcDemuxCS.DVD
{
    public class VtsIfo : IfoBase
    {

        public readonly uint Category;
        public readonly uint TitleVobStartSector;
        public readonly PartOfTitleSearchPointerTable? TitlesAndChapters = null;
        public readonly ProgamChainInformationTable? TitleProgramChainTable = null;
        public override MenuProgramChainLanguageUnitTable? MenuProgramChainTable { get; } = null; 
        public readonly vts_tmapt_t? TimeMap = null; 
        public readonly CellAddressTable? TitleCellAddressTable = null;
        public readonly vobu_admap_t? TitleVobuAddressMap = null; 
        public readonly VideoAttributes TitlesVobVideoAttributes; 
        public readonly audio_attr_t[] TitlesVobAudioAttributes; 
        public readonly subp_attr_t[] TitlesVobSubpictureAttributes; 
        public readonly multichannel_ext_t[] MultichannelExtensions = new multichannel_ext_t[8];

        private VtsIfo(Stream file) : base(file)
        {
            DvdUtils.CHECK_VALUE(ID == "DVDVIDEO-VTS");
            file.Seek(0x22, SeekOrigin.Begin);
            Category = file.Read<uint>();
            file.ReadZeros(5);

            file.Seek(0x3E, SeekOrigin.Begin);
            file.ReadZeros(47);

            file.Seek(0x84, SeekOrigin.Begin);
            file.ReadZeros(4);

            file.Seek(0xC4, SeekOrigin.Begin);
            TitleVobStartSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(TitleVobStartSector == 0 || (TitleVobStartSector > LastIFOSector && TitleVobStartSector < LastBUPSector));

            uint titlesSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(titlesSector <= LastIFOSector);
            if (titlesSector != 0) TitlesAndChapters = new PartOfTitleSearchPointerTable(file, titlesSector * DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xCC, SeekOrigin.Begin);
            uint titlePgcSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(titlePgcSector <= LastIFOSector);
            if (titlePgcSector != 0) TitleProgramChainTable = new ProgamChainInformationTable(file, titlePgcSector * DvdUtils.DVD_BLOCK_LEN, false);

            file.Seek(0xD0, SeekOrigin.Begin);
            uint menuPgcSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(menuPgcSector <= LastIFOSector);
            if (menuPgcSector != 0) MenuProgramChainTable = new MenuProgramChainLanguageUnitTable(file, menuPgcSector * DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xD4, SeekOrigin.Begin);
            uint timeMapSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(timeMapSector <= LastIFOSector);
            if (timeMapSector != 0) TimeMap = new vts_tmapt_t(file, timeMapSector *  DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xE0, SeekOrigin.Begin);
            uint cellAddrSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(cellAddrSector <= LastIFOSector);
            if (cellAddrSector != 0) TitleCellAddressTable = new CellAddressTable(file, cellAddrSector *  DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xE4, SeekOrigin.Begin);
            uint vobuSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(vobuSector <= LastIFOSector);
            if (vobuSector != 0) TitleVobuAddressMap = new vobu_admap_t(file, vobuSector *  DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0x200, SeekOrigin.Begin);
            TitlesVobVideoAttributes = new VideoAttributes(file);

            ushort numAudioStreams = file.Read<ushort>();
            DvdUtils.CHECK_VALUE(numAudioStreams <= 8);
            TitlesVobAudioAttributes = new audio_attr_t[numAudioStreams];
            file.Read<audio_attr_t>(TitlesVobAudioAttributes);
            file.ReadZeros((8 - (int)numAudioStreams) * 8);

            file.ReadZeros(16);

            ushort numSubpStreams = file.Read<ushort>();
            DvdUtils.CHECK_VALUE(numSubpStreams <= 32);
            TitlesVobSubpictureAttributes = new subp_attr_t[numSubpStreams];
            file.Read<subp_attr_t>(TitlesVobSubpictureAttributes);
            file.ReadZeros((32 - (int)numSubpStreams) * 6);

            file.ReadZeros(2);

            file.Read<multichannel_ext_t>(MultichannelExtensions);

            file.ReadZeros(40);
        }

        public static new VtsIfo? Open(IIfoFileReader reader, int title)
        {
            if (title <= 0) return null;

            // Try to load from the ifo file first
            try
            {
                using (Stream file = OpenFile(reader, title, false))
                {
                    return new VtsIfo(file);
                }
            }
            catch (Exception ex)
            {
            }

            // Failed to parse ifo, try BUP (the backup file)
            try
            {
                using (Stream file = OpenFile(reader, title, true))
                {
                    return new VtsIfo(file);
                }
            }
            catch (Exception ex)
            {

            }

            // Failed to parse both ifo and bup
            return null;
        }

        public static new VtsIfo? Open(string DvdRoot, int title)
        {
            return Open(new SimpleIfoReader(DvdRoot), title);
        }

    }
}
