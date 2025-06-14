using PgcDemuxCS.DVD.IfoTypes.Common;
using PgcDemuxCS.DVD.IfoTypes.VMGI;
using PgcDemuxCS.DVD.IfoTypes.VTS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgcDemuxCS.DVD
{
    public abstract class IfoBase
    {

        /// <summary>
        /// The ifo identifier. 12 bytes long, can only be "DVDVIDEO-VMG" or "DVDVIDEO-VTS"
        /// </summary>
        public readonly string ID;

        /// <summary>
        /// The final sector of the BUP file.
        /// Similar to the VOB files, the BUP file is treated as an extension of the main IFO file, not as two separate files.
        /// See <see cref="http://www.mpucoder.com/DVD/ifo.html#c_adt"/> for more info.
        /// </summary>
        internal readonly uint LastBUPSector;

        /// <summary>
        /// The final sector of this IFO file.
        /// </summary>
        internal readonly uint LastIFOSector;

        /// <summary>
        /// Unclear if this is a disc/movie version, or the DVD format version.
        /// </summary>
        public readonly Version Version;

        /// <summary>
        /// The last byte of the VMGI_MAT/VTS_MAT data structure
        /// </summary>
        internal readonly uint LastByteIndex;

        /// <summary>
        /// The sector where the Menu VOB data starts
        /// <seealso cref="DvdUtils.DVD_BLOCK_LEN"/>
        /// </summary>
        public readonly uint MenuVobStartSector;
        public readonly CellAddressTable? MenuCellAddressTable = null;
        public readonly vobu_admap_t? MenuVobuAddressMap = null;
        public readonly VideoAttributes MenuVobVideoAttributes;
        public readonly audio_attr_t? MenuVobAudioAttributes = null;
        public readonly subp_attr_t? MenuSubpictureAttributes = null;

        public abstract MenuProgramChainLanguageUnitTable? MenuProgramChainTable { get; }

        protected IfoBase(Stream file)
        {
            ID = file.ReadString(12);
            LastBUPSector = file.Read<uint>();
            file.ReadZeros(12);
            LastIFOSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(LastBUPSector != 0);
            DvdUtils.CHECK_VALUE(LastIFOSector != 0);
            DvdUtils.CHECK_VALUE(LastIFOSector * 2 <= LastBUPSector);

            // Read version number
            file.ReadZeros(1);
            byte version = file.Read<byte>();
            Version = new Version((version >> 4) & 0xFF, version & 0xFF);

            file.Seek(0x2B, SeekOrigin.Begin);
            file.ReadZeros(19);

            file.Seek(0x68, SeekOrigin.Begin);
            file.ReadZeros(24);

            file.Seek(0x80, SeekOrigin.Begin);
            LastByteIndex = file.Read<uint>();
            DvdUtils.CHECK_VALUE(LastByteIndex >= 341);
            DvdUtils.CHECK_VALUE(LastByteIndex / DvdUtils.DVD_BLOCK_LEN <= LastIFOSector);

            file.Seek(0x88, SeekOrigin.Begin);
            file.ReadZeros(56);

            MenuVobStartSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(MenuVobStartSector == 0 || (MenuVobStartSector > LastIFOSector && MenuVobStartSector < LastBUPSector));

            file.Seek(0xD8, SeekOrigin.Begin);
            uint menuADTSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(menuADTSector <= LastIFOSector);
            if (menuADTSector != 0) MenuCellAddressTable = new CellAddressTable(file, menuADTSector * DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xDC, SeekOrigin.Begin);
            uint vobuAddressMapSector = file.Read<uint>();
            DvdUtils.CHECK_VALUE(vobuAddressMapSector <= LastIFOSector);
            if (vobuAddressMapSector != 0) MenuVobuAddressMap = new vobu_admap_t(file, vobuAddressMapSector * DvdUtils.DVD_BLOCK_LEN);

            file.Seek(0xE8, SeekOrigin.Begin);
            file.ReadZeros(24);
            MenuVobVideoAttributes = new VideoAttributes(file);

            ushort numAudioStreams = file.Read<ushort>();
            DvdUtils.CHECK_VALUE(numAudioStreams <= 1);
            if (numAudioStreams == 0)
            {
                file.ReadZeros(8);
            }
            else
            {
                MenuVobAudioAttributes = new audio_attr_t(file);
            }

            file.ReadZeros(56 + 16);

            ushort numSubpictureStreams = file.Read<ushort>();
            DvdUtils.CHECK_VALUE(numSubpictureStreams <= 1);
            if (numSubpictureStreams == 0)
            {
                file.ReadZeros(6);
            }
            else
            {
                MenuSubpictureAttributes = new subp_attr_t(file);
            }

            file.ReadZeros(164);
        }

        protected static Stream OpenFile(IIfoFileReader reader, int title, bool backup)
        {
            string filename;
            if (title == 0) filename = $"VIDEO_TS.{(backup ? "BUP" : "IFO")}";
            else filename = $"VTS_{title:00}_0.{(backup ? "BUP" : "IFO")}";

            return reader.OpenFile(filename);
        }

        /// <summary>
        /// Title=0 will open "VIDEO_TS.ifo", Title=1 will open "VTS_01_0.ifo", etc
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="Title"></param>
        /// <returns></returns>
        public static IfoBase? Open(IIfoFileReader reader, int title)
        {
            if (title < 0) return null;
            else if (title == 0) return VmgIfo.Open(reader);
            else return VtsIfo.Open(reader, title);
        }

        /// <summary>
        /// Opens an IFO and reads in all the data for the corresponding file.
        /// Accepted ifo file names:
        /// "VIDEO_TS" (VMG ifo)
        /// "VTS_xx_0" (VTS xx ifo)
        /// </summary>
        public static IfoBase? Open(IIfoFileReader reader, string ifoName)
        {
            ifoName = ifoName.ToUpper();
            if (ifoName == "VIDEO_TS")
            {
                return Open(reader, 0);
            } else if (ifoName.Length == 8 && int.TryParse(ifoName[4..6], out int title))
            {
                return Open(reader, title);
            }

            // Failed to parse both ifo and bup
            return null;
        }

        public static IfoBase? Open(string DvdRoot, int title)
        {
            return Open(new SimpleIfoReader(DvdRoot), title);
        }

        /// <summary>
        /// Opens an IFO and reads in all the data for the corresponding file.
        /// Accepted ifo file names:
        /// "VIDEO_TS" (VMG ifo)
        /// "VTS_xx_0" (VTS xx ifo)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IfoBase? Open(string DvdRoot, string ifoName)
        {
            return Open(new SimpleIfoReader(DvdRoot), ifoName);
        }
    }
}
