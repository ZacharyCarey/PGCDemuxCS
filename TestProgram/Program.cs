using PgcDemuxCS;

namespace TestProgram
{
    internal class Program
    {
        static void Main(string[] args)
        {
            PgcDemux.Run("pgcdemux -vid 6 -title -noaud -nosub -nocellt -nolog -customvob bnvasl C:\\USERS\\ZACK\\VIDEOS\\WILLY_WONKA\\VIDEO_TS\\VTS_01_0.IFO C:\\Users\\Zack\\Downloads\\DemuxDest");
        }
    }
}
