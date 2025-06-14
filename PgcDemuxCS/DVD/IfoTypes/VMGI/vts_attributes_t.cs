
using PgcDemuxCS.DVD.IfoTypes.Common;

namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Video Title Set Attributes.
    /// </summary>
    internal class vts_attributes_t
    {
        internal const uint MIN_SIZE = 356;

        /*internal uint last_byte;
        internal uint vts_cat;

        internal video_attr_t vtsm_vobs_attr = new();
        internal byte zero_1;

        /// <summary>
        /// should be 0 or 1
        /// </summary>
        internal byte nr_of_vtsm_audio_streams;
        internal audio_attr_t vtsm_audio_attr = new();
        internal audio_attr_t[] zero_2 = DvdUtils.CreateClassArray<audio_attr_t>(7);
        internal byte[] zero_3 = new byte[16];
        internal byte zero_4;

        /// <summary>
        /// should be 0 or 1
        /// </summary>
        internal byte nr_of_vtsm_subp_streams;
        internal subp_attr_t vtsm_subp_attr = new();
        internal subp_attr_t[] zero_5 = DvdUtils.CreateClassArray<subp_attr_t>(27);

        internal byte[] zero_6 = new byte[2];

        internal video_attr_t vtstt_vobs_video_attr = new();
        internal byte zero_7;
        internal byte nr_of_vtstt_audio_streams;
        internal audio_attr_t[] vtstt_audio_attr = DvdUtils.CreateClassArray<audio_attr_t>(8);
        internal byte[] zero_8 = new byte[16];
        internal byte zero_9;
        internal byte nr_of_vtstt_subp_streams;
        internal subp_attr_t[] vtstt_subp_attr = DvdUtils.CreateClassArray<subp_attr_t>(32);*/

        internal vts_attributes_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            // TODO parse data ifoRead_VTS_ATTRIBUTES
            byte[] data = new byte[542];
            file.Read(data);
        }
    }
}