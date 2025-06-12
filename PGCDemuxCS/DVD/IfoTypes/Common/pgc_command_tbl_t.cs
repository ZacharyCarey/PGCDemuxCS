
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// PGC Command Table <see cref="http://www.mpucoder.com/DVD/pgc.html#cmd"/>
    /// </summary>
    internal class pgc_command_tbl_t
    {
        internal const int COMMAND_DATA_SIZE = 8;
        internal const int PGC_COMMAND_TBL_SIZE = 8;

        internal ushort nr_of_pre;
        internal ushort nr_of_post;
        internal ushort nr_of_cell;
        internal ushort last_byte;
        internal vm_cmd_t[] pre_cmds = Array.Empty<vm_cmd_t>();
        internal vm_cmd_t[] post_cmds = Array.Empty<vm_cmd_t>();
        internal vm_cmd_t[] cell_cmds = Array.Empty<vm_cmd_t>();

        internal pgc_command_tbl_t(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            nr_of_pre = file.Read<ushort>();
            nr_of_post = file.Read<ushort>();
            nr_of_cell = file.Read<ushort>();
            last_byte = file.Read<ushort>();

            // Fix endiness
            DvdUtils.B2N_16(ref nr_of_pre);
            DvdUtils.B2N_16(ref nr_of_post);
            DvdUtils.B2N_16(ref nr_of_cell);
            DvdUtils.B2N_16(ref last_byte);

            DvdUtils.CHECK_VALUE(nr_of_pre + nr_of_post + nr_of_cell <= 255);
            DvdUtils.CHECK_VALUE((nr_of_pre + nr_of_post + nr_of_cell) * COMMAND_DATA_SIZE + PGC_COMMAND_TBL_SIZE <= last_byte + 1);

            // Read additional data
            if (nr_of_pre != 0)
            {
                pre_cmds = new vm_cmd_t[nr_of_pre];
                file.Read<vm_cmd_t>(pre_cmds);
            }

            if (nr_of_post != 0) {
                post_cmds = new vm_cmd_t[nr_of_post];
                file.Read<vm_cmd_t>(post_cmds);
            }

            if (nr_of_cell != 0) {
                cell_cmds = new vm_cmd_t[nr_of_cell];
                file.Read<vm_cmd_t>(cell_cmds);
            }
        }
    }
}