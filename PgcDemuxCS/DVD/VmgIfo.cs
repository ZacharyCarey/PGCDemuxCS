using PgcDemuxCS.DVD.IfoTypes.Common;
using PgcDemuxCS.DVD.IfoTypes.VMGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgcDemuxCS.DVD
{
    public class VmgIfo : IfoBase
    {

        /// <summary>
        /// Byte 2 of 'VMG Category' (00xx0000) is the Region Code
        /// </summary>
        public readonly uint Category;

        public readonly ushort NumberOfVolumes; 
        public readonly ushort VolumeNumber;

        /// <summary>
        /// Which side of the disc is being read. Can only be 1 or 2.
        /// </summary>
        public readonly byte DiscSide;
        public readonly ushort NumberOfTitleSets;

        /// <summary>
        /// Usually the studio that distributed the movie.
        /// For example, "WARNER_HOME_VIDEO"
        /// Maximum 32 bytes.
        /// </summary>
        public readonly string ProviderID; 

        /// <summary>
        /// Unsure what this is used for.
        /// </summary>
        public readonly ulong VmgPosCode;

        public readonly PGC? FirstPlayPGC = null;
        public readonly ChapterSearchPointerTable? Chapters = null; 
        public override MenuProgramChainLanguageUnitTable? MenuProgramChainTable { get; } = null;
        public readonly ParentalManagementInfoTable? ParentalManagementMasks = null;
        public readonly VtsAttributeTable? VtsStreamAttributes = null;
        public readonly TextDataManagerInfo? TextData = null;

        private VmgIfo(Stream file) : base(file)
        {
            DvdUtils.CHECK_VALUE(ID == "DVDVIDEO-VMG");
            file.Seek(0x22, SeekOrigin.Begin);
            Category = file.Read<uint>();
            NumberOfVolumes = file.Read<ushort>();
            DvdUtils.CHECK_VALUE(NumberOfVolumes != 0);
            VolumeNumber = file.Read<ushort>();
            DvdUtils.CHECK_VALUE(VolumeNumber != 0);
            DvdUtils.CHECK_VALUE(VolumeNumber <= NumberOfVolumes);
            DiscSide = file.Read<byte>();
            DvdUtils.CHECK_VALUE(DiscSide == 1 || DiscSide == 2);

            file.Seek(0x3E, SeekOrigin.Begin);
            NumberOfTitleSets = file.Read<ushort>();
            DvdUtils.CHECK_VALUE(NumberOfTitleSets != 0);
            ProviderID = file.ReadString(32);
            VmgPosCode = file.Read<ulong>();

            file.Seek(0x84, SeekOrigin.Begin);
            uint firstPlayAddr = file.Read<uint>(); // Absolute, not sector
            DvdUtils.CHECK_VALUE(firstPlayAddr < LastByteIndex);
            if (firstPlayAddr != 0) FirstPlayPGC = new PGC(file, firstPlayAddr);

            file.Seek(0xC4, SeekOrigin.Begin);
            uint tableOfTitlesSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(tableOfTitlesSector <= LastIFOSector);
            if (tableOfTitlesSector != 0) Chapters = new ChapterSearchPointerTable(file, tableOfTitlesSector * DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xC8, SeekOrigin.Begin);
            uint menuPGCSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(menuPGCSector <= LastIFOSector);
            if (menuPGCSector != 0) MenuProgramChainTable = new MenuProgramChainLanguageUnitTable(file, menuPGCSector * DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xCC, SeekOrigin.Begin);
            uint ptlSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(ptlSector <= LastIFOSector);
            if (ptlSector != 0) ParentalManagementMasks = new ParentalManagementInfoTable(file, ptlSector * DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xD0, SeekOrigin.Begin);
            uint vtsAttrSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(vtsAttrSector <= LastIFOSector);
            if (vtsAttrSector != 0) VtsStreamAttributes = new VtsAttributeTable(file, vtsAttrSector * DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xD4, SeekOrigin.Begin);
            uint textDataSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(textDataSector <= LastIFOSector);
            if (textDataSector != 0) TextData = new TextDataManagerInfo(file, textDataSector * DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xE0, SeekOrigin.Begin);
            file.ReadZeros(8);

            file.Seek(0x200, SeekOrigin.Begin);
            file.ReadZeros(512);
        }

        public static VmgIfo? Open(IIfoFileReader reader)
        {
            // Try to load from the ifo file first
            try
            {
                using (Stream file = OpenFile(reader, 0, false))
                {
                    return new VmgIfo(file);
                }
            }
            catch (Exception ex)
            {
            }

            // Failed to parse ifo, try BUP (the backup file)
            try
            {
                using (Stream file = OpenFile(reader, 0, true))
                {
                    return new VmgIfo(file);
                }
            }
            catch (Exception ex)
            {

            }

            // Failed to parse both ifo and bup
            return null;
        }

        public static VmgIfo? Open(string DvdRoot)
        {
            return Open(new SimpleIfoReader(DvdRoot));
        }
    }
}
