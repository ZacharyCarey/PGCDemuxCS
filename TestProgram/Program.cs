using PgcDemuxCS;

namespace TestProgram
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string dvdRoot = "C:\\USERS\\ZACK\\VIDEOS\\WILLY_WONKA";
            string cmd = "pgcdemux -vid 6 -title -noaud -nosub -nocellt -nolog -customvob bnvasl VTS_01_0.IFO C:\\Users\\Zack\\Downloads\\DemuxDest";
            PgcDemux.Run(cmd, dvdRoot);
        }


    }
}
