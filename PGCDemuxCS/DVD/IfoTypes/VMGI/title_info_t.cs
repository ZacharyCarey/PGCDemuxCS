

namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Title information <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#tt"/>
    /// </summary>
    internal class title_info_t : IStreamReadable<title_info_t>
    {
        internal const uint Size = 12;

        internal playback_type_t pb_ty; // TODO rename "title type"
        internal byte nr_of_angles;
        internal ushort nr_of_ptts;
        internal ushort parental_id;
        internal byte title_set_nr;
        internal byte vts_ttn;
        internal uint title_set_sector;

        private title_info_t(Stream file)
        {
            // Read data
            pb_ty = new playback_type_t(file);
            nr_of_angles = file.Read<byte>();
            nr_of_ptts = file.Read<ushort>();
            parental_id = file.Read<ushort>();
            title_set_nr = file.Read<byte>();
            vts_ttn = file.Read<byte>();
            title_set_sector = file.Read<uint>();

            DvdUtils.B2N_16(ref nr_of_ptts);
            DvdUtils.B2N_16(ref parental_id);
            DvdUtils.B2N_32(ref title_set_sector);

            DvdUtils.CHECK_VALUE(pb_ty.zero_1 == false);
            DvdUtils.CHECK_VALUE(nr_of_angles != 0);
            DvdUtils.CHECK_VALUE(nr_of_angles < 10);
            // CHECK_VALUE(tt_srpt->title[i].nr_of_ptts != 0); 
            // XXX: this assertion breaks Ghostbusters: 
            DvdUtils.CHECK_VALUE(nr_of_ptts < 1000); // ?? 
            DvdUtils.CHECK_VALUE(title_set_nr != 0);
            DvdUtils.CHECK_VALUE(title_set_nr < 100); // ?? 
            DvdUtils.CHECK_VALUE(vts_ttn != 0);
            DvdUtils.CHECK_VALUE(vts_ttn < 100); // ?? 
            // CHECK_VALUE(tt_srpt->title[i].title_set_sector != 0); 
        }

        public static title_info_t? Read(Stream file)
        {
            try
            {
                return new title_info_t(file);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}