
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Type to store per-command data
    /// </summary>
    public class VmCommand : IStreamReadable<VmCommand>
    {
        public byte[] Bytes = new byte[8];

        private VmCommand(Stream file)
        {
            file.Read(this.Bytes);
        }

        static VmCommand? IStreamReadable<VmCommand>.Read(Stream file)
        {
            return new VmCommand(file);
        }
    }
}