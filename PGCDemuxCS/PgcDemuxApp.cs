using PGCDemuxCS;
using System.Numerics;

namespace PgcDemuxCS
{
    internal class PgcDemuxApp
    {
        public const string PGCDEMUX_VERSION = "1.2.0.5";
        internal const int MODUPDATE = 100;
        internal const int MAXLOOKFORAUDIO = 10000;
        internal IIfoFileReader FileReader;

        private static Ref<byte> pcmheader = new ByteArrayRef(new byte[]{
        0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
        0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x80, 0xBB, 0x00, 0x00, 0x70, 0x17, 0x00, 0x00,
        0x04, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61, 0x00, 0x00, 0x00, 0x00
    }, 0);

        private T hi_nib<T>(T a) where T : IShiftOperators<T, int, T>, IBitwiseOperators<T, T, T>, INumber<T>
        {
            return (a >> 4) & T.CreateChecked(0x0F);
        }

        private T lo_nib<T>(T a) where T : IBitwiseOperators<T, T, T>, INumber<T>
        {
            return a & T.CreateChecked(0x0F);
        }

        internal static PgcDemuxApp theApp = new();

        private PgcDemuxApp()
        {

        }

        public static bool Run(PgcDemuxOptions command, IIfoFileReader reader)
        {
            theApp.FileReader = reader;
            return theApp.InitInstance(command);
        }

       
        public Ref<byte> m_buffer = new ByteArrayRef(new byte[2050], 0);

        public bool m_bInProcess;
        public int m_iRet, nResponse;

        public long m_i64OutputLBA;
        public int m_nVobout, m_nVidout, m_nCidout;
        public int m_nCurrVid;      // Used to write in different VOB files, one per VID
        public int m_nVidPacks, m_nAudPacks, m_nSubPacks, m_nNavPacks, m_nPadPacks, m_nUnkPacks;
        public int m_nTotalFrames;
        public bool bNewCell;
        public int m_nLastVid, m_nLastCid;

        public readonly CFILE[] fsub = new CFILE[32];
        public readonly CFILE[] faud = new CFILE[8];
        public CFILE fvid = null;
        public CFILE fvob = null;
        public readonly AudioFormat[] m_audfmt = new AudioFormat[8];
        public readonly string[] m_csAudname = new string[8];
        public readonly int[] m_iFirstSubPTS = new int[32];
        public readonly int[] m_iFirstAudPTS = new int[8];
        public int m_iFirstVidPTS = 0;
        public int m_iFirstNavPTS0 = 0;
        public readonly int[] m_iAudIndex = new int[8];
        public int m_iSubPTS, m_iAudPTS, m_iVidPTS, m_iNavPTS0_old, m_iNavPTS0, m_iNavPTS1_old, m_iNavPTS1;
        public int m_iOffsetPTS;

        // Only in PCM
        public readonly int[] nbitspersample = new int[8];
        public readonly int[] nchannels = new int[8];
        public readonly int[] fsample = new int[8];

        private PgcDemuxOptions Options;
        private IfoInfo FileInfo;

        public virtual bool InitInstance(PgcDemuxOptions command)
        {
            this.Options = command;
            this.Options.VerifyInputs();

            int i, k;
            int nSelVid, nSelCid;

            this.FileInfo = null;

            //	SetRegistryKey( "jsoto's tools" );
            //	WriteProfileInt("MySection", "My Key",123);

            for (i = 0; i < 32; i++) fsub[i] = null;
            for (i = 0; i < 8; i++)
                faud[i] = null;
            fvob = fvid = null;

            m_bInProcess = false;

            // CLI mode
            m_bInProcess = true;
            this.FileInfo = new IfoInfo(FileReader, this.Options); // Read IFO data
            if (m_iRet == 0)
            {
                if (Options.m_iMode == ModeType.PGC)
                {
                    // Check if PGC exists in done in PgcDemux
                    if (Options.m_iDomain == DomainType.Titles)
                        m_iRet = PgcDemux(Options.m_nSelPGC, Options.m_nSelAng, null);
                    else
                        m_iRet = PgcMDemux(Options.m_nSelPGC, null);
                }
                if (Options.m_iMode == ModeType.VID)
                {
                    // Look for nSelVid
                    nSelVid = -1;
                    if (Options.m_iDomain == DomainType.Titles)
                    {
                        for (k = 0; k < FileInfo.m_AADT_Vid_list.GetSize() && nSelVid == -1; k++)
                            if (FileInfo.m_AADT_Vid_list[k].VID == Options.m_nVid)
                                nSelVid = k;
                    }
                    else
                    {
                        for (k = 0; k < FileInfo.m_MADT_Vid_list.GetSize() && nSelVid == -1; k++)
                            if (FileInfo.m_MADT_Vid_list[k].VID == Options.m_nVid)
                                nSelVid = k;
                    }
                    if (nSelVid == -1)
                    {
                        m_iRet = -1;
                        Util.MyErrorBox("Selected Vid not found!");
                    }
                    if (m_iRet == 0)
                    {
                        if (Options.m_iDomain == DomainType.Titles)
                            m_iRet = VIDDemux(nSelVid, null);
                        else
                            m_iRet = VIDMDemux(nSelVid, null);
                    }
                }
                if (Options.m_iMode == ModeType.CID)
                {
                    // Look for nSelVid
                    nSelCid = -1;
                    if (Options.m_iDomain == DomainType.Titles)
                    {
                        for (k = 0; k < FileInfo.m_AADT_Cell_list.GetSize() && nSelCid == -1; k++)
                            if (FileInfo.m_AADT_Cell_list[k].VID == Options.m_nVid && FileInfo.m_AADT_Cell_list[k].CID == Options.m_nCid)
                                nSelCid = k;
                    }
                    else
                    {
                        for (k = 0; k < FileInfo.m_MADT_Cell_list.GetSize() && nSelCid == -1; k++)
                            if (FileInfo.m_MADT_Cell_list[k].VID == Options.m_nVid && FileInfo.m_MADT_Cell_list[k].CID == Options.m_nCid)
                                nSelCid = k;
                    }
                    if (nSelCid == -1)
                    {
                        m_iRet = -1;
                        Util.MyErrorBox("Selected Vid/Cid not found!");
                    }
                    if (m_iRet == 0)
                    {
                        if (Options.m_iDomain == DomainType.Titles)
                            m_iRet = CIDDemux(nSelCid, null);
                        else
                            m_iRet = CIDMDemux(nSelCid, null);
                    }
                }


            }
            m_bInProcess = false;

            //  return false so that we exit the
            //  application, rather than start the application's message pump.
            return false;
        }

        public virtual void UpdateProgress(object pDlg, int nPerc)
        {
            // TODO update progress
        }
        public virtual int ExitInstance()
        {
            return m_iRet;
        }

