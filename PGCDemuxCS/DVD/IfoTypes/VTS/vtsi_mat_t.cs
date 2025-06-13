
using PgcDemuxCS.DVD.IfoTypes.Common;

namespace PgcDemuxCS.DVD.IfoTypes.VTS
{
    /// <summary>
    /// Video Title Set Information Management Table
    /// </summary>
    public class vtsi_mat_t
    {
        /// <summary>
        /// 12 bytes
        /// </summary>
        public string vts_identifier;
        public uint vts_last_sector;
        public byte[] zero_1 = new byte[12];
        public uint vtsi_last_sector;
        public byte zero_2;
        public byte specification_version;
        public uint vts_category;
        public ushort zero_3;
        public ushort zero_4;
        public byte zero_5;
        public byte[] zero_6 = new byte[19];
        public ushort zero_7;
        public byte[] zero_8 = new byte[32];
        public ulong zero_9;
        public byte[] zero_10 = new byte[24];
        public uint vtsi_last_byte;
        public uint zero_11;
        public byte[] zero_12 = new byte[56];

        /// <summary>
        /// Sector
        /// </summary>
        public uint vtsm_vobs;

        /// <summary>
        /// Sector
        /// </summary>
        public uint vtstt_vobs;

        /// <summary>
        /// Sector
        /// </summary>
        public uint vts_ptt_srpt;

        /// <summary>
        /// Sector
        /// </summary>
        public uint vts_pgcit;

        /// <summary>
        /// Sector
        /// </summary>
        public uint vtsm_pgci_ut;

        /// <summary>
        /// Sector
        /// </summary>
        public uint vts_tmapt;

        /// <summary>
        /// Sector
        /// </summary>
        public uint vtsm_c_adt;

        /// <summary>
        /// Sector
        /// </summary>
        public uint vtsm_vobu_admap;

        /// <summary>
        /// Sector
        /// </summary>
        public uint vts_c_adt;

        /// <summary>
        /// Sector
        /// </summary>
        public uint vts_vobu_admap;
        public byte[] zero_13 = new byte[24];

        public video_attr_t vtsm_video_attr;
        public byte zero_14;

        /// <summary>
        /// should be 0 or 1
        /// </summary>
        public byte nr_of_vtsm_audio_streams;
        public audio_attr_t? vtsm_audio_attr = null;

        /// <summary>
        /// should be 0 or 1
        /// </summary>
        public byte nr_of_vtsm_subp_streams;
        public subp_attr_t? vtsm_subp_attr = null;

        public video_attr_t vts_video_attr;
        public byte zero_19;
        public byte nr_of_vts_audio_streams;
        public audio_attr_t[] vts_audio_attr;
        public byte[] zero_20 = new byte[17];
        public byte nr_of_vts_subp_streams;
        public subp_attr_t[] vts_subp_attr;
        public ushort zero_21;
        public multichannel_ext_t[] vts_mu_audio_attr = new multichannel_ext_t[8];
        /* XXX: how much 'padding' here, if any? */

