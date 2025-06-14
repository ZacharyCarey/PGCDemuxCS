using PgcDemuxCS;
using PgcDemuxCS;
using PgcDemuxCS.DVD;
using PgcDemuxCS.DVD.IfoTypes.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgcDemuxCS
{
    internal class DomainInfo
    {
        /// <summary>
        /// Number of PGC's found in the file
        /// </summary>
        public int m_nPGCs;

        public int[] m_nCells = new int[IfoInfo.MAX_PGC];
        public CellPositionInfo[][] m_C_POST = new CellPositionInfo[IfoInfo.MAX_PGC][];
        public CellPlaybackInfo[][] m_C_PBKT = new CellPlaybackInfo[IfoInfo.MAX_PGC][];

        // Compiled list of menu and title cells
        public CArray<ADT_CELL_LIST> m_AADT_Cell_list = new();
        public CArray<ADT_VID_LIST> m_AADT_Vid_list = new();
    }

    /// <summary>
    /// Reads required information about the specified IFO
    /// </summary>
    internal class IfoInfo
    {
        internal const int MAXLENGTH = 20 * 1024 * 1024;
        internal const int MAX_PGC = 32768;
        internal const int MAX_MPGC = 32768;
        internal const int MAX_LU = 100;

        public DomainInfo MenuInfo = new();
        public DomainInfo TitleInfo = new();

        /// <summary>
        /// True indicates the VMGM file is selected (VIDEO_TS.IFO)
        /// </summary>
        public bool m_bVMGM;

        public CellAddressTable? m_iVTS_C_ADT;
        public pgci_ut_t m_iVTSM_PGCI;
        public CellAddressTable m_iVTSM_C_ADT;

        public readonly int[] m_nAngles = new int[MAX_PGC];

        public readonly TimeSpan[] m_dwDuration = new TimeSpan[MAX_PGC];
        public readonly TimeSpan[] m_dwMDuration = new TimeSpan[MAX_MPGC];

        public readonly ProgamChainInformationTable[] m_iVTSM_LU = new ProgamChainInformationTable[MAX_LU];
        public readonly int[] m_nIniPGCinLU = new int[MAX_LU];
        public readonly int[] m_nPGCinLU = new int[MAX_LU];
        public readonly PGC[] m_iMENU_PGC = new PGC[MAX_MPGC];
        public readonly int[] m_nLU_MPGC = new int[MAX_MPGC];

        public int m_nLUs;
        public int nLU, nAbsPGC;

        /// <summary>
        /// The size of each VOB file. There can be up to 10 VOB files for each IFO
        /// </summary>
        public readonly long[] m_i64VOBSize = new long[10];

        /// <summary>
        /// The number of detected VOB files
        /// </summary>
        public int m_nVobFiles;

        public readonly int VtsNumber;
        public bool IsVMG => VtsNumber == 0; // TODO replace m_bVMGM

        public IfoInfo(IIfoFileReader reader, PgcDemuxOptions options)
        {
            string csAux, csAux2;
            int i, j, k, kk, nCell, nVIDs;
            kk = 0;
            ADT_CELL_LIST myADT_Cell;
            ADT_VID_LIST myADT_Vid;
            int nTotADT, nADT, VidADT, CidADT;
            int iArraysize;
            bool bAlready, bEndAngle;
            int iIniSec, iEndSec;
            FileInfo statbuf;
            int iSize, iCat;

            string ifoName = options.m_csInputIFO.ToUpper();
            if ((ifoName[^6..] != "_0.IFO" || ifoName[..4] != "VTS_") && ifoName[^12..] != "VIDEO_TS.IFO")
            {
                Util.MyErrorBox("Invalid input file!");
                throw new ArgumentException("Invalid input file");
            }

            if (ifoName == "VIDEO_TS.IFO")
            {
                m_bVMGM = true;
                options.Domain = DomainType.Menus;
                VtsNumber = 0;
            }
            else
            {
                m_bVMGM = false;
                VtsNumber = int.Parse(ifoName[4..6]);
            }

            IfoBase ifo = IfoBase.Open(reader, Path.GetFileNameWithoutExtension(ifoName));

            TitleInfo.m_AADT_Cell_list.RemoveAll();
            MenuInfo.m_AADT_Cell_list.RemoveAll();
            TitleInfo.m_AADT_Vid_list.RemoveAll();
            MenuInfo.m_AADT_Vid_list.RemoveAll();


            // Get Title Cells
            if (ifo is VmgIfo vmg)
            {
                m_iVTSM_PGCI = ifo.MenuProgramChainTable;
                m_iVTSM_C_ADT = ifo.MenuCellAddressTable;
                m_iVTS_C_ADT = null;
                TitleInfo.m_nPGCs = 0;
            }
            else if (ifo is VtsIfo vts)
            {
                m_iVTSM_PGCI = ifo.MenuProgramChainTable;
                m_iVTSM_C_ADT = ifo.MenuCellAddressTable;
                m_iVTS_C_ADT = vts.TitleCellAddressTable;
                TitleInfo.m_nPGCs = vts.TitleProgramChainTable.Count;

                // Title PGCs	
                if (TitleInfo.m_nPGCs > MAX_PGC)
                {
                    csAux = $"ERROR: Max PGCs limit ({MAX_PGC}) has been reached.";
                    Util.MyErrorBox(csAux);
                    throw new IOException(csAux);
                }
                for (k = 0; k < TitleInfo.m_nPGCs; k++)
                {
                    var pgc = vts.TitleProgramChainTable[k].Pgc;
                    m_dwDuration[k] = pgc.PlaybackTime;

                    TitleInfo.m_C_PBKT[k] = (pgc.CellPlayback.Length == 0) ? null : pgc.CellPlayback;
                    TitleInfo.m_C_POST[k] = (pgc.CellPosition.Length == 0) ? null : pgc.CellPosition;

                    TitleInfo.m_nCells[k] = vts.TitleProgramChainTable[k].Pgc.NumberOfCells;


                    m_nAngles[k] = 1;

                    for (nCell = 0, bEndAngle = false; nCell < TitleInfo.m_nCells[k] && bEndAngle == false; nCell++)
                    {
                        var cellInfo = vts.TitleProgramChainTable[k].Pgc.CellPlayback[nCell];
                        bool isFirstAngle = (cellInfo.CellType == AngleBlockType.First && cellInfo.BlockType == BlockType.Angle);
                        bool isMiddleAngle = (cellInfo.CellType == AngleBlockType.Middle && cellInfo.BlockType == BlockType.Angle);
                        bool isLastAngle = (cellInfo.CellType == AngleBlockType.Last && cellInfo.BlockType == BlockType.Angle);
                        bool isNormal = (cellInfo.CellType == AngleBlockType.Normal && cellInfo.BlockType == BlockType.Normal);

                        //			0101=First; 1001=Middle ;	1101=Last
                        if (isFirstAngle)
                            m_nAngles[k] = 1;
                        else if (isMiddleAngle)
                            m_nAngles[k]++;
                        else if (isLastAngle)
                        {
                            m_nAngles[k]++;
                            bEndAngle = true;
                        }
                    }
                }
            } else
            {
                throw new InvalidCastException("Unknown IFO type.");
            }

            // Menu PGCs
            if (m_iVTSM_PGCI == null)
                m_nLUs = 0;
            else
                m_nLUs = m_iVTSM_PGCI.nr_of_lus;

            MenuInfo.m_nPGCs = 0;
            if (m_nLUs > MAX_LU)
            {
                csAux = $"ERROR: Max LUs limit ({MAX_LU}) has been reached.";
                Util.MyErrorBox(csAux);
                throw new IOException(csAux);
            }

            for (nLU = 0; nLU < m_nLUs; nLU++)
            {
                m_iVTSM_LU[nLU] = m_iVTSM_PGCI.lu[nLU].pgcit;
                m_nPGCinLU[nLU] = m_iVTSM_LU[nLU].Count;
                m_nIniPGCinLU[nLU] = MenuInfo.m_nPGCs;

                for (j = 0; j < m_nPGCinLU[nLU]; j++)
                {
                    if ((MenuInfo.m_nPGCs + m_nPGCinLU[nLU]) > MAX_MPGC)
                    {
                        csAux = $"ERROR: Max MPGCs limit ({MAX_MPGC}) has been reached.";
                        Util.MyErrorBox(csAux);
                        throw new IOException(csAux);
                    }
                    nAbsPGC = j + MenuInfo.m_nPGCs;
                    m_nLU_MPGC[nAbsPGC] = nLU;
                    m_iMENU_PGC[nAbsPGC] = m_iVTSM_LU[nLU][j].Pgc;

                    MenuInfo.m_C_PBKT[nAbsPGC] = (m_iMENU_PGC[nAbsPGC].CellPlayback.Length == 0) ? null : m_iMENU_PGC[nAbsPGC].CellPlayback;
                    MenuInfo.m_C_POST[nAbsPGC] = (m_iMENU_PGC[nAbsPGC].CellPosition.Length == 0) ? null : m_iMENU_PGC[nAbsPGC].CellPosition;

                    MenuInfo.m_nCells[nAbsPGC] = m_iMENU_PGC[nAbsPGC].NumberOfCells;

                    if ((MenuInfo.m_C_PBKT[nAbsPGC] == null || MenuInfo.m_C_POST[nAbsPGC] == null) && MenuInfo.m_nCells[nAbsPGC] != 0)
                    // There is something wrong...
                    {
                        MenuInfo.m_nCells[nAbsPGC] = 0;
                        csAux = $"ERROR: There is something wrong in number of cells in LU {nLU:00}, Menu PGC {j:00}.";
                        Util.MyErrorBox(csAux);
                        throw new IOException(csAux);
                    }
                    m_dwMDuration[nAbsPGC] = m_iMENU_PGC[nAbsPGC].PlaybackTime;

                } // For PGCs
                MenuInfo.m_nPGCs += m_nPGCinLU[nLU];
            }


            ///////////// VTS_C_ADT  ///////////////////////
            if (m_iVTS_C_ADT == null) nTotADT = 0;
            else
            {
                nTotADT = m_iVTS_C_ADT.Count;
            }

            //Cells
            for (nADT = 0; nADT < nTotADT; nADT++)
            {
                VidADT = m_iVTS_C_ADT[nADT].VobID;
                CidADT = m_iVTS_C_ADT[nADT].CellID;

                iArraysize = TitleInfo.m_AADT_Cell_list.GetSize();
                for (k = 0, bAlready = false; k < iArraysize; k++)
                {
                    if (CidADT == TitleInfo.m_AADT_Cell_list[k].CID &&
                        VidADT == TitleInfo.m_AADT_Cell_list[k].VID)
                    {
                        bAlready = true;
                        kk = k;
                    }
                }
                if (!bAlready)
                {
                    myADT_Cell = new();
                    myADT_Cell.CID = CidADT;
                    myADT_Cell.VID = VidADT;
                    myADT_Cell.iSize = 0;
                    myADT_Cell.iIniSec = 0x7fffffff;
                    myADT_Cell.iEndSec = 0;
                    kk = InsertCell(myADT_Cell, DomainType.Titles);
                    //			m_AADT_Cell_list.SetAtGrow(iArraysize,myADT_Cell);
                    //			kk=iArraysize;
                }
                iIniSec = (int)m_iVTS_C_ADT[nADT].StartSector;
                iEndSec = (int)m_iVTS_C_ADT[nADT].LastSector;
                if (iIniSec < TitleInfo.m_AADT_Cell_list[kk].iIniSec) TitleInfo.m_AADT_Cell_list[kk].iIniSec = iIniSec;
                if (iEndSec > TitleInfo.m_AADT_Cell_list[kk].iEndSec) TitleInfo.m_AADT_Cell_list[kk].iEndSec = iEndSec;
                iSize = (iEndSec - iIniSec + 1);
                TitleInfo.m_AADT_Cell_list[kk].iSize += (iEndSec - iIniSec + 1);
            }

            ///////////// VTSM_C_ADT  ///////////////////////
            if (m_iVTSM_C_ADT == null) nTotADT = 0;
            else
            {
                nTotADT = m_iVTSM_C_ADT.Count;
            }

            // Cells
            for (nADT = 0; nADT < nTotADT; nADT++)
            {
                VidADT = m_iVTSM_C_ADT[nADT].VobID;
                CidADT = m_iVTSM_C_ADT[nADT].CellID;

                iArraysize = MenuInfo.m_AADT_Cell_list.GetSize();
                for (k = 0, bAlready = false; k < iArraysize; k++)
                {
                    if (CidADT == MenuInfo.m_AADT_Cell_list[k].CID &&
                        VidADT == MenuInfo.m_AADT_Cell_list[k].VID)
                    {
                        bAlready = true;
                        kk = k;
                    }
                }
                if (!bAlready)
                {
                    myADT_Cell = new();
                    myADT_Cell.CID = CidADT;
                    myADT_Cell.VID = VidADT;
                    myADT_Cell.iSize = 0;
                    myADT_Cell.iIniSec = 0x7fffffff;
                    myADT_Cell.iEndSec = 0;
                    kk = InsertCell(myADT_Cell, DomainType.Menus);
                    //			m_MADT_Cell_list.SetAtGrow(iArraysize,myADT_Cell);
                    //			kk=iArraysize;
                }
                iIniSec = (int)m_iVTSM_C_ADT[nADT].StartSector;
                iEndSec = (int)m_iVTSM_C_ADT[nADT].LastSector;
                if (iIniSec < MenuInfo.m_AADT_Cell_list[kk].iIniSec) MenuInfo.m_AADT_Cell_list[kk].iIniSec = iIniSec;
                if (iEndSec > MenuInfo.m_AADT_Cell_list[kk].iEndSec) MenuInfo.m_AADT_Cell_list[kk].iEndSec = iEndSec;
                iSize = (iEndSec - iIniSec + 1);
                MenuInfo.m_AADT_Cell_list[kk].iSize += (iEndSec - iIniSec + 1);
            }

            FillDurations(ifo);

            //////////////////////////////////////////////////////////////	
            /////////////   VIDs
            // VIDs in Titles
            iArraysize = TitleInfo.m_AADT_Cell_list.GetSize();
            for (i = 0; i < iArraysize; i++)
            {
                VidADT = TitleInfo.m_AADT_Cell_list[i].VID;

                nVIDs = TitleInfo.m_AADT_Vid_list.GetSize();
                for (k = 0, bAlready = false; k < nVIDs; k++)
                {
                    if (VidADT == TitleInfo.m_AADT_Vid_list[k].VID)
                    {
                        bAlready = true;
                        kk = k;
                    }
                }
                if (!bAlready)
                {
                    myADT_Vid = new();
                    myADT_Vid.VID = VidADT;
                    myADT_Vid.iSize = 0;
                    myADT_Vid.nCells = 0;
                    myADT_Vid.dwDuration = TimeSpan.Zero;
                    TitleInfo.m_AADT_Vid_list.SetAtGrow(nVIDs, myADT_Vid);
                    kk = nVIDs;
                }
                TitleInfo.m_AADT_Vid_list[kk].iSize += TitleInfo.m_AADT_Cell_list[i].iSize;
                TitleInfo.m_AADT_Vid_list[kk].nCells++;
                TitleInfo.m_AADT_Vid_list[kk].dwDuration = TitleInfo.m_AADT_Cell_list[i].dwDuration + TitleInfo.m_AADT_Vid_list[kk].dwDuration;
            }

            // VIDs in Menus
            iArraysize = MenuInfo.m_AADT_Cell_list.GetSize();
            for (i = 0; i < iArraysize; i++)
            {
                VidADT = MenuInfo.m_AADT_Cell_list[i].VID;

                nVIDs = MenuInfo.m_AADT_Vid_list.GetSize();
                for (k = 0, bAlready = false; k < nVIDs; k++)
                {
                    if (VidADT == MenuInfo.m_AADT_Vid_list[k].VID)
                    {
                        bAlready = true;
                        kk = k;
                    }
                }
                if (!bAlready)
                {
                    myADT_Vid = new();
                    myADT_Vid.VID = VidADT;
                    myADT_Vid.iSize = 0;
                    myADT_Vid.nCells = 0;
                    myADT_Vid.dwDuration = TimeSpan.Zero;
                    MenuInfo.m_AADT_Vid_list.SetAtGrow(nVIDs, myADT_Vid);
                    kk = nVIDs;
                }
                MenuInfo.m_AADT_Vid_list[kk].iSize += MenuInfo.m_AADT_Cell_list[i].iSize;
                MenuInfo.m_AADT_Vid_list[kk].nCells++;
                MenuInfo.m_AADT_Vid_list[kk].dwDuration = MenuInfo.m_AADT_Cell_list[i].dwDuration + MenuInfo.m_AADT_Vid_list[kk].dwDuration;
            }

            // Fill VOB file size
            if (m_bVMGM)
            {
                m_nVobFiles = 0;

                for (k = 0; k < 10; k++)
                    m_i64VOBSize[k] = 0;

                // TODO get name without extension
                csAux2 = options.m_csInputIFO[..^3];
                csAux = csAux2 + "VOB";

                CFILE? file = CFILE.OpenRead(reader, csAux);
                if (file != null)
                {
                    m_i64VOBSize[0] = file.Size;
                    file.fclose();
                }
            }
            else
            {
                for (k = 0; k < 10; k++)
                {
                    csAux2 = options.m_csInputIFO[..^5];
                    csAux = $"{k}.VOB";
                    csAux = csAux2 + csAux;

                    CFILE? file = CFILE.OpenRead(reader, csAux);
                    if (file != null)
                    {
                        m_i64VOBSize[k] = file.Size;
                        m_nVobFiles = k;
                        file.fclose();
                    }
                    else
                        m_i64VOBSize[k] = 0;
                }
            }
        }

        public int InsertCell(ADT_CELL_LIST myADT_Cell, DomainType iDomain)
        {
            int iArraysize, i, ii = 0;
            bool bIsHigher;

            if (iDomain == DomainType.Titles)
            {
                iArraysize = TitleInfo.m_AADT_Cell_list.GetSize();
                ii = iArraysize;
                for (i = 0, bIsHigher = true; i < iArraysize && bIsHigher; i++)
                {
                    if (myADT_Cell.VID < TitleInfo.m_AADT_Cell_list[i].VID) { ii = i; bIsHigher = false; }
                    else if (myADT_Cell.VID > TitleInfo.m_AADT_Cell_list[i].VID) bIsHigher = true;
                    else
                    {
                        if (myADT_Cell.CID < TitleInfo.m_AADT_Cell_list[i].CID) { ii = i; bIsHigher = false; }
                        else if (myADT_Cell.CID > TitleInfo.m_AADT_Cell_list[i].CID) bIsHigher = true;
                    }

                }
                TitleInfo.m_AADT_Cell_list.InsertAt(ii, myADT_Cell);
            }
            if (iDomain == DomainType.Menus)
            {
                iArraysize = MenuInfo.m_AADT_Cell_list.GetSize();
                ii = iArraysize;
                for (i = 0, bIsHigher = true; i < iArraysize && bIsHigher; i++)
                {
                    if (myADT_Cell.VID < MenuInfo.m_AADT_Cell_list[i].VID) { ii = i; bIsHigher = false; }
                    else if (myADT_Cell.VID > MenuInfo.m_AADT_Cell_list[i].VID) bIsHigher = true;
                    else
                    {
                        if (myADT_Cell.CID < MenuInfo.m_AADT_Cell_list[i].CID) { ii = i; bIsHigher = false; }
                        else if (myADT_Cell.CID > MenuInfo.m_AADT_Cell_list[i].CID) bIsHigher = true;
                    }

                }
                //		if (i>0 && bIsHigher) i--;
                MenuInfo.m_AADT_Cell_list.InsertAt(ii, myADT_Cell);
            }
            return ii;
        }

        public virtual void FillDurations(IfoBase ifo)
        {
            int iArraysize;
            int i, j, k;
            int VIDa, CIDa, VIDb, CIDb;
            bool bFound;
            video_attr_t iVideoAttr;
            int iFormat;


            iArraysize = TitleInfo.m_AADT_Cell_list.GetSize();

            for (i = 0; i < iArraysize; i++)
            {
                VIDb = TitleInfo.m_AADT_Cell_list[i].VID;
                CIDb = TitleInfo.m_AADT_Cell_list[i].CID;
                for (j = 0, bFound = false; j < TitleInfo.m_nPGCs && !bFound; j++)
                {
                    for (k = 0; k < TitleInfo.m_nCells[j]; k++)
                    {
                        VIDa = TitleInfo.m_C_POST[j][k].VobID;
                        CIDa = TitleInfo.m_C_POST[j][k].CellID;
                        if (VIDa == VIDb && CIDa == CIDb)
                        {
                            bFound = true;
                            TitleInfo.m_AADT_Cell_list[i].dwDuration = TitleInfo.m_C_PBKT[j][k].PlaybackTime;
                        }
                    }
                }
                if (!bFound)
                {
                    if (ifo is VtsIfo vts)
                    {
                        iVideoAttr = vts.TitlesVobVideoAttributes;
                        iFormat = iVideoAttr.video_format;
                        /*if (iFormat == 0) // NTSC
                            TitleInfo.m_AADT_Cell_list[i].dwDuration = 0xC0;
                        else // PAL
                            TitleInfo.m_AADT_Cell_list[i].dwDuration = 0x40;*/
                        TitleInfo.m_AADT_Cell_list[i].dwDuration = TimeSpan.Zero;
                    } else
                    {
                        //TitleInfo.m_AADT_Cell_list[i].dwDuration = 0xC0; // Default to NTSC
                        TitleInfo.m_AADT_Cell_list[i].dwDuration = TimeSpan.Zero;
                    }
                }
            }

            iArraysize = MenuInfo.m_AADT_Cell_list.GetSize();

            for (i = 0; i < iArraysize; i++)
            {
                VIDb = MenuInfo.m_AADT_Cell_list[i].VID;
                CIDb = MenuInfo.m_AADT_Cell_list[i].CID;
                for (j = 0, bFound = false; j < MenuInfo.m_nPGCs && !bFound; j++)
                {
                    for (k = 0; k < MenuInfo.m_nCells[j]; k++)
                    {
                        VIDa = MenuInfo.m_C_POST[j][k].VobID;
                        CIDa = MenuInfo.m_C_POST[j][k].CellID;
                        if (VIDa == VIDb && CIDa == CIDb)
                        {
                            bFound = true;
                            MenuInfo.m_AADT_Cell_list[i].dwDuration = MenuInfo.m_C_PBKT[j][k].PlaybackTime;
                        }
                    }
                }
                if (!bFound)
                {
                    iVideoAttr = ifo.MenuVobVideoAttributes;
                    iFormat = iVideoAttr.video_format;
                    /*if (iFormat == 0) // NTSC
                        MenuInfo.m_AADT_Cell_list[i].dwDuration = 0xC0;
                    else // PAL
                        MenuInfo.m_AADT_Cell_list[i].dwDuration = 0x40;*/
                    MenuInfo.m_AADT_Cell_list[i].dwDuration = TimeSpan.Zero;
                }
            }
        }

    }
}
