
namespace PgcDemuxCS.DVD.IfoTypes.VMGI
{
    /// <summary>
    /// Video Title Set Attribute Table <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#atrt"/>
    /// Supposedly just copies of VTS attributes, not sure if it is usefull
    /// </summary>
    public class VtsAttributeTable
    {
        internal const uint Size = 8;

        public ushort NumberOfVTSs;
        //public vts_attributes_t[] vts;

        internal VtsAttributeTable(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            NumberOfVTSs = file.Read<ushort>();
            file.ReadZeros(2);
            uint last_byte = file.Read<uint>();

            // Verify
            DvdUtils.CHECK_VALUE(NumberOfVTSs != 0);
            DvdUtils.CHECK_VALUE(NumberOfVTSs < 100); /* ?? */
            DvdUtils.CHECK_VALUE((uint)NumberOfVTSs * (4 + vts_attributes_t.MIN_SIZE) + VtsAttributeTable.Size < last_byte + 1);

            // Offsets table for each vts_attributes
            uint[] vts_atrt_offsets = new uint[NumberOfVTSs];
            file.Read(vts_atrt_offsets);

            for (int i = 0; i < NumberOfVTSs; i++)
            {
                DvdUtils.CHECK_VALUE(vts_atrt_offsets[i] + vts_attributes_t.MIN_SIZE < last_byte + 1); // Verify
            }

            /*vts = new vts_attributes_t[nr_of_vtss];
            for (int i = 0; i < nr_of_vtss; i++) {
                uint offset = vts_atrt_offsets[i];
                vts[i] = new vts_attributes_t(file, (sector * DvdUtils.DVD_BLOCK_LEN) + offset);

                // This assert can't be in ifoRead_VTS_ATTRIBUTES 
                DvdUtils.CHECK_VALUE(offset + vts[i].last_byte <= last_byte + 1);
                // Is this check correct? 
            }*/
        }

    }
}