        private vtsi_mat_t(Stream file)
        {
            file.Seek(0, SeekOrigin.Begin);

            // Read data
            vts_identifier = file.ReadString(12);
            vts_last_sector = file.Read<uint>();
            file.Read(zero_1);
            vtsi_last_sector = file.Read<uint>();
            zero_2 = file.Read<byte>();
            specification_version = file.Read<byte>();
            vts_category = file.Read<uint>();
            zero_3 = file.Read<ushort>();
            zero_4 = file.Read<ushort>();
            zero_5 = file.Read<byte>();
            file.Read(zero_6);
            zero_7 = file.Read<ushort>();
            file.Read(zero_8);
            zero_9 = file.Read<ulong>();
            file.Read(zero_10);
            vtsi_last_byte = file.Read<uint>();
            zero_11 = file.Read<uint>();
            file.Read(zero_12);
            vtsm_vobs = file.Read<uint>();
            vtstt_vobs = file.Read<uint>();
            vts_ptt_srpt = file.Read<uint>();
            vts_pgcit = file.Read<uint>();
            vtsm_pgci_ut = file.Read<uint>();
            vts_tmapt = file.Read<uint>();
            vtsm_c_adt = file.Read<uint>();
            vtsm_vobu_admap = file.Read<uint>();
            vts_c_adt = file.Read<uint>();
            vts_vobu_admap = file.Read<uint>();
            file.Read(zero_13);
            vtsm_video_attr = new(file);
            zero_14 = file.Read<byte>();

            nr_of_vtsm_audio_streams = file.Read<byte>();
            if (nr_of_vtsm_audio_streams == 1) vtsm_audio_attr = new audio_attr_t(file);

            file.Seek(0x154, SeekOrigin.Begin);
            nr_of_vtsm_subp_streams = file.Read<byte>();
            if (nr_of_vtsm_subp_streams == 1) vtsm_subp_attr = new subp_attr_t(file);

            file.Seek(0x200, SeekOrigin.Begin);
            vts_video_attr = new video_attr_t(file);
            zero_19 = file.Read<byte>();
            nr_of_vts_audio_streams = file.Read<byte>();
            vts_audio_attr = new audio_attr_t[nr_of_vts_audio_streams];
            file.Read<audio_attr_t>(vts_audio_attr);
            file.Seek(0x0244, SeekOrigin.Begin);
            file.Read(zero_20);
            nr_of_vts_subp_streams = file.Read<byte>();
            vts_subp_attr = new subp_attr_t[nr_of_vts_subp_streams];
            file.Read<subp_attr_t>(vts_subp_attr);
            file.Seek(0x0316, SeekOrigin.Begin);
            zero_21 = file.Read<ushort>();
            file.Read<multichannel_ext_t>(vts_mu_audio_attr);

            // Sanity check
            if (vts_identifier != "DVDVIDEO-VTS") throw new IOException("Failed to find VTS identifier");

            // Fix endiness
            DvdUtils.B2N_32(ref vts_last_sector);
            DvdUtils.B2N_32(ref vtsi_last_sector);
            DvdUtils.B2N_32(ref vts_category);
            DvdUtils.B2N_32(ref vtsi_last_byte);
            DvdUtils.B2N_32(ref vtsm_vobs);
            DvdUtils.B2N_32(ref vtstt_vobs);
            DvdUtils.B2N_32(ref vts_ptt_srpt);
            DvdUtils.B2N_32(ref vts_pgcit);
            DvdUtils.B2N_32(ref vtsm_pgci_ut);
            DvdUtils.B2N_32(ref vts_tmapt);
            DvdUtils.B2N_32(ref vtsm_c_adt);
            DvdUtils.B2N_32(ref vtsm_vobu_admap);
            DvdUtils.B2N_32(ref vts_c_adt);
            DvdUtils.B2N_32(ref vts_vobu_admap);

            // Verify
            DvdUtils.CHECK_ZERO(zero_1);
            DvdUtils.CHECK_ZERO(zero_2);
            DvdUtils.CHECK_ZERO(zero_3);
            DvdUtils.CHECK_ZERO(zero_4);
            DvdUtils.CHECK_ZERO(zero_5);
            DvdUtils.CHECK_ZERO(zero_6);
            DvdUtils.CHECK_ZERO(zero_7);
            DvdUtils.CHECK_ZERO(zero_8);
            DvdUtils.CHECK_ZERO(zero_9);
            DvdUtils.CHECK_ZERO(zero_10);
            DvdUtils.CHECK_ZERO(zero_11);
            DvdUtils.CHECK_ZERO(zero_12);
            DvdUtils.CHECK_ZERO(zero_13);
            DvdUtils.CHECK_ZERO(zero_14);
            DvdUtils.CHECK_ZERO(zero_19);
            DvdUtils.CHECK_ZERO(zero_20);
            DvdUtils.CHECK_ZERO(zero_21);
            DvdUtils.CHECK_VALUE(vtsi_last_sector * 2 <= vts_last_sector);
            DvdUtils.CHECK_VALUE(vtsi_last_byte / DvdUtils.DVD_BLOCK_LEN <= vtsi_last_sector);
            DvdUtils.CHECK_VALUE(vtsm_vobs == 0 || (vtsm_vobs > vtsi_last_sector && vtsm_vobs < vts_last_sector));
            DvdUtils.CHECK_VALUE(vtstt_vobs == 0 || (vtstt_vobs > vtsi_last_sector && vtstt_vobs < vts_last_sector));
            DvdUtils.CHECK_VALUE(vts_ptt_srpt <= vtsi_last_sector);
            DvdUtils.CHECK_VALUE(vts_pgcit <= vtsi_last_sector);
            DvdUtils.CHECK_VALUE(vtsm_pgci_ut <= vtsi_last_sector);
            DvdUtils.CHECK_VALUE(vts_tmapt <= vtsi_last_sector);
            DvdUtils.CHECK_VALUE(vtsm_c_adt <= vtsi_last_sector);
            DvdUtils.CHECK_VALUE(vtsm_vobu_admap <= vtsi_last_sector);
            DvdUtils.CHECK_VALUE(vts_c_adt <= vtsi_last_sector);
            DvdUtils.CHECK_VALUE(vts_vobu_admap <= vtsi_last_sector);

            DvdUtils.CHECK_VALUE(nr_of_vtsm_audio_streams <= 1);
            DvdUtils.CHECK_VALUE(nr_of_vtsm_subp_streams <= 1);

            DvdUtils.CHECK_VALUE(nr_of_vts_audio_streams <= 8);
            /*for (i = nr_of_vts_audio_streams; i < 8; i++)
                DvdUtils.CHECK_ZERO(vts_audio_attr[i]);*/

            DvdUtils.CHECK_VALUE(nr_of_vts_subp_streams <= 32);
            /*for (i = nr_of_vts_subp_streams; i < 32; i++)
                DvdUtils.CHECK_ZERO(vts_subp_attr[i]);*/
        }

        internal static bool ifoRead_VTS(Stream file, out vtsi_mat_t? result)
        {
            try
            {
                result = new vtsi_mat_t(file);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }
    }
}