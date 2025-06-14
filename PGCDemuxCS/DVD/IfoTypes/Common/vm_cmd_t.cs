
using PgcDemuxCS.DVD;

namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Type to store per-command data
    /// </summary>
    public class vm_cmd_t : IStreamReadable<vm_cmd_t>
    {
        byte[] bytes = new byte[8];

        private vm_cmd_t(Stream file)
        {
            file.Read(this.bytes);
        }

        static vm_cmd_t? IStreamReadable<vm_cmd_t>.Read(Stream file)
        {
            try
            {
                return new vm_cmd_t(file);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}