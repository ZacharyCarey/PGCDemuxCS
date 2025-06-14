
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// PGC Command Table <see cref="http://www.mpucoder.com/DVD/pgc.html#cmd"/>
    /// </summary>
    public class PgcCommandTable
    {
        internal const int COMMAND_DATA_SIZE = 8;
        internal const int PGC_COMMAND_TBL_SIZE = 8;

        public VmCommand[] pre_cmds;
        public VmCommand[] post_cmds;
        public VmCommand[] cell_cmds;

        internal PgcCommandTable(Stream file, uint offset)
        {
            file.Seek(offset, SeekOrigin.Begin);

            // Read data
            ushort nr_of_pre = file.Read<ushort>();
            ushort nr_of_post = file.Read<ushort>();
            ushort nr_of_cell = file.Read<ushort>();
            ushort last_byte = file.Read<ushort>();

            DvdUtils.CHECK_VALUE(nr_of_pre + nr_of_post + nr_of_cell <= 255);
            DvdUtils.CHECK_VALUE((nr_of_pre + nr_of_post + nr_of_cell) * COMMAND_DATA_SIZE + PGC_COMMAND_TBL_SIZE <= last_byte + 1);

            // Read additional data
            if (nr_of_pre != 0)
            {
                pre_cmds = new VmCommand[nr_of_pre];
                file.Read<VmCommand>(pre_cmds);
            }
            else pre_cmds = Array.Empty<VmCommand>();

            if (nr_of_post != 0)
            {
                post_cmds = new VmCommand[nr_of_post];
                file.Read<VmCommand>(post_cmds);
            }
            else post_cmds = Array.Empty<VmCommand>();

            if (nr_of_cell != 0)
            {
                cell_cmds = new VmCommand[nr_of_cell];
                file.Read<VmCommand>(cell_cmds);
            }
            else cell_cmds = Array.Empty<VmCommand>();
        }
    }
}