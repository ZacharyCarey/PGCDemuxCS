
using PgcDemuxCS.DVD;
using PgcDemuxCS.DVD.IfoTypes.Common;

namespace PgcDemuxCS.DVD.IfoTypes.VMGI 
{
    /// <summary>
    /// Video Manager Information Management Table
    /// </summary>
    internal class vmgi_mat_t
    {
        internal string vmg_identifier; // 12 bytes
        internal uint vmg_last_sector;
        internal byte[] zero_1 = new byte[12];
        internal uint vmgi_last_sector;
        internal byte zero_2;
        internal byte specification_version;
        internal uint vmg_category;
        internal ushort vmg_nr_of_volumes;
        internal ushort vmg_this_volume_nr;
        internal byte disc_side;
        internal byte[] zero_3 = new byte[19];

        /// <summary>
        /// Number of VTSs.
        /// </summary>
        internal ushort vmg_nr_of_title_sets;
        internal byte[] provider_identifier = new byte[32];
        internal ulong vmg_pos_code;
        internal byte[] zero_4 = new byte[24];
        internal uint vmgi_last_byte;
        internal uint first_play_pgc;
        internal byte[] zero_5 = new byte[56];

        /// <summary>
        /// sector
        /// </summary>
        internal uint vmgm_vobs;

        /// <summary>
        /// sector
        /// </summary>
        internal uint tt_srpt;

        /// <summary>
        /// sector
        /// </summary>
        internal uint vmgm_pgci_ut;

        /// <summary>
        /// sector
        /// </summary>
        internal uint ptl_mait;

        /// <summary>
        /// sector
        /// </summary>
        internal uint vts_atrt;

        /// <summary>
        /// sector
        /// </summary>
        internal uint txtdt_mgi;

        /// <summary>
        /// sector
        /// </summary>
        internal uint vmgm_c_adt;

        /// <summary>
        /// sector
        /// </summary>
        internal uint vmgm_vobu_admap;
        internal byte[] zero_6 = new byte[32];

        internal video_attr_t vmgm_video_attr;
        internal byte zero_7;
        internal byte nr_of_vmgm_audio_streams; /* should be 0 or 1 */
        internal audio_attr_t vmgm_audio_attr;
        internal audio_attr_t[] zero_8 = new audio_attr_t[7];
        internal byte[] zero_9 = new byte[17];
        internal byte nr_of_vmgm_subp_streams; /* should be 0 or 1 */
        internal subp_attr_t vmgm_subp_attr;
        internal subp_attr_t[] zero_10 = new subp_attr_t[27];  /* XXX: how much 'padding' here? */

