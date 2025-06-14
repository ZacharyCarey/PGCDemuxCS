
namespace PgcDemuxCS.DVD.IfoTypes.Common
{
    /// <summary>
    /// Program chain information search pointer <see cref="http://www.mpucoder.com/DVD/ifo_vmg.html#pgciut"/>
    /// </summary>
    public class ProgramChainInfoSearchPointer
    {
        public readonly PgcCategory Category;
        internal readonly uint Offset;
        public pgc_t pgc { get; internal set; } // Gets set by ProgamChainInformationTable

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal ProgramChainInfoSearchPointer(Stream file, bool isVMG)
#pragma warning restore CS8618 
        {
            BitStream bits = new BitStream(file);

            // Read data
            Category = PgcCategory.Parse(file, isVMG);
            Offset = bits.Read<uint>();
        }
    }
}