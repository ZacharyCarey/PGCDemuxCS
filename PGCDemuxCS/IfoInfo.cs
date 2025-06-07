using PgcDemuxCS;
using PGCDemuxCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgcDemuxCS
{
    /// <summary>
    /// Reads required information about the specified IFO
    /// </summary>
    internal class IfoInfo
    {
        internal const int MAXLENGTH = 20 * 1024 * 1024;
        internal const int MAX_PGC = 32768;
        internal const int MAX_MPGC = 32768;
        internal const int MAX_LU = 100;

        /// <summary>
        /// IFO file loaded into byte buffer
        /// </summary>
        public Ref<byte> m_pIFO = Ref<byte>.Null;

        /// <summary>
        /// Length of the loadedd IFO file.
        /// See <see cref="m_pIFO"/>
        /// </summary>
        public int m_iIFOlen;

        // Compiled list of menu and title cells
        public CArray<ADT_CELL_LIST> m_AADT_Cell_list = new();
        public CArray<ADT_CELL_LIST> m_MADT_Cell_list = new();
        public CArray<ADT_VID_LIST> m_AADT_Vid_list = new();
        public CArray<ADT_VID_LIST> m_MADT_Vid_list = new();

        /// <summary>
        /// True indicates the VMGM file is selected (VIDEO_TS.IFO)
        /// </summary>
        public bool m_bVMGM;

        public int m_iVTS_PTT_SRPT, m_iVTS_PGCI, m_iVTS_C_ADT;
        public int m_iVTS_VOBU_ADMAP, m_iVTS_TMAPTI;
        public int m_iVTSM_PGCI, m_iVTSM_C_ADT, m_iVTSM_VOBU_ADMAP;

        public readonly int[] m_iVTS_PGC = new int[MAX_PGC];
        public readonly int[] m_C_PBKT = new int[MAX_PGC];
        public readonly int[] m_C_POST = new int[MAX_PGC];
        public readonly int[] m_nCells = new int[MAX_PGC];
        public readonly int[] m_nAngles = new int[MAX_PGC];

        public readonly ulong[] m_dwDuration = new ulong[MAX_PGC];
        public readonly ulong[] m_dwMDuration = new ulong[MAX_MPGC];

        public readonly int[] m_iVTSM_LU = new int[MAX_LU];
        public readonly int[] m_nIniPGCinLU = new int[MAX_LU];
        public readonly int[] m_nPGCinLU = new int[MAX_LU];
        public readonly int[] m_iMENU_PGC = new int[MAX_MPGC];
        public readonly int[] m_M_C_PBKT = new int[MAX_MPGC];
        public readonly int[] m_M_C_POST = new int[MAX_MPGC];
        public readonly int[] m_nMCells = new int[MAX_MPGC];
        public readonly int[] m_nLU_MPGC = new int[MAX_MPGC];

        /// <summary>
        /// Number of PGC's found in the file
        /// </summary>
        public int m_nPGCs;

        public int m_nLUs, m_nMPGCs;
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
            CFILE inFile = null;
            int iIniSec, iEndSec;
            FileInfo statbuf;
            int iSize, iCat;
            int iIFOSize = 0;

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

            inFile = CFILE.OpenRead(reader, ifoName);
            if (inFile == null)
            {
                csAux = $"Unable to open {ifoName}";
                Util.MyErrorBox(csAux);
                throw new IOException(csAux);
            }

            iIFOSize = (int)inFile.Size;

            if (iIFOSize > MAXLENGTH)
            {
                csAux = $"IFO too big {ifoName}";
                Util.MyErrorBox(csAux);
                inFile.fclose();
                throw new IOException(csAux);
            }

            //if (m_pIFO!=null)  delete[]  m_pIFO;

            m_pIFO = new ByteArrayRef(new byte[iIFOSize + 2048], 0);

            // Read IFO
            for (i = 0; !inFile.feof() && i < MAXLENGTH; i++)
                m_pIFO[i] = (byte)inFile.fgetc();
            m_iIFOlen = i - 1;
            inFile.fclose();

            m_AADT_Cell_list.RemoveAll();
            m_MADT_Cell_list.RemoveAll();
            m_AADT_Vid_list.RemoveAll();
            m_MADT_Vid_list.RemoveAll();


            // Get Title Cells
            if (m_bVMGM)
            {
                m_iVTS_PTT_SRPT = 0;
                m_iVTS_PGCI = 0;
                m_iVTSM_PGCI = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xC8));
                m_iVTS_TMAPTI = 0;
                m_iVTSM_C_ADT = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xD8));
                m_iVTSM_VOBU_ADMAP = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xDC));
                m_iVTS_C_ADT = 0;
                m_iVTS_VOBU_ADMAP = 0;
            }
            else
            {
                m_iVTS_PTT_SRPT = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xC8));
                m_iVTS_PGCI = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xCC));
                m_iVTSM_PGCI = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xD0));
                m_iVTS_TMAPTI = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xD4));
                m_iVTSM_C_ADT = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xD8));
                m_iVTSM_VOBU_ADMAP = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xDC));
                m_iVTS_C_ADT = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xE0));
                m_iVTS_VOBU_ADMAP = 2048 * Util.GetNbytes(4, m_pIFO.AtIndex(0xE4));
            }
            if (m_bVMGM)
                m_nPGCs = 0;
            else
                m_nPGCs = Util.GetNbytes(2, m_pIFO.AtIndex(m_iVTS_PGCI));


            // Title PGCs	
            if (m_nPGCs > MAX_PGC)
            {
                csAux = $"ERROR: Max PGCs limit ({MAX_PGC}) has been reached.";
                Util.MyErrorBox(csAux);
                throw new IOException(csAux);
            }
            for (k = 0; k < m_nPGCs; k++)
            {
                m_iVTS_PGC[k] = Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTS_PGCI + 0x04 + (k + 1) * 8)) + m_iVTS_PGCI;
                m_dwDuration[k] = (uint)Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTS_PGC[k] + 4));

                m_C_PBKT[k] = Util.GetNbytes(2, m_pIFO.AtIndex(m_iVTS_PGC[k] + 0xE8));
                if (m_C_PBKT[k] != 0) m_C_PBKT[k] += m_iVTS_PGC[k];

                m_C_POST[k] = Util.GetNbytes(2, m_pIFO.AtIndex(m_iVTS_PGC[k] + 0xEA));
                if (m_C_POST[k] != 0) m_C_POST[k] += m_iVTS_PGC[k];

                m_nCells[k] = m_pIFO[m_iVTS_PGC[k] + 3];


                m_nAngles[k] = 1;

                for (nCell = 0, bEndAngle = false; nCell < m_nCells[k] && bEndAngle == false; nCell++)
                {
                    iCat = Util.GetNbytes(1, m_pIFO.AtIndex(m_C_PBKT[k] + 24 * nCell));
                    iCat = iCat & 0xF0;
                    //			0101=First; 1001=Middle ;	1101=Last
                    if (iCat == 0x50)
                        m_nAngles[k] = 1;
                    else if (iCat == 0x90)
                        m_nAngles[k]++;
                    else if (iCat == 0xD0)
                    {
                        m_nAngles[k]++;
                        bEndAngle = true;
                    }
                }
            }


            // Menu PGCs
            if (m_iVTSM_PGCI == 0)
                m_nLUs = 0;
            else
                m_nLUs = Util.GetNbytes(2, m_pIFO.AtIndex(m_iVTSM_PGCI));

            m_nMPGCs = 0;
            if (m_nLUs > MAX_LU)
            {
                csAux = $"ERROR: Max LUs limit ({MAX_LU}) has been reached.";
                Util.MyErrorBox(csAux);
                throw new IOException(csAux);
            }

            for (nLU = 0; nLU < m_nLUs; nLU++)
            {
                m_iVTSM_LU[nLU] = Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTSM_PGCI + 0x04 + (nLU + 1) * 8)) + m_iVTSM_PGCI;
                m_nPGCinLU[nLU] = Util.GetNbytes(2, m_pIFO.AtIndex(m_iVTSM_LU[nLU]));
                m_nIniPGCinLU[nLU] = m_nMPGCs;

                for (j = 0; j < m_nPGCinLU[nLU]; j++)
                {
                    if ((m_nMPGCs + m_nPGCinLU[nLU]) > MAX_MPGC)
                    {
                        csAux = $"ERROR: Max MPGCs limit ({MAX_MPGC}) has been reached.";
                        Util.MyErrorBox(csAux);
                        throw new IOException(csAux);
                    }
                    nAbsPGC = j + m_nMPGCs;
                    m_nLU_MPGC[nAbsPGC] = nLU;
                    m_iMENU_PGC[nAbsPGC] = Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTSM_LU[nLU] + 0x04 + (j + 1) * 8)) + m_iVTSM_LU[nLU];

                    m_M_C_PBKT[nAbsPGC] = Util.GetNbytes(2, m_pIFO.AtIndex(m_iMENU_PGC[nAbsPGC] + 0xE8));
                    if (m_M_C_PBKT[nAbsPGC] != 0) m_M_C_PBKT[nAbsPGC] += m_iMENU_PGC[nAbsPGC];
                    m_M_C_POST[nAbsPGC] = Util.GetNbytes(2, m_pIFO.AtIndex(m_iMENU_PGC[nAbsPGC] + 0xEA));
                    if (m_M_C_POST[nAbsPGC] != 0) m_M_C_POST[nAbsPGC] += m_iMENU_PGC[nAbsPGC];

                    m_nMCells[nAbsPGC] = m_pIFO[m_iMENU_PGC[nAbsPGC] + 3];

                    if ((m_M_C_PBKT[nAbsPGC] == 0 || m_M_C_POST[nAbsPGC] == 0) && m_nMCells[nAbsPGC] != 0)
                    // There is something wrong...
                    {
                        m_nMCells[nAbsPGC] = 0;
                        csAux = $"ERROR: There is something wrong in number of cells in LU {nLU:00}, Menu PGC {j:00}.";
                        Util.MyErrorBox(csAux);
                        throw new IOException(csAux);
                    }
                    m_dwMDuration[nAbsPGC] = (uint)Util.GetNbytes(4, m_pIFO.AtIndex(m_iMENU_PGC[nAbsPGC] + 4));

                } // For PGCs
                m_nMPGCs += m_nPGCinLU[nLU];
            }


            ///////////// VTS_C_ADT  ///////////////////////
            if (m_iVTS_C_ADT == 0) nTotADT = 0;
            else
            {
                nTotADT = Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTS_C_ADT + 4));
                nTotADT = (nTotADT - 7) / 12;
            }

            //Cells
            for (nADT = 0; nADT < nTotADT; nADT++)
            {
                VidADT = Util.GetNbytes(2, m_pIFO.AtIndex(m_iVTS_C_ADT + 8 + 12 * nADT));
                CidADT = m_pIFO[m_iVTS_C_ADT + 8 + 12 * nADT + 2];

                iArraysize = m_AADT_Cell_list.GetSize();
                for (k = 0, bAlready = false; k < iArraysize; k++)
                {
                    if (CidADT == m_AADT_Cell_list[k].CID &&
                        VidADT == m_AADT_Cell_list[k].VID)
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
                iIniSec = Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTS_C_ADT + 8 + 12 * nADT + 4));
                iEndSec = Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTS_C_ADT + 8 + 12 * nADT + 8));
                if (iIniSec < m_AADT_Cell_list[kk].iIniSec) m_AADT_Cell_list[kk].iIniSec = iIniSec;
                if (iEndSec > m_AADT_Cell_list[kk].iEndSec) m_AADT_Cell_list[kk].iEndSec = iEndSec;
                iSize = (iEndSec - iIniSec + 1);
                m_AADT_Cell_list[kk].iSize += (iEndSec - iIniSec + 1);
            }

            ///////////// VTSM_C_ADT  ///////////////////////
            if (m_iVTSM_C_ADT == 0) nTotADT = 0;
            else
            {
                nTotADT = Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTSM_C_ADT + 4));
                nTotADT = (nTotADT - 7) / 12;
            }

            // Cells
            for (nADT = 0; nADT < nTotADT; nADT++)
            {
                VidADT = Util.GetNbytes(2, m_pIFO.AtIndex(m_iVTSM_C_ADT + 8 + 12 * nADT));
                CidADT = m_pIFO[m_iVTSM_C_ADT + 8 + 12 * nADT + 2];

                iArraysize = m_MADT_Cell_list.GetSize();
                for (k = 0, bAlready = false; k < iArraysize; k++)
                {
                    if (CidADT == m_MADT_Cell_list[k].CID &&
                        VidADT == m_MADT_Cell_list[k].VID)
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
                iIniSec = Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTSM_C_ADT + 8 + 12 * nADT + 4));
                iEndSec = Util.GetNbytes(4, m_pIFO.AtIndex(m_iVTSM_C_ADT + 8 + 12 * nADT + 8));
                if (iIniSec < m_MADT_Cell_list[kk].iIniSec) m_MADT_Cell_list[kk].iIniSec = iIniSec;
                if (iEndSec > m_MADT_Cell_list[kk].iEndSec) m_MADT_Cell_list[kk].iEndSec = iEndSec;
                iSize = (iEndSec - iIniSec + 1);
                m_MADT_Cell_list[kk].iSize += (iEndSec - iIniSec + 1);
            }

            FillDurations();

            //////////////////////////////////////////////////////////////	
            /////////////   VIDs
            // VIDs in Titles
            iArraysize = m_AADT_Cell_list.GetSize();
            for (i = 0; i < iArraysize; i++)
            {
                VidADT = m_AADT_Cell_list[i].VID;

                nVIDs = m_AADT_Vid_list.GetSize();
                for (k = 0, bAlready = false; k < nVIDs; k++)
                {
                    if (VidADT == m_AADT_Vid_list[k].VID)
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
                    myADT_Vid.dwDuration = 0;
                    m_AADT_Vid_list.SetAtGrow(nVIDs, myADT_Vid);
                    kk = nVIDs;
                }
                m_AADT_Vid_list[kk].iSize += m_AADT_Cell_list[i].iSize;
                m_AADT_Vid_list[kk].nCells++;
                m_AADT_Vid_list[kk].dwDuration = Util.AddDuration(m_AADT_Cell_list[i].dwDuration, m_AADT_Vid_list[kk].dwDuration);
            }

            // VIDs in Menus
            iArraysize = m_MADT_Cell_list.GetSize();
            for (i = 0; i < iArraysize; i++)
            {
                VidADT = m_MADT_Cell_list[i].VID;

                nVIDs = m_MADT_Vid_list.GetSize();
                for (k = 0, bAlready = false; k < nVIDs; k++)
                {
                    if (VidADT == m_MADT_Vid_list[k].VID)
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
                    myADT_Vid.dwDuration = 0;
                    m_MADT_Vid_list.SetAtGrow(nVIDs, myADT_Vid);
                    kk = nVIDs;
                }
                m_MADT_Vid_list[kk].iSize += m_MADT_Cell_list[i].iSize;
                m_MADT_Vid_list[kk].nCells++;
                m_MADT_Vid_list[kk].dwDuration = Util.AddDuration(m_MADT_Cell_list[i].dwDuration, m_MADT_Vid_list[kk].dwDuration);
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
                iArraysize = m_AADT_Cell_list.GetSize();
                ii = iArraysize;
                for (i = 0, bIsHigher = true; i < iArraysize && bIsHigher; i++)
                {
                    if (myADT_Cell.VID < m_AADT_Cell_list[i].VID) { ii = i; bIsHigher = false; }
                    else if (myADT_Cell.VID > m_AADT_Cell_list[i].VID) bIsHigher = true;
                    else
                    {
                        if (myADT_Cell.CID < m_AADT_Cell_list[i].CID) { ii = i; bIsHigher = false; }
                        else if (myADT_Cell.CID > m_AADT_Cell_list[i].CID) bIsHigher = true;
                    }

                }
                m_AADT_Cell_list.InsertAt(ii, myADT_Cell);
            }
            if (iDomain == DomainType.Menus)
            {
                iArraysize = m_MADT_Cell_list.GetSize();
                ii = iArraysize;
                for (i = 0, bIsHigher = true; i < iArraysize && bIsHigher; i++)
                {
                    if (myADT_Cell.VID < m_MADT_Cell_list[i].VID) { ii = i; bIsHigher = false; }
                    else if (myADT_Cell.VID > m_MADT_Cell_list[i].VID) bIsHigher = true;
                    else
                    {
                        if (myADT_Cell.CID < m_MADT_Cell_list[i].CID) { ii = i; bIsHigher = false; }
                        else if (myADT_Cell.CID > m_MADT_Cell_list[i].CID) bIsHigher = true;
                    }

                }
                //		if (i>0 && bIsHigher) i--;
                m_MADT_Cell_list.InsertAt(ii, myADT_Cell);
            }
            return ii;
        }

        public virtual void FillDurations()
        {
            int iArraysize;
            int i, j, k;
            int VIDa, CIDa, VIDb, CIDb;
            bool bFound;
            int iVideoAttr, iFormat;


            iArraysize = m_AADT_Cell_list.GetSize();

            for (i = 0; i < iArraysize; i++)
            {
                VIDb = m_AADT_Cell_list[i].VID;
                CIDb = m_AADT_Cell_list[i].CID;
                for (j = 0, bFound = false; j < m_nPGCs && !bFound; j++)
                {
                    for (k = 0; k < m_nCells[j]; k++)
                    {
                        VIDa = Util.GetNbytes(2, m_pIFO.AtIndex(m_C_POST[j] + k * 4));
                        CIDa = m_pIFO[m_C_POST[j] + k * 4 + 3];
                        if (VIDa == VIDb && CIDa == CIDb)
                        {
                            bFound = true;
                            m_AADT_Cell_list[i].dwDuration = (ulong)Util.GetNbytes(4, m_pIFO.AtIndex(m_C_PBKT[j] + 0x18 * k + 4));
                        }
                    }
                }
                if (!bFound)
                {
                    iVideoAttr = m_pIFO[0x200] * 256 + m_pIFO[0x201];
                    iFormat = (iVideoAttr & 0x1000) >> 12;
                    if (iFormat == 0) // NTSC
                        m_AADT_Cell_list[i].dwDuration = 0xC0;
                    else // PAL
                        m_AADT_Cell_list[i].dwDuration = 0x40;
                }
            }

            iArraysize = m_MADT_Cell_list.GetSize();

            for (i = 0; i < iArraysize; i++)
            {
                VIDb = m_MADT_Cell_list[i].VID;
                CIDb = m_MADT_Cell_list[i].CID;
                for (j = 0, bFound = false; j < m_nMPGCs && !bFound; j++)
                {
                    for (k = 0; k < m_nMCells[j]; k++)
                    {
                        VIDa = Util.GetNbytes(2, m_pIFO.AtIndex(m_M_C_POST[j] + k * 4));
                        CIDa = m_pIFO[m_M_C_POST[j] + k * 4 + 3];
                        if (VIDa == VIDb && CIDa == CIDb)
                        {
                            bFound = true;
                            m_MADT_Cell_list[i].dwDuration = (ulong)Util.GetNbytes(4, m_pIFO.AtIndex(m_M_C_PBKT[j] + 0x18 * k + 4));
                        }
                    }
                }
                if (!bFound)
                {
                    iVideoAttr = m_pIFO[0x100] * 256 + m_pIFO[0x101];
                    iFormat = (iVideoAttr & 0x1000) >> 12;
                    if (iFormat == 0) // NTSC
                        m_MADT_Cell_list[i].dwDuration = 0xC0;
                    else // PAL
                        m_MADT_Cell_list[i].dwDuration = 0x40;
                }
            }
        }

    }
}