        private vmgi_mat_t(Stream file)
        {
            file.Seek(0, SeekOrigin.Begin);

            // Read data
            vmg_identifier = file.ReadString(12);
            vmg_last_sector = file.Read<uint>();
            file.Read(zero_1);
            vmgi_last_sector = file.Read<uint>();
            zero_2 = file.Read<byte>();
            specification_version = file.Read<byte>();
            vmg_category = file.Read<uint>();
            vmg_nr_of_volumes = file.Read<ushort>();
            vmg_this_volume_nr = file.Read<ushort>();
            disc_side = file.Read<byte>();
            file.Read(zero_3);
            vmg_nr_of_title_sets = file.Read<ushort>();
            file.Read(provider_identifier);
            vmg_pos_code = file.Read<ulong>();
            file.Read(zero_4);
            vmgi_last_byte = file.Read<uint>();
            first_play_pgc = file.Read<uint>();
            file.Read(zero_5);
            vmgm_vobs = file.Read<uint>();
            tt_srpt = file.Read<uint>();
            vmgm_pgci_ut = file.Read<uint>();
            ptl_mait = file.Read<uint>();
            vts_atrt = file.Read<uint>();
            txtdt_mgi = file.Read<uint>();
            vmgm_c_adt = file.Read<uint>();
            vmgm_vobu_admap = file.Read<uint>();
            file.Read(zero_6);
            vmgm_video_attr = new video_attr_t(file);
            zero_7 = file.Read<byte>();
            nr_of_vmgm_audio_streams = file.Read<byte>();
            vmgm_audio_attr = new audio_attr_t(file);
            file.Read<audio_attr_t>(zero_8);
            file.Read(zero_9);
            nr_of_vmgm_subp_streams = file.Read<byte>();
            vmgm_subp_attr = new subp_attr_t(file);
            file.Read<subp_attr_t>(zero_10);

            if (vmg_identifier != "DVDVIDEO-VMG") throw new IOException();

            DvdUtils.B2N_32(ref vmg_last_sector);
            DvdUtils.B2N_32(ref vmgi_last_sector);
            DvdUtils.B2N_32(ref vmg_category);
            DvdUtils.B2N_16(ref vmg_nr_of_volumes);
            DvdUtils.B2N_16(ref vmg_this_volume_nr);
            DvdUtils.B2N_16(ref vmg_nr_of_title_sets);
            DvdUtils.B2N_64(ref vmg_pos_code);
            DvdUtils.B2N_32(ref vmgi_last_byte);
            DvdUtils.B2N_32(ref first_play_pgc);
            DvdUtils.B2N_32(ref vmgm_vobs);
            DvdUtils.B2N_32(ref tt_srpt);
            DvdUtils.B2N_32(ref vmgm_pgci_ut);
            DvdUtils.B2N_32(ref ptl_mait);
            DvdUtils.B2N_32(ref vts_atrt);
            DvdUtils.B2N_32(ref txtdt_mgi);
            DvdUtils.B2N_32(ref vmgm_c_adt);
            DvdUtils.B2N_32(ref vmgm_vobu_admap);


            DvdUtils.CHECK_ZERO(zero_1);
            DvdUtils.CHECK_ZERO(zero_2);
            /* DVDs created by VDR-to-DVD device LG RC590M violate the following check with
             * vmgi_mat->zero_3 = 0x00000000010000000000000000000000000000. */
            DvdUtils.CHECK_ZERO(zero_3);
            DvdUtils.CHECK_ZERO(zero_4);
            DvdUtils.CHECK_ZERO(zero_5);
            DvdUtils.CHECK_ZERO(zero_6);
            DvdUtils.CHECK_ZERO(zero_7);
            //DvdUtils.CHECK_ZERO(zero_8);
            DvdUtils.CHECK_ZERO(zero_9);
            //DvdUtils.CHECK_ZERO(zero_10);
            DvdUtils.CHECK_VALUE(vmg_last_sector != 0);
            DvdUtils.CHECK_VALUE(vmgi_last_sector != 0);
            DvdUtils.CHECK_VALUE(vmgi_last_sector * 2 <= vmg_last_sector);
            DvdUtils.CHECK_VALUE(vmgi_last_sector * 2 <= vmg_last_sector);
            DvdUtils.CHECK_VALUE(vmg_nr_of_volumes != 0);
            DvdUtils.CHECK_VALUE(vmg_this_volume_nr != 0);
            DvdUtils.CHECK_VALUE(vmg_this_volume_nr <= vmg_nr_of_volumes);
            DvdUtils.CHECK_VALUE(disc_side == 1 || disc_side == 2);
            DvdUtils.CHECK_VALUE(vmg_nr_of_title_sets != 0);
            DvdUtils.CHECK_VALUE(vmgi_last_byte >= 341);
            DvdUtils.CHECK_VALUE(vmgi_last_byte / DvdUtils.DVD_BLOCK_LEN <= vmgi_last_sector);
            /* It seems that first_play_pgc is optional. */
            DvdUtils.CHECK_VALUE(first_play_pgc < vmgi_last_byte);
            DvdUtils.CHECK_VALUE(vmgm_vobs == 0 ||
                        (vmgm_vobs > vmgi_last_sector &&
                         vmgm_vobs < vmg_last_sector));
            DvdUtils.CHECK_VALUE(tt_srpt <= vmgi_last_sector);
            DvdUtils.CHECK_VALUE(vmgm_pgci_ut <= vmgi_last_sector);
            DvdUtils.CHECK_VALUE(ptl_mait <= vmgi_last_sector);
            DvdUtils.CHECK_VALUE(vts_atrt <= vmgi_last_sector);
            DvdUtils.CHECK_VALUE(txtdt_mgi <= vmgi_last_sector);
            DvdUtils.CHECK_VALUE(vmgm_c_adt <= vmgi_last_sector);
            DvdUtils.CHECK_VALUE(vmgm_vobu_admap <= vmgi_last_sector);

            DvdUtils.CHECK_VALUE(nr_of_vmgm_audio_streams <= 1);
            DvdUtils.CHECK_VALUE(nr_of_vmgm_subp_streams <= 1);
        }

        internal static bool ifoRead_VMG(Stream file, out vmgi_mat_t? result)
        {
            try
            {
                result = new vmgi_mat_t(file);
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