        public int PgcDemux(int nPGC, int nAng, object pDlg)
        {
            int nTotalSectors;
            int nSector, nCell;
            int k, iArraysize;
            int CID, VID;
            long i64IniSec, i64EndSec;
            long i64sectors;
            int nVobin = 0;
            string csAux, csAux2;
            CFILE inFile, fout;
            long i64;
            bool bMyCell;
            int iRet;
            uint dwCellDuration;
            int nFrames;
            int nCurrAngle, iCat;

            if (nPGC >= FileInfo.m_nPGCs)
            {
                Util.MyErrorBox("Error: PGC does not exist");
                m_bInProcess = false;
                return -1;
            }

            IniDemuxGlobalVars();
            if (OpenVideoFile()) return -1;
            m_bInProcess = true;

            // Calculate  the total number of sectors
            nTotalSectors = 0;
            iArraysize = FileInfo.m_AADT_Cell_list.GetSize();
            for (nCell = nCurrAngle = 0; nCell < FileInfo.m_nCells[nPGC]; nCell++)
            {
                VID = Util.GetNbytes(2, FileInfo.m_pIFO.AtIndex(FileInfo.m_C_POST[nPGC] + 4 * nCell));
                CID = FileInfo.m_pIFO[FileInfo.m_C_POST[nPGC] + 3 + 4 * nCell];

                iCat = FileInfo.m_pIFO[FileInfo.m_C_PBKT[nPGC] + 24 * nCell];
                iCat = iCat & 0xF0;
                //		0101=First; 1001=Middle ;	1101=Last
                if (iCat == 0x50)
                    nCurrAngle = 1;
                else if ((iCat == 0x90 || iCat == 0xD0) && nCurrAngle != 0)
                    nCurrAngle++;
                if (iCat == 0 || (nAng + 1) == nCurrAngle)
                {
                    for (k = 0; k < iArraysize; k++)
                    {
                        if (CID == FileInfo.m_AADT_Cell_list[k].CID &&
                            VID == FileInfo.m_AADT_Cell_list[k].VID)
                        {
                            nTotalSectors += FileInfo.m_AADT_Cell_list[k].iSize;
                        }
                    }
                }
                if (iCat == 0xD0) nCurrAngle = 0;
            }

            nSector = 0;
            iRet = 0;
            for (nCell = nCurrAngle = 0; nCell < FileInfo.m_nCells[nPGC] && m_bInProcess == true; nCell++)
            {
                iCat = FileInfo.m_pIFO[FileInfo.m_C_PBKT[nPGC] + 24 * nCell];
                iCat = iCat & 0xF0;
                //		0101=First; 1001=Middle ;	1101=Last
                if (iCat == 0x50)
                    nCurrAngle = 1;
                else if ((iCat == 0x90 || iCat == 0xD0) && nCurrAngle != 0)
                    nCurrAngle++;
                if (iCat == 0 || (nAng + 1) == nCurrAngle)
                {

                    VID = Util.GetNbytes(2, FileInfo.m_pIFO.AtIndex(FileInfo.m_C_POST[nPGC] + 4 * nCell));
                    CID = FileInfo.m_pIFO[FileInfo.m_C_POST[nPGC] + 3 + 4 * nCell];

                    i64IniSec = Util.GetNbytes(4, FileInfo.m_pIFO.AtIndex(FileInfo.m_C_PBKT[nPGC] + nCell * 24 + 8));
                    i64EndSec = Util.GetNbytes(4, FileInfo.m_pIFO.AtIndex(FileInfo.m_C_PBKT[nPGC] + nCell * 24 + 0x14));
                    for (k = 1, i64sectors = 0; k < 10; k++)
                    {
                        i64sectors += (FileInfo.m_i64VOBSize[k] / 2048);
                        if (i64IniSec < i64sectors)
                        {
                            i64sectors -= (FileInfo.m_i64VOBSize[k] / 2048);
                            nVobin = k;
                            k = 20;
                        }
                    }
                    // TODO create a function for this
                    csAux2 = Options.m_csInputIFO[..^5];
                    csAux = $"{nVobin}.VOB";
                    csAux = csAux2 + csAux;
                    inFile = CFILE.OpenRead(FileReader, csAux);
                    if (inFile == null)
                    {
                        Util.MyErrorBox("Error opening input VOB: " + csAux);
                        m_bInProcess = false;
                        iRet = -1;
                    }
                    if (m_bInProcess) inFile.fseek((long)((i64IniSec - i64sectors) * 2048), SeekOrigin.Begin);

                    for (i64 = 0, bMyCell = true; i64 < (i64EndSec - i64IniSec + 1) && m_bInProcess == true; i64++)
                    {
                        //readpack
                        if ((i64 % MODUPDATE) == 0) UpdateProgress(pDlg, (int)((100 * nSector) / nTotalSectors));
                        if (Util.readbuffer(m_buffer, inFile) != 2048)
                        {
                            if (inFile != null) inFile.fclose();
                            nVobin++;
                            csAux2 = Options.m_csInputIFO[..^5];
                            csAux = $"{nVobin}.VOB";
                            csAux = csAux2 + csAux;
                            inFile = CFILE.OpenRead(FileReader, csAux);
                            if (Util.readbuffer(m_buffer, inFile) != 2048)
                            {
                                Util.MyErrorBox("Input error: Reached end of VOB too early");
                                m_bInProcess = false;
                                iRet = -1;
                            }
                        }

                        if (m_bInProcess == true)
                        {
                            if (Util.IsSynch(m_buffer) != true)
                            {
                                Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                                m_bInProcess = false;
                                iRet = -1;
                            }
                            if (Util.IsNav(m_buffer))
                            {
                                if (m_buffer[0x420] == (byte)(VID % 256) &&
                                    m_buffer[0x41F] == (byte)(VID / 256) &&
                                    m_buffer[0x422] == (byte)CID)
                                    bMyCell = true;
                                else
                                    bMyCell = false;
                            }

                            if (bMyCell)
                            {
                                nSector++;
                                iRet = ProcessPack(true);
                            }

                        }
                    } // For readpacks
                    if (inFile != null) inFile.fclose();
                    inFile = null;
                }  // if (iCat==0 || (nAng+1) == nCurrAngle)
                if (iCat == 0xD0) nCurrAngle = 0;
            }   // For Cells 

            CloseAndNull();
            nFrames = 0;

            if (Options.m_bCheckCellt && m_bInProcess == true)
            {
                csAux = Options.m_csOutputPath + '\\' + "Celltimes.txt";
                fout = CFILE.fopen(csAux, "w");
                for (nCell = 0, nCurrAngle = 0; nCell < FileInfo.m_nCells[nPGC] && m_bInProcess == true; nCell++)
                {
                    dwCellDuration = (uint)Util.GetNbytes(4, FileInfo.m_pIFO.AtIndex(FileInfo.m_C_PBKT[nPGC] + 24 * nCell + 4));

                    iCat = FileInfo.m_pIFO[FileInfo.m_C_PBKT[nPGC] + 24 * nCell];
                    iCat = iCat & 0xF0;
                    //			0101=First; 1001=Middle ;	1101=Last
                    if (iCat == 0x50)
                        nCurrAngle = 1;
                    else if ((iCat == 0x90 || iCat == 0xD0) && nCurrAngle != 0)
                        nCurrAngle++;
                    if (iCat == 0 || (nAng + 1) == nCurrAngle)
                    {
                        nFrames += Util.DurationInFrames(dwCellDuration);
                        if (nCell != (FileInfo.m_nCells[nPGC] - 1) || Options.m_bCheckEndTime)
                            fout.fprintf($"{nFrames}\n");
                    }

                    if (iCat == 0xD0) nCurrAngle = 0;
                }
                fout.fclose();
            }

            m_nTotalFrames = nFrames;

            if (Options.m_bCheckLog && m_bInProcess == true) OutputLog(nPGC, nAng, DomainType.Titles);

            return iRet;
        }
        public virtual int PgcMDemux(int nPGC, object pDlg)
        {
            int nTotalSectors;
            int nSector, nCell;
            int k, iArraysize;
            int CID, VID;
            long i64IniSec, i64EndSec;
            string csAux, csAux2;
            CFILE inFile;
            CFILE fout;
            long i64;
            bool bMyCell;
            int iRet;
            uint dwCellDuration;
            int nFrames;


            if (nPGC >= FileInfo.m_nMPGCs)
            {
                Util.MyErrorBox("Error: PGC does not exist");
                m_bInProcess = false;
                return -1;
            }

            IniDemuxGlobalVars();
            if (OpenVideoFile()) return -1;
            m_bInProcess = true;

            // Calculate  the total number of sectors
            nTotalSectors = 0;
            iArraysize = FileInfo.m_MADT_Cell_list.GetSize();
            for (nCell = 0; nCell < FileInfo.m_nMCells[nPGC]; nCell++)
            {
                VID = Util.GetNbytes(2, FileInfo.m_pIFO.AtIndex(FileInfo.m_M_C_POST[nPGC] + 4 * nCell));
                CID = FileInfo.m_pIFO[FileInfo.m_M_C_POST[nPGC] + 3 + 4 * nCell];
                for (k = 0; k < iArraysize; k++)
                {
                    if (CID == FileInfo.m_MADT_Cell_list[k].CID &&
                        VID == FileInfo.m_MADT_Cell_list[k].VID)
                    {
                        nTotalSectors += FileInfo.m_MADT_Cell_list[k].iSize;
                    }
                }
            }

            nSector = 0;
            iRet = 0;

            for (nCell = 0; nCell < FileInfo.m_nMCells[nPGC] && m_bInProcess == true; nCell++)
            {
                VID = Util.GetNbytes(2, FileInfo.m_pIFO.AtIndex(FileInfo.m_M_C_POST[nPGC] + 4 * nCell));
                CID = FileInfo.m_pIFO[FileInfo.m_M_C_POST[nPGC] + 3 + 4 * nCell];

                i64IniSec = Util.GetNbytes(4, FileInfo.m_pIFO.AtIndex(FileInfo.m_M_C_PBKT[nPGC] + nCell * 24 + 8));
                i64EndSec = Util.GetNbytes(4, FileInfo.m_pIFO.AtIndex(FileInfo.m_M_C_PBKT[nPGC] + nCell * 24 + 0x14));

                if (FileInfo.m_bVMGM)
                {
                    csAux2 = Options.m_csInputIFO[..^3];
                    csAux = csAux2 + "VOB";
                }
                else
                {
                    csAux2 = Options.m_csInputIFO[..^5];
                    csAux = csAux2 + "0.VOB";
                }
                inFile = CFILE.OpenRead(FileReader, csAux);
                if (inFile == null)
                {
                    Util.MyErrorBox("Error opening input VOB: " + csAux);
                    m_bInProcess = false;
                    iRet = -1;
                }
                if (m_bInProcess) inFile.fseek((long)((i64IniSec) * 2048), SeekOrigin.Begin);

                for (i64 = 0, bMyCell = true; i64 < (i64EndSec - i64IniSec + 1) && m_bInProcess == true; i64++)
                {
                    //readpack
                    if ((i64 % MODUPDATE) == 0) UpdateProgress(pDlg, (int)((100 * nSector) / nTotalSectors));
                    if (Util.readbuffer(m_buffer, inFile) != 2048)
                    {
                        if (inFile != null) inFile.fclose();
                        Util.MyErrorBox("Input error: Reached end of VOB too early");
                        m_bInProcess = false;
                        iRet = -1;
                    }

                    if (m_bInProcess == true)
                    {
                        if (Util.IsSynch(m_buffer) != true)
                        {
                            Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                            m_bInProcess = false;
                            iRet = -1;
                        }
                        if (Util.IsNav(m_buffer))
                        {
                            if (m_buffer[0x420] == (byte)(VID % 256) &&
                                m_buffer[0x41F] == (byte)(VID / 256) &&
                                m_buffer[0x422] == (byte)CID)
                                bMyCell = true;
                            else
                                bMyCell = false;
                        }

                        if (bMyCell)
                        {
                            nSector++;
                            iRet = ProcessPack(true);
                        }
                    }
                } // For readpacks
                if (inFile != null) inFile.fclose();
                inFile = null;
            }   // For Cells 

            CloseAndNull();

            nFrames = 0;

            if (Options.m_bCheckCellt && m_bInProcess == true)
            {
                csAux = Options.m_csOutputPath + '\\' + "Celltimes.txt";
                fout = CFILE.fopen(csAux, "w");
                for (nCell = 0; nCell < FileInfo.m_nMCells[nPGC] && m_bInProcess == true; nCell++)
                {
                    dwCellDuration = (uint)Util.GetNbytes(4, FileInfo.m_pIFO.AtIndex(FileInfo.m_M_C_PBKT[nPGC] + 24 * nCell + 4));
                    nFrames += Util.DurationInFrames(dwCellDuration);
                    if (nCell != (FileInfo.m_nMCells[nPGC] - 1) || Options.m_bCheckEndTime)
                        fout.fprintf($"{nFrames}\n");
                }
                fout.fclose();
            }

            m_nTotalFrames = nFrames;

            if (Options.m_bCheckLog && m_bInProcess == true) OutputLog(nPGC, 1, DomainType.Menus);

            return iRet;
        }
        public virtual int VIDDemux(int nVid, object pDlg)
        {
            int nTotalSectors;
            int nSector, nCell;
            int k, iArraysize;
            int CID, VID, nDemuxedVID;
            long i64IniSec, i64EndSec;
            long i64sectors;
            int nVobin = 0;
            string csAux, csAux2;
            CFILE inFile;
            CFILE fout;
            long i64;
            bool bMyCell;
            int iRet;
            int nFrames;
            int nLastCell;

            if (nVid >= FileInfo.m_AADT_Vid_list.GetSize())
            {
                Util.MyErrorBox("Error: Selected Vid does not exist");
                m_bInProcess = false;
                return -1;
            }

            IniDemuxGlobalVars();
            if (OpenVideoFile()) return -1;
            m_bInProcess = true;

            // Calculate  the total number of sectors
            nTotalSectors = FileInfo.m_AADT_Vid_list[nVid].iSize;
            nSector = 0;
            iRet = 0;
            nDemuxedVID = FileInfo.m_AADT_Vid_list[nVid].VID;

            iArraysize = FileInfo.m_AADT_Cell_list.GetSize();
            for (nCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
            {
                VID = FileInfo.m_AADT_Cell_list[nCell].VID;
                CID = FileInfo.m_AADT_Cell_list[nCell].CID;

                if (VID == nDemuxedVID)
                {
                    i64IniSec = FileInfo.m_AADT_Cell_list[nCell].iIniSec;
                    i64EndSec = FileInfo.m_AADT_Cell_list[nCell].iEndSec;
                    for (k = 1, i64sectors = 0; k < 10; k++)
                    {
                        i64sectors += (FileInfo.m_i64VOBSize[k] / 2048);
                        if (i64IniSec < i64sectors)
                        {
                            i64sectors -= (FileInfo.m_i64VOBSize[k] / 2048);
                            nVobin = k;
                            k = 20;
                        }
                    }
                    csAux2 = Options.m_csInputIFO[..^5];
                    csAux = $"{nVobin}.VOB";
                    csAux = csAux2 + csAux;
                    inFile = CFILE.OpenRead(FileReader, csAux);
                    if (inFile == null)
                    {
                        Util.MyErrorBox("Error opening input VOB: " + csAux);
                        m_bInProcess = false;
                        iRet = -1;
                    }
                    if (m_bInProcess) inFile.fseek((long)((i64IniSec - i64sectors) * 2048), SeekOrigin.Begin);

                    for (i64 = 0, bMyCell = true; i64 < (i64EndSec - i64IniSec + 1) && m_bInProcess == true; i64++)
                    {
                        //readpack
                        if ((i64 % MODUPDATE) == 0) UpdateProgress(pDlg, (int)((100 * nSector) / nTotalSectors));
                        if (Util.readbuffer(m_buffer, inFile) != 2048)
                        {
                            if (inFile != null) inFile.fclose();
                            nVobin++;
                            csAux2 = Options.m_csInputIFO[..^5];
                            csAux = $"{nVobin}.VOB";
                            csAux = csAux2 + csAux;
                            inFile = CFILE.OpenRead(FileReader, csAux);
                            if (Util.readbuffer(m_buffer, inFile) != 2048)
                            {
                                Util.MyErrorBox("Input error: Reached end of VOB too early");
                                m_bInProcess = false;
                                iRet = -1;
                            }
                        }

                        if (m_bInProcess == true)
                        {
                            if (Util.IsSynch(m_buffer) != true)
                            {
                                Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                                m_bInProcess = false;
                                iRet = -1;
                            }
                            if (Util.IsNav(m_buffer))
                            {
                                if (m_buffer[0x420] == (byte)(VID % 256) &&
                                    m_buffer[0x41F] == (byte)(VID / 256) &&
                                    m_buffer[0x422] == (byte)CID)
                                    bMyCell = true;
                                else
                                    bMyCell = false;
                            }

                            if (bMyCell)
                            {
                                nSector++;
                                iRet = ProcessPack(true);
                            }
                        }
                    } // For readpacks
                    if (inFile != null) inFile.fclose();
                    inFile = null;
                }  // if (VID== DemuxedVID)
            }   // For Cells 

            CloseAndNull();
            nFrames = 0;

            if (Options.m_bCheckCellt && m_bInProcess == true)
            {
                csAux = Options.m_csOutputPath + '\\' + "Celltimes.txt";
                fout = CFILE.fopen(csAux, "w");

                nDemuxedVID = FileInfo.m_AADT_Vid_list[nVid].VID;

                iArraysize = FileInfo.m_AADT_Cell_list.GetSize();
                for (nCell = nLastCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
                {
                    VID = FileInfo.m_AADT_Cell_list[nCell].VID;
                    if (VID == nDemuxedVID)
                        nLastCell = nCell;
                }

                for (nCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
                {
                    VID = FileInfo.m_AADT_Cell_list[nCell].VID;

                    if (VID == nDemuxedVID)
                    {
                        nFrames += Util.DurationInFrames(FileInfo.m_AADT_Cell_list[nCell].dwDuration);
                        if (nCell != nLastCell || Options.m_bCheckEndTime)
                            fout.fprintf($"{nFrames}\n");
                    }
                }
                fout.fclose();
            }

            m_nTotalFrames = nFrames;

            if (Options.m_bCheckLog && m_bInProcess == true) OutputLog(nVid, 1, DomainType.Titles);

            return iRet;
        }
        public virtual int VIDMDemux(int nVid, object pDlg)
        {
            int nTotalSectors;
            int nSector, nCell;
            int iArraysize;
            int CID, VID, nDemuxedVID;
            long i64IniSec, i64EndSec;
            string csAux, csAux2;
            CFILE inFile;
            CFILE fout;
            long i64;
            bool bMyCell;
            int iRet;
            int nFrames;
            int nLastCell;

            if (nVid >= FileInfo.m_MADT_Vid_list.GetSize())
            {
                Util.MyErrorBox("Error: Selected Vid does not exist");
                m_bInProcess = false;
                return -1;
            }

            IniDemuxGlobalVars();
            if (OpenVideoFile()) return -1;
            m_bInProcess = true;

            // Calculate  the total number of sectors
            nTotalSectors = FileInfo.m_MADT_Vid_list[nVid].iSize;
            nSector = 0;
            iRet = 0;
            nDemuxedVID = FileInfo.m_MADT_Vid_list[nVid].VID;

            iArraysize = FileInfo.m_MADT_Cell_list.GetSize();
            for (nCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
            {
                VID = FileInfo.m_MADT_Cell_list[nCell].VID;
                CID = FileInfo.m_MADT_Cell_list[nCell].CID;

                if (VID == nDemuxedVID)
                {
                    i64IniSec = FileInfo.m_MADT_Cell_list[nCell].iIniSec;
                    i64EndSec = FileInfo.m_MADT_Cell_list[nCell].iEndSec;
                    if (FileInfo.m_bVMGM)
                    {
                        csAux2 = Options.m_csInputIFO[..^3];
                        csAux = csAux2 + "VOB";
                    }
                    else
                    {
                        csAux2 = Options.m_csInputIFO[..^5];
                        csAux = csAux2 + "0.VOB";
                    }
                    inFile = CFILE.OpenRead(FileReader, csAux);
                    if (inFile == null)
                    {
                        Util.MyErrorBox("Error opening input VOB: " + csAux);
                        m_bInProcess = false;
                        iRet = -1;
                    }
                    if (m_bInProcess) inFile.fseek((long)((i64IniSec) * 2048), SeekOrigin.Begin);

                    for (i64 = 0, bMyCell = true; i64 < (i64EndSec - i64IniSec + 1) && m_bInProcess == true; i64++)
                    {
                        //readpack
                        if ((i64 % MODUPDATE) == 0) UpdateProgress(pDlg, (int)((100 * nSector) / nTotalSectors));
                        if (Util.readbuffer(m_buffer, inFile) != 2048)
                        {
                            if (inFile != null) inFile.fclose();
                            Util.MyErrorBox("Input error: Reached end of VOB too early");
                            m_bInProcess = false;
                            iRet = -1;
                        }

                        if (m_bInProcess == true)
                        {
                            if (Util.IsSynch(m_buffer) != true)
                            {
                                Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                                m_bInProcess = false;
                                iRet = -1;
                            }
                            if (Util.IsNav(m_buffer))
                            {
                                if (m_buffer[0x420] == (byte)(VID % 256) &&
                                    m_buffer[0x41F] == (byte)(VID / 256) &&
                                    m_buffer[0x422] == (byte)CID)
                                    bMyCell = true;
                                else
                                    bMyCell = false;
                            }

                            if (bMyCell)
                            {
                                nSector++;
                                iRet = ProcessPack(true);
                            }
                        }
                    } // For readpacks
                    if (inFile != null) inFile.fclose();
                    inFile = null;
                } // If (VID==DemuxedVID)
            }   // For Cells 

            CloseAndNull();

            nFrames = 0;

            if (Options.m_bCheckCellt && m_bInProcess == true)
            {
                csAux = Options.m_csOutputPath + '\\' + "Celltimes.txt";
                fout = CFILE.fopen(csAux, "w");

                nDemuxedVID = FileInfo.m_MADT_Vid_list[nVid].VID;

                iArraysize = FileInfo.m_MADT_Cell_list.GetSize();

                for (nCell = nLastCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
                {
                    VID = FileInfo.m_MADT_Cell_list[nCell].VID;
                    if (VID == nDemuxedVID) nLastCell = nCell;
                }


                for (nCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
                {
                    VID = FileInfo.m_MADT_Cell_list[nCell].VID;

                    if (VID == nDemuxedVID)
                    {
                        nFrames += Util.DurationInFrames(FileInfo.m_MADT_Cell_list[nCell].dwDuration);
                        if (nCell != nLastCell || Options.m_bCheckEndTime)
                            fout.fprintf($"{nFrames}\n");
                    }
                }
                fout.fclose();
            }

            m_nTotalFrames = nFrames;

            if (Options.m_bCheckLog && m_bInProcess == true) OutputLog(nVid, 1, DomainType.Menus);

            return iRet;
        }
        public virtual int CIDDemux(int nCell, object pDlg)
        {
            int nTotalSectors;
            int nSector;
            int k;
            int CID, VID;
            long i64IniSec, i64EndSec;
            long i64sectors;
            int nVobin = 0;
            string csAux, csAux2;
            CFILE inFile;
            CFILE fout;
            long i64;
            bool bMyCell;
            int iRet;
            int nFrames;

            if (nCell >= FileInfo.m_AADT_Cell_list.GetSize())
            {
                Util.MyErrorBox("Error: Selected Cell does not exist");
                m_bInProcess = false;
                return -1;
            }

            IniDemuxGlobalVars();
            if (OpenVideoFile()) return -1;
            m_bInProcess = true;

            // Calculate  the total number of sectors
            nTotalSectors = FileInfo.m_AADT_Cell_list[nCell].iSize;
            nSector = 0;
            iRet = 0;

            VID = FileInfo.m_AADT_Cell_list[nCell].VID;
            CID = FileInfo.m_AADT_Cell_list[nCell].CID;

            i64IniSec = FileInfo.m_AADT_Cell_list[nCell].iIniSec;
            i64EndSec = FileInfo.m_AADT_Cell_list[nCell].iEndSec;
            for (k = 1, i64sectors = 0; k < 10; k++)
            {
                i64sectors += (FileInfo.m_i64VOBSize[k] / 2048);
                if (i64IniSec < i64sectors)
                {
                    i64sectors -= (FileInfo.m_i64VOBSize[k] / 2048);
                    nVobin = k;
                    k = 20;
                }
            }
            csAux2 = Options.m_csInputIFO[..^5];
            csAux = $"{nVobin}.VOB";
            csAux = csAux2 + csAux;
            inFile = CFILE.OpenRead(FileReader, csAux);
            if (inFile == null)
            {
                Util.MyErrorBox("Error opening input VOB: " + csAux);
                m_bInProcess = false;
                iRet = -1;
            }
            if (m_bInProcess) inFile.fseek((long)((i64IniSec - i64sectors) * 2048), SeekOrigin.Begin);

            for (i64 = 0, bMyCell = true; i64 < (i64EndSec - i64IniSec + 1) && m_bInProcess == true; i64++)
            {
                //readpack
                if ((i64 % MODUPDATE) == 0) UpdateProgress(pDlg, (int)((100 * nSector) / nTotalSectors));
                if (Util.readbuffer(m_buffer, inFile) != 2048)
                {
                    if (inFile != null) inFile.fclose();
                    nVobin++;
                    csAux2 = Options.m_csInputIFO[..^5];
                    csAux = $"{nVobin}.VOB";
                    csAux = csAux2 + csAux;
                    inFile = CFILE.OpenRead(FileReader, csAux);
                    if (Util.readbuffer(m_buffer, inFile) != 2048)
                    {
                        Util.MyErrorBox("Input error: Reached end of VOB too early");
                        m_bInProcess = false;
                        iRet = -1;
                    }
                }

                if (m_bInProcess == true)
                {
                    if (Util.IsSynch(m_buffer) != true)
                    {
                        Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                        m_bInProcess = false;
                        iRet = -1;
                    }
                    if (Util.IsNav(m_buffer))
                    {
                        if (m_buffer[0x420] == (byte)(VID % 256) &&
                            m_buffer[0x41F] == (byte)(VID / 256) &&
                            m_buffer[0x422] == (byte)CID)
                            bMyCell = true;
                        else
                            bMyCell = false;
                    }

                    if (bMyCell)
                    {
                        nSector++;
                        iRet = ProcessPack(true);
                    }
                }
            } // For readpacks
            if (inFile != null) inFile.fclose();
            inFile = null;

            CloseAndNull();

            nFrames = 0;

            if (Options.m_bCheckCellt && m_bInProcess == true)
            {
                csAux = Options.m_csOutputPath + '\\' + "Celltimes.txt";
                fout = CFILE.fopen(csAux, "w");
                nFrames = Util.DurationInFrames(FileInfo.m_AADT_Cell_list[nCell].dwDuration);
                if (Options.m_bCheckEndTime)
                    fout.fprintf($"{nFrames}\n");
                fout.fclose();
            }

            m_nTotalFrames = nFrames;

            if (Options.m_bCheckLog && m_bInProcess == true) OutputLog(nCell, 1, DomainType.Titles);

            return iRet;
        }
        public virtual int CIDMDemux(int nCell, object pDlg)
        {
            int nTotalSectors;
            int nSector;
            int CID, VID;
            long i64IniSec, i64EndSec;
            string csAux, csAux2;
            CFILE inFile;
            CFILE fout;
            long i64;
            bool bMyCell;
            int iRet;
            int nFrames;

            if (nCell >= FileInfo.m_MADT_Cell_list.GetSize())
            {
                Util.MyErrorBox("Error: Selected Cell does not exist");
                m_bInProcess = false;
                return -1;
            }

            IniDemuxGlobalVars();
            if (OpenVideoFile()) return -1;
            m_bInProcess = true;

            // Calculate  the total number of sectors
            nTotalSectors = FileInfo.m_MADT_Cell_list[nCell].iSize;
            nSector = 0;
            iRet = 0;

            VID = FileInfo.m_MADT_Cell_list[nCell].VID;
            CID = FileInfo.m_MADT_Cell_list[nCell].CID;

            i64IniSec = FileInfo.m_MADT_Cell_list[nCell].iIniSec;
            i64EndSec = FileInfo.m_MADT_Cell_list[nCell].iEndSec;
            if (FileInfo.m_bVMGM)
            {
                csAux2 = Options.m_csInputIFO[..^3];
                csAux = csAux2 + "VOB";
            }
            else
            {
                csAux2 = Options.m_csInputIFO[..^5];
                csAux = csAux2 + "0.VOB";
            }
            inFile = CFILE.OpenRead(FileReader, csAux);
            if (inFile == null)
            {
                Util.MyErrorBox("Error opening input VOB: " + csAux);
                m_bInProcess = false;
                iRet = -1;
            }
            if (m_bInProcess) inFile.fseek((long)((i64IniSec) * 2048), SeekOrigin.Begin);

            for (i64 = 0, bMyCell = true; i64 < (i64EndSec - i64IniSec + 1) && m_bInProcess == true; i64++)
            {
                //readpack
                if ((i64 % MODUPDATE) == 0) UpdateProgress(pDlg, (int)((100 * nSector) / nTotalSectors));
                if (Util.readbuffer(m_buffer, inFile) != 2048)
                {
                    if (inFile != null) inFile.fclose();
                    Util.MyErrorBox("Input error: Reached end of VOB too early");
                    m_bInProcess = false;
                    iRet = -1;
                }
                if (m_bInProcess == true)
                {
                    if (Util.IsSynch(m_buffer) != true)
                    {
                        Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                        m_bInProcess = false;
                        iRet = -1;
                    }
                    if (Util.IsNav(m_buffer))
                    {
                        if (m_buffer[0x420] == (byte)(VID % 256) &&
                            m_buffer[0x41F] == (byte)(VID / 256) &&
                            m_buffer[0x422] == (byte)CID)
                            bMyCell = true;
                        else
                            bMyCell = false;
                    }

                    if (bMyCell)
                    {
                        nSector++;
                        iRet = ProcessPack(true);
                    }
                }
            } // For readpacks
            if (inFile != null) inFile.fclose();
            inFile = null;

            CloseAndNull();

            nFrames = 0;

            if (Options.m_bCheckCellt && m_bInProcess == true)
            {
                csAux = Options.m_csOutputPath + '\\' + "Celltimes.txt";
                fout = CFILE.fopen(csAux, "w");
                nFrames = Util.DurationInFrames(FileInfo.m_MADT_Cell_list[nCell].dwDuration);
                if (Options.m_bCheckEndTime)
                    fout.fprintf($"{nFrames}\n");
                fout.fclose();
            }

            m_nTotalFrames = nFrames;

            if (Options.m_bCheckLog && m_bInProcess == true) OutputLog(nCell, 1, DomainType.Menus);

            return iRet;
        }
        public virtual void demuxvideo(Ref<byte> buffer)
        {
            int start, nbytes;

            start = 0x17 + buffer[0x16];
            nbytes = buffer[0x12] * 256 + buffer[0x13] + 0x14;

            Util.writebuffer(buffer.AtIndex(start), fvid, nbytes - start);
        }
        public virtual void demuxaudio(Ref<byte> buffer, int nBytesOffset)
        {
            int start, nbytes, i, j;
            int nbit, ncha;
            byte streamID;
            Ref<byte> mybuffer = new ByteArrayRef(new byte[2050], 0);

            start = 0x17 + buffer[0x16];
            nbytes = buffer[0x12] * 256 + buffer[0x13] + 0x14;
            if (Util.IsAudMpeg(buffer))
                streamID = buffer[0x11];
            else
            {
                streamID = buffer[start];
                start += 4;
            }

            // Open File descriptor if it isn't open
            if (check_aud_open(streamID) == 1)
                return;

            // Check if PCM
            if (streamID >= 0xa0 && streamID <= 0xa7)
            {
                start += 3;

                if (nchannels[streamID & 0x7] == -1)
                    nchannels[streamID & 0x7] = (buffer[0x17 + buffer[0x16] + 5] & 0x7) + 1;

                nbit = (buffer[0x17 + buffer[0x16] + 5] >> 6) & 0x3;

                if (nbit == 0) nbit = 16;
                else if (nbit == 1) nbit = 20;
                else if (nbit == 2) nbit = 24;
                else nbit = 0;

                if (nbitspersample[streamID & 0x7] == -1)
                    nbitspersample[streamID & 0x7] = nbit;
                if (nbitspersample[streamID & 0x7] != nbit)
                    nbit = nbitspersample[streamID & 0x7];

                if (fsample[streamID & 0x7] == -1)
                {
                    fsample[streamID & 0x7] = (buffer[0x17 + buffer[0x16] + 5] >> 4) & 0x3;
                    if (fsample[streamID & 0x7] == 0) fsample[streamID & 0x7] = 48000;
                    else fsample[streamID & 0x7] = 96000;
                }

                ncha = nchannels[streamID & 0x7];
                if (nbit == 24)
                {
                    for (j = start; j < (nbytes - 6 * ncha + 1); j += (6 * ncha))
                    {
                        for (i = 0; i < 2 * ncha; i++)
                        {
                            mybuffer[j + 3 * i + 2] = buffer[j + 2 * i];
                            mybuffer[j + 3 * i + 1] = buffer[j + 2 * i + 1];
                            mybuffer[j + 3 * i] = buffer[j + 4 * ncha + i];
                        }
                    }

                }
                else if (nbit == 16)
                {
                    for (i = start; i < (nbytes - 1); i += 2)
                    {
                        mybuffer[i] = buffer[i + 1];
                        mybuffer[i + 1] = buffer[i];
                    }
                }
                else if (nbit == 20)
                {
                    for (j = start; j < (nbytes - 5 * ncha + 1); j += (5 * ncha))
                    {
                        for (i = 0; i < ncha; i++)
                        {
                            mybuffer[j + 5 * i + 0] = (byte)((hi_nib<byte>(buffer[j + 4 * ncha + i]) << 4) + hi_nib<byte>(buffer[j + 4 * i + 1]));
                            mybuffer[j + 5 * i + 1] = (byte)((lo_nib(buffer[j + 4 * i + 1]) << 4) + hi_nib(buffer[j + 4 * i + 0]));
                            mybuffer[j + 5 * i + 2] = (byte)((lo_nib(buffer[j + 4 * i + 0]) << 4) + lo_nib(buffer[j + 4 * ncha + i]));
                            mybuffer[j + 5 * i + 3] = buffer[j + 4 * i + 3];
                            mybuffer[j + 5 * i + 4] = buffer[j + 4 * i + 2];
                        }
                    }
                }

                if ((nbit == 16 && ((nbytes - start) % 2) != 0) ||
                    (nbit == 24 && ((nbytes - start) % (6 * ncha)) != 0) ||
                    (nbit == 20 && ((nbytes - start) % (5 * ncha)) != 0))

                    Util.MyErrorBox("Error: Uncompleted PCM sample");

                // if PCM do not take into account nBytesOffset
                Util.writebuffer(mybuffer.AtIndex(start), faud[streamID & 0x7], nbytes - start);
            }
            else
            {
                // Very easy, no process at all, but take into account nBytesOffset...
                start += nBytesOffset;
                Util.writebuffer(buffer.AtIndex(start), faud[streamID & 0x7], nbytes - start);

            }
        }
        public virtual void demuxsubs(Ref<byte> buffer)
        {
            int start, nbytes;
            byte streamID;
            int k;
            Ref<byte> mybuff = new ByteArrayRef(new byte[10], 0);
            int iPTS;

            start = 0x17 + buffer[0x16];
            nbytes = buffer[0x12] * 256 + buffer[0x13] + 0x14;
            streamID = buffer[start];

            if (check_sub_open(streamID) == 1)
                return;
            if ((buffer[0x16] == 0) || (m_buffer[0x15] & 0x80) != 0x80)
                Util.writebuffer(buffer.AtIndex(start + 1), fsub[streamID & 0x1F], nbytes - start - 1);
            else
            {
                // fill 10 characters
                for (k = 0; k < 10; k++)
                    mybuff[k] = 0;

                iPTS = m_iSubPTS - m_iFirstNavPTS0 + m_iOffsetPTS;

                mybuff[0] = 0x53;
                mybuff[1] = 0x50;
                mybuff[2] = (byte)(iPTS % 256);
                mybuff[3] = (byte)((iPTS >> 8) % 256);
                mybuff[4] = (byte)((iPTS >> 16) % 256);
                mybuff[5] = (byte)((iPTS >> 24) % 256);

                Util.writebuffer(mybuff, fsub[streamID & 0x1F], 10);
                Util.writebuffer(buffer.AtIndex(start + 1), fsub[streamID & 0x1F], nbytes - start - 1);
            }
        }
        public virtual void WritePack(Ref<byte> buffer)
        {
            string csAux;

            if (m_bInProcess == true)
            {
                if (Options.m_bCheckVob2)
                {
                    if (fvob == null || m_nVidout != m_nCurrVid)
                    {
                        m_nCurrVid = m_nVidout;
                        if (fvob != null) fvob.fclose();
                        if (Options.m_iDomain == DomainType.Titles)
                            csAux = $"VTS_01_1_{m_nVidout:000}.VOB";
                        else
                            csAux = $"VTS_01_0_{m_nVidout:000}.VOB";
                        csAux = Options.m_csOutputPath + '\\' + csAux;
                        fvob = CFILE.fopen(csAux, "wb");
                    }
                }
                else
                {
                    if (fvob == null || ((m_i64OutputLBA) % (512 * 1024 - 1)) == 0)
                    {
                        if (fvob != null) fvob.fclose();
                        if (Options.m_iDomain == DomainType.Titles)
                        {
                            m_nVobout++;
                            csAux = $"VTS_01_{m_nVobout}.VOB";
                        }
                        else
                            csAux = "VTS_01_0.VOB";

                        csAux = Options.m_csOutputPath + '\\' + csAux;
                        fvob = CFILE.fopen(csAux, "wb");
                    }
                }

                if (fvob != null) Util.writebuffer(buffer, fvob, 2048);
                m_i64OutputLBA++;
            }
        }
        public virtual void CloseAndNull()
        {
            int i;
            uint byterate, nblockalign;
            FileInfo statbuf;
            long i64size;


            if (fvob != null)
            {
                fvob.fclose();
                fvob = null;
            }
            if (fvid != null)
            {
                fvid.fclose();
                fvid = null;
            }
            for (i = 0; i < 32; i++)
                if (fsub[i] != null)
                {
                    fsub[i].fclose();
                    fsub[i] = null;
                }
            for (i = 0; i < 8; i++)
                if (faud[i] != null)
                {
                    if (m_audfmt[i] == AudioFormat.WAV)
                    {
                        i64size = 0;
                        faud[i].fclose();

                        CFILE? file = CFILE.OpenRead(FileReader, m_csAudname[i]);
                        if (file != null)
                        {
                            i64size = file.Size;
                            file.fclose();
                        }

                        if (i64size >= 8) i64size -= 8;

                        faud[i] = CFILE.fopen(m_csAudname[i], "r+b");

                        faud[i].fseek(4, SeekOrigin.Begin);
                        faud[i].fputc((byte)(i64size % 256));
                        faud[i].fputc((byte)((i64size >> 8) % 256));
                        faud[i].fputc((byte)((i64size >> 16) % 256));
                        faud[i].fputc((byte)((i64size >> 24) % 256));

                        //				# of channels (2 bytes!!)
                        faud[i].fseek(22, SeekOrigin.Begin);
                        faud[i].fputc((byte)(nchannels[i] % 256));

                        //				Sample rate ( 48k / 96k in DVD)
                        faud[i].fseek(24, SeekOrigin.Begin);
                        faud[i].fputc((byte)(fsample[i] % 256));
                        faud[i].fputc((byte)((fsample[i] >> 8) % 256));
                        faud[i].fputc((byte)((fsample[i] >> 16) % 256));
                        faud[i].fputc((byte)((fsample[i] >> 24) % 256));

                        //				Byte rate ( 4 bytes)== SampleRate * NumChannels * BitsPerSample/8
                        //                    6000* NumChannels * BitsPerSample
                        byterate = (uint)((fsample[i] / 8) * nchannels[i] * nbitspersample[i]);
                        faud[i].fseek(28, SeekOrigin.Begin);
                        faud[i].fputc((byte)(byterate % 256));
                        faud[i].fputc((byte)((byterate >> 8) % 256));
                        faud[i].fputc((byte)((byterate >> 16) % 256));
                        faud[i].fputc((byte)((byterate >> 24) % 256));


                        //				Block align ( 2 bytes)== NumChannels * BitsPerSample/8
                        nblockalign = (uint)(nbitspersample[i] * nchannels[i] / 8);
                        faud[i].fseek(32, SeekOrigin.Begin);
                        faud[i].fputc((byte)(nblockalign % 256));
                        faud[i].fputc((byte)((nblockalign >> 8) % 256));

                        //				Bits per sample ( 2 bytes)
                        faud[i].fseek(34, SeekOrigin.Begin);
                        faud[i].fputc((byte)(nbitspersample[i] % 256));

                        if (i64size >= 36) i64size -= 36;
                        faud[i].fseek(40, SeekOrigin.Begin);
                        //				fseek(faud[i],54,SEEK_SET);
                        faud[i].fputc((byte)(i64size % 256));
                        faud[i].fputc((byte)((i64size >> 8) % 256));
                        faud[i].fputc((byte)((i64size >> 16) % 256));
                        faud[i].fputc((byte)((i64size >> 24) % 256));
                    }
                    faud[i].fclose();
                    faud[i] = null;
                }
        }
        public virtual int check_sub_open(byte i)
        {
            string csAux;

            i -= 0x20;

            if (i > 31) return -1;

            if (fsub[i] == null)
            {
                csAux = $"Subpictures_{(i + 0x20):X2}.sup";
                csAux = Options.m_csOutputPath + '\\' + csAux;
                if ((fsub[i] = CFILE.fopen(csAux, "wb")) == null)
                {
                    Util.MyErrorBox("Error opening output subs file:" + csAux);
                    m_bInProcess = false;
                    return 1;
                }
                else return 0;
            }
            else
                return 0;
        }
        public virtual int check_aud_open(byte i)
        {
            string csAux;
            byte ii;
            /*
            0x80-0x87: ac3  --> ac3
            0x88-0x8f: dts  --> dts
            0x90-0x97: sdds --> dds
            0x98-0x9f: unknown
            0xa0-0xa7: lpcm  -->wav
            0xa8-0xaf: unknown
            0xb0-0xbf: unknown
            0xc0-0xc8: mpeg1 --> mpa
            0xc8-0xcf: unknown 
            0xd0-0xd7: mpeg2 --> mpb
            0xd8-0xdf: unknown 
            ---------------------------------------------
            SDSS   AC3   DTS   LPCM   MPEG-1   MPEG-2

             90    80    88     A0     C0       D0
             91    81    89     A1     C1       D1
             92    82    8A     A2     C2       D2
             93    83    8B     A3     C3       D3
             94    84    8C     A4     C4       D4
             95    85    8D     A5     C5       D5
             96    86    8E     A6     C6       D6
             97    87    8F     A7     C7       D7
            ---------------------------------------------
            */

            ii = i;

            if (ii < 0x80) return -1;

            i = (byte)(i & 0x7);

            if (faud[i] == null)
            {
                if (ii >= 0x80 && ii <= 0x87)
                {
                    csAux = $"AudioFile_{(i + 0x80):X2}.ac3";
                    m_audfmt[i] = AudioFormat.AC3;
                }
                else if (ii >= 0x88 && ii <= 0x8f)
                {
                    csAux = $"AudioFile_{(i + 0x88):X2}.dts";
                    m_audfmt[i] = AudioFormat.DTS;
                }
                else if (ii >= 0x90 && ii <= 0x97)
                {
                    csAux = $"AudioFile_{(i + 0x90):X2}.dds";
                    m_audfmt[i] = AudioFormat.DDS;
                }
                else if (ii >= 0xa0 && ii <= 0xa7)
                {
                    csAux = $"AudioFile_{(i + 0xa0):X2}.wav";
                    m_audfmt[i] = AudioFormat.WAV;
                }
                else if (ii >= 0xc0 && ii <= 0xc7)
                {
                    csAux = $"AudioFile_{(i + 0xc0):X2}.mpa";
                    m_audfmt[i] = AudioFormat.MP1;
                }
                else if (ii >= 0xd0 && ii <= 0xd7)
                {
                    csAux = $"AudioFile_{(i + 0xd0):X2}.mpa";
                    m_audfmt[i] = AudioFormat.MP2;
                }
                else
                {
                    csAux = $"AudioFile_{(ii):X2}.unk";
                    m_audfmt[i] = AudioFormat.Unknown;
                }

                csAux = Options.m_csOutputPath + '\\' + csAux;
                m_csAudname[i] = csAux;

                if ((faud[i] = CFILE.fopen(csAux, "wb")) == null)
                {
                    Util.MyErrorBox("Error opening output audio file:" + csAux);
                    m_bInProcess = false;
                    return 1;
                }

                if (m_audfmt[i] == AudioFormat.WAV)
                {
                    faud[i].fwrite(pcmheader, sizeof(byte), 44);
                }

                return 0;
            }
            else
                return 0;
        }

        private static int nPack = 0;
        private static int nFirstRef = 0;

        public virtual int ProcessPack(bool bWrite)
        {
            int sID;
            bool bFirstAud;
            int nBytesOffset;

            if (bWrite && Options.m_bCheckVob)
            {
                if (Util.IsNav(m_buffer))
                {
                    if (Options.m_bCheckLBA) Util.ModifyLBA(m_buffer, m_i64OutputLBA);
                    m_nVidout = (int)Util.GetNbytes(2, m_buffer.AtIndex(0x41f));
                    m_nCidout = (int)m_buffer[0x422];
                    nFirstRef = (int)Util.GetNbytes(4, m_buffer.AtIndex(0x413));
                    nPack = 0;

                    bNewCell = false;
                    if (m_nVidout != m_nLastVid || m_nCidout != m_nLastCid)
                    {
                        bNewCell = true;
                        m_nLastVid = m_nVidout;
                        m_nLastCid = m_nCidout;
                    }
                }
                else
                    nPack++;
                if ((Util.IsNav(m_buffer) && Options.m_bCheckNavPack) ||
                     (Util.IsAudio(m_buffer) && Options.m_bCheckAudioPack) ||
                     (Util.IsSubs(m_buffer) && Options.m_bCheckSubPack))
                    WritePack(m_buffer);
                else if (Util.IsVideo(m_buffer) && Options.m_bCheckVideoPack)
                {
                    if (!Options.m_bCheckIFrame)
                        WritePack(m_buffer);
                    else
                    {
                        //				if (nFirstRef == nPack)  
                        //					if ( ! PatchEndOfSequence(m_buffer))
                        //						WritePack (Pad_pack);
                        if (bNewCell && nFirstRef >= nPack) WritePack(m_buffer);
                    }
                }

            }
            if (Util.IsNav(m_buffer))
            {
                // do nothing
                m_nNavPacks++;
                m_iNavPTS0 = (int)Util.GetNbytes(4, m_buffer.AtIndex(0x39));
                m_iNavPTS1 = (int)Util.GetNbytes(4, m_buffer.AtIndex(0x3d));
                if (m_iFirstNavPTS0 == 0) m_iFirstNavPTS0 = m_iNavPTS0;
                if (m_iNavPTS1_old > m_iNavPTS0)
                {
                    // Discontinuity, so add the offset 
                    m_iOffsetPTS += (m_iNavPTS1_old - m_iNavPTS0);
                }
                m_iNavPTS0_old = m_iNavPTS0;
                m_iNavPTS1_old = m_iNavPTS1;
            }
            else if (Util.IsVideo(m_buffer))
            {
                m_nVidPacks++;
                if ((m_buffer[0x15] & 0x80) != 0)
                {
                    m_iVidPTS = Util.readpts(m_buffer.AtIndex(0x17));
                    if (m_iFirstVidPTS == 0) m_iFirstVidPTS = m_iVidPTS;
                }
                if (bWrite && Options.m_bCheckVid) demuxvideo(m_buffer);
            }
            else if (Util.IsAudio(m_buffer))
            {
                m_nAudPacks++;
                nBytesOffset = 0;

                sID = Util.getAudId(m_buffer) & 0x07;

                bFirstAud = false;

                if ((m_buffer[0x15] & 0x80) != 0)
                {
                    if (m_iFirstAudPTS[sID] == 0)
                    {
                        bFirstAud = true;
                        m_iAudPTS = Util.readpts(m_buffer.AtIndex(0x17));
                        m_iFirstAudPTS[sID] = m_iAudPTS;
                        //				m_iAudIndex[sID]=m_buffer[0x17+m_buffer[0x16]];
                        m_iAudIndex[sID] = Util.getAudId(m_buffer);
                    }
                }
                if (bFirstAud)
                {
                    nBytesOffset = GetAudHeader(m_buffer);
                    if (nBytesOffset < 0)
                        // This pack does not have an Audio Frame Header, so its PTS is  not valid.
                        m_iFirstAudPTS[sID] = 0;
                }

                if (bWrite && Options.m_bCheckAud && m_iFirstAudPTS[sID] != 0)
                {
                    demuxaudio(m_buffer, nBytesOffset);
                }
            }
            else if (Util.IsSubs(m_buffer))
            {
                m_nSubPacks++;
                sID = m_buffer[0x17 + m_buffer[0x16]] & 0x1F;

                if ((m_buffer[0x15] & 0x80) != 0)
                {
                    m_iSubPTS = Util.readpts(m_buffer.AtIndex(0x17));
                    if (m_iFirstSubPTS[sID] == 0)
                        m_iFirstSubPTS[sID] = m_iSubPTS;
                }
                if (bWrite && Options.m_bCheckSub) demuxsubs(m_buffer);
            }
            else if (Util.IsPad(m_buffer))
            {
                m_nPadPacks++;
            }
            else
            {
                m_nUnkPacks++;
            }
            return 0;
        }
        public virtual void OutputLog(int nItem, int nAng, DomainType iDomain)
        {

        }

        public virtual void IniDemuxGlobalVars()
        {
            int k;
            string csAux;

            // clear PTS
            for (k = 0; k < 32; k++)
                m_iFirstSubPTS[k] = 0;
            for (k = 0; k < 8; k++)
            {
                m_iFirstAudPTS[k] = 0;
                nchannels[k] = -1;
                nbitspersample[k] = -1;
                fsample[k] = -1;
            }
            m_iFirstVidPTS = 0;
            m_iFirstNavPTS0 = 0;
            m_iNavPTS0_old = m_iNavPTS0 = 0;
            m_iNavPTS1_old = m_iNavPTS1 = 0;

            m_nNavPacks = m_nVidPacks = m_nAudPacks = m_nSubPacks = m_nUnkPacks = m_nPadPacks = 0;
            m_i64OutputLBA = 0;
            m_nVobout = m_nVidout = m_nCidout = 0;
            m_nLastVid = 0;
            m_nLastCid = 0;

            m_nCurrVid = 0;
            m_iOffsetPTS = 0;
            bNewCell = false;
        }
        public virtual bool OpenVideoFile()
        {
            string csAux;

            if (Options.m_bCheckVid)
            {
                csAux = Options.m_csOutputPath + '\\' + "VideoFile.m2v";
                fvid = CFILE.fopen(csAux, "wb");
                if (fvid == null) return true;
            }

            return false;
        }
        public virtual int GetAudioDelay(ModeType iMode, int nSelection)
        {
            int VID = 0, CID = 0;
            int k, nCell;
            long i64IniSec, i64EndSec;
            long i64sectors;
            int nVobin = 0;
            string csAux, csAux2;
            CFILE inFile;
            long i64;
            bool bMyCell;
            int iRet;

            IniDemuxGlobalVars();

            if (iMode == ModeType.PGC)
            {
                if (nSelection >= FileInfo.m_nPGCs)
                {
                    Util.MyErrorBox("Error: PGC does not exist");
                    return -1;
                }
                nCell = 0;
                VID = Util.GetNbytes(2, FileInfo.m_pIFO.AtIndex(FileInfo.m_C_POST[nSelection] + 4 * nCell));
                CID = FileInfo.m_pIFO[FileInfo.m_C_POST[nSelection] + 3 + 4 * nCell];
            }
            else if (iMode == ModeType.VID)
            {
                if (nSelection >= FileInfo.m_AADT_Vid_list.GetSize())
                {
                    Util.MyErrorBox("Error: VID does not exist");
                    return -1;
                }
                VID = FileInfo.m_AADT_Vid_list[nSelection].VID;
                CID = -1;
                for (k = 0; k < FileInfo.m_AADT_Cell_list.GetSize() && CID == -1; k++)
                {
                    if (VID == FileInfo.m_AADT_Cell_list[k].VID)
                        CID = FileInfo.m_AADT_Cell_list[k].CID;
                }

            }
            else if (iMode == ModeType.CID)
            {
                if (nSelection >= FileInfo.m_AADT_Cell_list.GetSize())
                {
                    Util.MyErrorBox("Error: CID does not exist");
                    return -1;
                }
                VID = FileInfo.m_AADT_Cell_list[nSelection].VID;
                CID = FileInfo.m_AADT_Cell_list[nSelection].CID;
            }

            for (k = 0, nCell = -1; k < FileInfo.m_AADT_Cell_list.GetSize() && nCell == -1; k++)
            {
                if (VID == FileInfo.m_AADT_Cell_list[k].VID &&
                    CID == FileInfo.m_AADT_Cell_list[k].CID)
                    nCell = k;
            }

            if (nCell < 0)
            {
                Util.MyErrorBox("Error: VID/CID not found!.");
                return -1;
            }
            //
            // Now we have VID; CID; and the index in Cell Array "nCell".
            // So we are going to open the VOB and read the delays using ProcessPack(false)
            i64IniSec = FileInfo.m_AADT_Cell_list[nCell].iIniSec;
            i64EndSec = FileInfo.m_AADT_Cell_list[nCell].iEndSec;

            iRet = 0;
            for (k = 1, i64sectors = 0; k < 10; k++)
            {
                i64sectors += (FileInfo.m_i64VOBSize[k] / 2048);
                if (i64IniSec < i64sectors)
                {
                    i64sectors -= (FileInfo.m_i64VOBSize[k] / 2048);
                    nVobin = k;
                    k = 20;
                }
            }
            csAux2 = Options.m_csInputIFO[..^5];
            csAux = $"{nVobin}.VOB";
            csAux = csAux2 + csAux;
            inFile = CFILE.OpenRead(FileReader, csAux);
            if (inFile == null)
            {
                Util.MyErrorBox("Error opening input VOB: " + csAux);
                iRet = -1;
            }
            if (iRet == 0) inFile.fseek((long)((i64IniSec - i64sectors) * 2048), SeekOrigin.Begin);

            for (i64 = 0, bMyCell = true; iRet == 0 && i64 < (i64EndSec - i64IniSec + 1) && i64 < MAXLOOKFORAUDIO; i64++)
            {
                //readpack
                if (Util.readbuffer(m_buffer, inFile) != 2048)
                {
                    if (inFile != null) inFile.fclose();
                    nVobin++;
                    csAux2 = Options.m_csInputIFO[..^5];
                    csAux = $"{nVobin}.VOB";
                    csAux = csAux2 + csAux;
                    inFile = CFILE.OpenRead(FileReader, csAux);
                    if (Util.readbuffer(m_buffer, inFile) != 2048)
                    {
                        Util.MyErrorBox("Input error: Reached end of VOB too early");
                        iRet = -1;
                    }
                }

                if (iRet == 0)
                {
                    if (Util.IsSynch(m_buffer) != true)
                    {
                        Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                        iRet = -1;
                    }
                    if ((iRet == 0) && Util.IsNav(m_buffer))
                    {
                        if (m_buffer[0x420] == (byte)(VID % 256) &&
                            m_buffer[0x41F] == (byte)(VID / 256) &&
                            m_buffer[0x422] == (byte)CID)
                            bMyCell = true;
                        else
                            bMyCell = false;
                    }

                    if (iRet == 0 && bMyCell)
                    {
                        iRet = ProcessPack(false);
                    }
                }
            } // For readpacks
            if (inFile != null) inFile.fclose();
            inFile = null;

            return iRet;
        }
        public virtual int GetMAudioDelay(ModeType iMode, int nSelection)
        {
            int VID = 0, CID = 0;
            int k, nCell;
            long i64IniSec, i64EndSec;
            string csAux, csAux2;
            CFILE inFile;
            long i64;
            bool bMyCell;
            int iRet;

            IniDemuxGlobalVars();

            if (iMode == ModeType.PGC)
            {
                if (nSelection >= FileInfo.m_nMPGCs)
                {
                    Util.MyErrorBox("Error: PGC does not exist");
                    return -1;
                }
                nCell = 0;
                VID = Util.GetNbytes(2, FileInfo.m_pIFO.AtIndex(FileInfo.m_M_C_POST[nSelection] + 4 * nCell));
                CID = FileInfo.m_pIFO[FileInfo.m_M_C_POST[nSelection] + 3 + 4 * nCell];
            }
            else if (iMode == ModeType.VID)
            {
                if (nSelection >= FileInfo.m_MADT_Vid_list.GetSize())
                {
                    Util.MyErrorBox("Error: VID does not exist");
                    return -1;
                }
                VID = FileInfo.m_MADT_Vid_list[nSelection].VID;
                CID = -1;
                for (k = 0; k < FileInfo.m_MADT_Cell_list.GetSize() && CID == -1; k++)
                {
                    if (VID == FileInfo.m_MADT_Cell_list[k].VID)
                        CID = FileInfo.m_MADT_Cell_list[k].CID;
                }

            }
            else if (iMode == ModeType.CID)
            {
                if (nSelection >= FileInfo.m_MADT_Cell_list.GetSize())
                {
                    Util.MyErrorBox("Error: CID does not exist");
                    return -1;
                }
                VID = FileInfo.m_MADT_Cell_list[nSelection].VID;
                CID = FileInfo.m_MADT_Cell_list[nSelection].CID;
            }

            for (k = 0, nCell = -1; k < FileInfo.m_MADT_Cell_list.GetSize() && nCell == -1; k++)
            {
                if (VID == FileInfo.m_MADT_Cell_list[k].VID &&
                    CID == FileInfo.m_MADT_Cell_list[k].CID)
                    nCell = k;
            }

            if (nCell < 0)
            {
                Util.MyErrorBox("Error: VID/CID not found!.");
                return -1;
            }
            //
            // Now we have VID; CID; and the index in Cell Array "nCell".
            // So we are going to open the VOB and read the delays using ProcessPack(false)
            i64IniSec = FileInfo.m_MADT_Cell_list[nCell].iIniSec;
            i64EndSec = FileInfo.m_MADT_Cell_list[nCell].iEndSec;

            iRet = 0;

            if (FileInfo.m_bVMGM)
            {
                csAux2 = Options.m_csInputIFO[..^3];
                csAux = csAux2 + "VOB";
            }
            else
            {
                csAux2 = Options.m_csInputIFO[..^5];
                csAux = csAux2 + "0.VOB";
            }
            inFile = CFILE.OpenRead(FileReader, csAux);
            if (inFile == null)
            {
                Util.MyErrorBox("Error opening input VOB: " + csAux);
                iRet = -1;
            }
            if (iRet == 0) inFile.fseek((long)((i64IniSec) * 2048), SeekOrigin.Begin);

            for (i64 = 0, bMyCell = true; iRet == 0 && i64 < (i64EndSec - i64IniSec + 1) && i64 < MAXLOOKFORAUDIO; i64++)
            {
                //readpack
                if (Util.readbuffer(m_buffer, inFile) != 2048)
                {
                    Util.MyErrorBox("Input error: Reached end of VOB too early");
                    iRet = -1;
                }

                if (iRet == 0)
                {
                    if (Util.IsSynch(m_buffer) != true)
                    {
                        Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                        iRet = -1;
                    }
                    if ((iRet == 0) && Util.IsNav(m_buffer))
                    {
                        if (m_buffer[0x420] == (byte)(VID % 256) &&
                            m_buffer[0x41F] == (byte)(VID / 256) &&
                            m_buffer[0x422] == (byte)CID)
                            bMyCell = true;
                        else
                            bMyCell = false;
                    }

                    if (iRet == 0 && bMyCell)
                    {
                        iRet = ProcessPack(false);
                    }
                }
            } // For readpacks
            if (inFile != null) inFile.fclose();
            inFile = null;

            return iRet;
        }

        // Returns the number of bytes from audio start until first header
        // If no header found  returns -1
        public virtual int GetAudHeader(Ref<byte> buffer)
        {
            int i, start, nbytes;
            byte streamID;
            int firstheader, nHeaders;
            bool bFound = false;

            start = 0x17 + buffer[0x16];
            nbytes = buffer[0x12] * 256 + buffer[0x13] + 0x14;
            if (Util.IsAudMpeg(buffer))
                streamID = buffer[0x11];
            else
            {
                streamID = buffer[start];
                start += 4;
            }

            firstheader = 0;

            // Check if PCM
            if (streamID >= 0xa0 && streamID <= 0xa7) return 0;
            if (streamID >= 0x80 && streamID <= 0x8f)
            {
                // Stream is AC3 or DTS...
                nHeaders = buffer[start - 3];
                if (nHeaders != 0)
                {
                    bFound = true;
                    firstheader = buffer[start - 2] * 256 + buffer[start - 1] - 1;
                }
                else
                    bFound = false;
            }
            else if (streamID >= 0xc0 && streamID <= 0xc7)
            {
                // Stream is MPEG ...
                for (i = start, bFound = false; i < (nbytes - 1) && bFound == false; i++)
                {
                    //			if ( buffer[start+i] == 0xFF && (buffer[start+1+i] & 0xF0 )== 0xF0 )
                    if (buffer[i] == 0xFF && (buffer[i + 1] & 0xF0) == 0xF0)
                    {
                        bFound = true;
                        firstheader = i - start;
                    }
                }
            }

            if ((start + firstheader) >= nbytes) bFound = false;

            if (bFound)
                return firstheader;
            else
                return -1;
        }
    }
}