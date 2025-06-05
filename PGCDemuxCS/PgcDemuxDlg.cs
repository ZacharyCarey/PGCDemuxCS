
public class CPgcDemuxDlg
{
    public CPgcDemuxDlg()
    {
        m_bCheckAud = true;
        m_bCheckSub = true;
        m_bCheckVid = false;
        m_bCheckVob = false;
        m_bCheckLog = true;
        m_bCheckCellt = true;
        m_iRadioT = DomainType.Titles;
        m_csEditInput = "";
        m_csEditOutput = "";
        m_iRadioMode = 0;
        m_bCheckVob2 = false;
        m_bCheckEndTime = false;
    }

    public List<string> m_ComboAng;
    public uint m_CProgress; // progress out of 100
    public List<string> m_ComboPgc;
    public bool m_bCheckAud = true;
    public bool m_bCheckSub = true;
    public bool m_bCheckVid = false;
    public bool m_bCheckVob = false;
    public bool m_bCheckLog = true;
    public bool m_bCheckCellt = true;
    public DomainType m_iRadioT = DomainType.Titles;

    /// <summary>
    /// "Input IFO" text box. Path to the IFO file being read.
    /// </summary>
    public CString m_csEditInput;

    /// <summary>
    /// "Output Folder" text box. Path to directory where 
    /// files will be saved.
    /// </summary>
    public CString m_csEditOutput;
    public CString m_csEditCells;
    public ModeType m_iRadioMode = ModeType.PGC;
    public bool m_bCheckVob2 = false;
    public bool m_bCheckEndTime = false;

    protected virtual void ReadInputFile()
    {
        CString csAux, csAux1, csAux2;

        this.m_csEditInput.MakeUpper();
        csAux = m_csEditInput.Right(12);

        csAux1 = csAux.Left(4);
        csAux = m_csEditInput.Right(6);
        csAux2 = m_csEditInput.Right(12);
        if ((csAux != "_0.IFO" || csAux1 != "VTS_") && csAux2 != "VIDEO_TS.IFO")
        {
            Util.MyErrorBox("Invalid input file!");
            m_csEditInput = "";
            return;
        }
        if (csAux2 == "VIDEO_TS.IFO")
        {
            PgcDemuxApp.theApp.m_bVMGM = true;
            m_iRadioT = DomainType.Menus;
        }
        else
            PgcDemuxApp.theApp.m_bVMGM = false;

        PgcDemuxApp.theApp.m_csInputIFO = m_csEditInput;
        PgcDemuxApp.theApp.m_csInputPath = m_csEditInput.Left(m_csEditInput.ReverseFind('\\'));
        //UpdateData (FALSE);
        if (PgcDemuxApp.theApp.ReadIFO() == 0)
        {
            FillComboPgc();
            OnSelchangeCombopgc();
        }
        else
        {
            Util.MyErrorBox("Error reading IFO!");
            m_csEditInput = "";
        }
    }

    // "OK" Button. Starts processing based on settings
    public void OnOK()
    {
        // TODO: Add extra validation here
        CString csOutput, csInput, csAux;
        int nSelPGC, nSelAng;
        int iRet = 0;

        if (m_csEditOutput.Right(1) == "\\")
        {
            m_csEditOutput = m_csEditOutput.Left(m_csEditOutput.GetLength() - 1);
        }

        PgcDemuxApp.theApp.m_csOutputPath = m_csEditOutput;

        csOutput = PgcDemuxApp.theApp.m_csOutputPath;
        csInput = PgcDemuxApp.theApp.m_csInputPath;
        csOutput.MakeUpper();
        csInput.MakeUpper();

        if (FilesAlreadyExist())
        {
            Util.MyErrorBox("There are some files in destination folder\nwhich could be overwritten.\n Do you want to continue?");
            //if (iRet != IDYES) return;
        }

        if (m_csEditInput.IsEmpty())
        {
            Util.MyErrorBox("Fill input file!");
            return;
        }

        if (csOutput.IsEmpty())
        {
            Util.MyErrorBox("Fill outout folder!");
            return;
        }
        if (csOutput == csInput)
        {
            Util.MyErrorBox("Output Folder must be different from input one!");
            return;
        }

        if (!m_bCheckAud && !m_bCheckVid && !m_bCheckVob && !m_bCheckSub && !m_bCheckLog && !m_bCheckCellt)
        {
            Util.MyErrorBox("Check at least one checkbox!");
            return;
        }
        nSelPGC = 0; //m_ComboPgc.GetCurSel();
        nSelAng = 0; //m_ComboAng.GetCurSel();

        if (m_iRadioMode == ModeType.PGC)
        {
            if ((m_iRadioT == DomainType.Titles && PgcDemuxApp.theApp.m_nCells[nSelPGC] == 0) ||
                (m_iRadioT == DomainType.Menus && PgcDemuxApp.theApp.m_nMCells[nSelPGC] == 0))
            {
                Util.MyErrorBox("Select a PGC with at least one cell");
                return;
            }
        }
        if (m_iRadioMode == ModeType.VID)
        {
            if ((m_iRadioT == DomainType.Titles && PgcDemuxApp.theApp.m_AADT_Cell_list.GetSize() == 0) ||
                (m_iRadioT == DomainType.Menus && PgcDemuxApp.theApp.m_MADT_Cell_list.GetSize() == 0))
            {
                Util.MyErrorBox("Select a VOB id with at least one cell");
                return;
            }
        }

        PgcDemuxApp.theApp.m_bCheckAud = m_bCheckAud;
        PgcDemuxApp.theApp.m_bCheckVid = m_bCheckVid;
        PgcDemuxApp.theApp.m_bCheckVob = m_bCheckVob;
        PgcDemuxApp.theApp.m_bCheckVob2 = m_bCheckVob2;
        PgcDemuxApp.theApp.m_bCheckSub = m_bCheckSub;
        PgcDemuxApp.theApp.m_bCheckLog = m_bCheckLog;
        PgcDemuxApp.theApp.m_bCheckCellt = m_bCheckCellt;
        PgcDemuxApp.theApp.m_bCheckEndTime = m_bCheckEndTime;
        PgcDemuxApp.theApp.m_iDomain = m_iRadioT;
        PgcDemuxApp.theApp.m_iMode = m_iRadioMode;

        PgcDemuxApp.theApp.m_bAbort = false;
        PgcDemuxApp.theApp.m_bInProcess = true;

        if (m_iRadioMode == ModeType.PGC)
        {
            if (m_iRadioT == DomainType.Titles)
                iRet = PgcDemuxApp.theApp.PgcDemux(nSelPGC, nSelAng, this);
            else
                iRet = PgcDemuxApp.theApp.PgcMDemux(nSelPGC, this);
        }
        else if (m_iRadioMode == ModeType.VID)
        {
            if (m_iRadioT == DomainType.Titles)
                iRet = PgcDemuxApp.theApp.VIDDemux(nSelPGC, this);
            else
                iRet = PgcDemuxApp.theApp.VIDMDemux(nSelPGC, this);
        }
        else if (m_iRadioMode == ModeType.CID)
        {
            if (m_iRadioT == DomainType.Titles)
                iRet = PgcDemuxApp.theApp.CIDDemux(nSelPGC, this);
            else
                iRet = PgcDemuxApp.theApp.CIDMDemux(nSelPGC, this);
        }


        PgcDemuxApp.theApp.m_bInProcess = false;

        if (PgcDemuxApp.theApp.m_bAbort)
            csAux = "Aborted";
        else
        {
            if (iRet == 0)
                csAux = "Finished OK.";
            else
                csAux = "Finished with error";
        }

        Util.MyInfoBox(csAux);
        UpdateProgress(0);

        PgcDemuxApp.theApp.m_bAbort = false;

        csAux = $"PgcDemux v{PgcDemuxApp.PGCDEMUX_VERSION}";
        //SetWindowText(csAux);
    }

    public void UpdateProgress(int nPercent)
    {
        if (nPercent < 0) nPercent = 0;
        if (nPercent > 100) nPercent = 100;
        // TODO display percent
    }

    protected void FillComboPgc()
    {
        int k, nLU, nPGC;
        string csAux;

        m_ComboPgc.Clear();

        if (m_iRadioMode == ModeType.PGC)
        {
            if (m_iRadioT == DomainType.Titles)
            {
                for (k = 0; k < PgcDemuxApp.theApp.m_nPGCs; k++)
                {
                    csAux = $"PGC # {(k + 1):00}--> ";
                    csAux += Util.FormatDuration(PgcDemuxApp.theApp.m_dwDuration[k]);
                    m_ComboPgc.Add(csAux);
                }
            }
            else
            {
                for (k = 0; k < PgcDemuxApp.theApp.m_nMPGCs; k++)
                {
                    nLU = PgcDemuxApp.theApp.m_nLU_MPGC[k];
                    nPGC = k - PgcDemuxApp.theApp.m_nIniPGCinLU[nLU];
                    csAux = $"LU {(nLU + 1):00} # {(nPGC + 1):00} --> ";
                    csAux += Util.FormatDuration(PgcDemuxApp.theApp.m_dwMDuration[k]);
                    m_ComboPgc.Add(csAux);
                }
            }
        }

        if (m_iRadioMode == ModeType.VID)
        {
            if (m_iRadioT == DomainType.Titles)
            {
                for (k = 0; k < PgcDemuxApp.theApp.m_AADT_Vid_list.GetSize(); k++)
                {
                    csAux = $"VID {(k + 1):00} ({PgcDemuxApp.theApp.m_AADT_Vid_list[k].VID:00})--> ";
                    csAux += Util.FormatDuration(PgcDemuxApp.theApp.m_AADT_Vid_list[k].dwDuration);
                    m_ComboPgc.Add(csAux);
                }
            }
            else
            {
                for (k = 0; k < PgcDemuxApp.theApp.m_MADT_Vid_list.GetSize(); k++)
                {
                    csAux = $"VID {(k + 1):00} ({PgcDemuxApp.theApp.m_MADT_Vid_list[k].VID:00})--> ";
                    csAux += Util.FormatDuration(PgcDemuxApp.theApp.m_MADT_Vid_list[k].dwDuration);
                    m_ComboPgc.Add(csAux);
                }
            }
        }

        if (m_iRadioMode == ModeType.CID)
        {
            if (m_iRadioT == DomainType.Titles)
            {
                for (k = 0; k < PgcDemuxApp.theApp.m_AADT_Cell_list.GetSize(); k++)
                {
                    csAux = $"{(k + 1):00} ({PgcDemuxApp.theApp.m_AADT_Cell_list[k].VID:00}/{PgcDemuxApp.theApp.m_AADT_Cell_list[k].CID:00})--> ";
                    csAux += Util.FormatDuration(PgcDemuxApp.theApp.m_AADT_Cell_list[k].dwDuration);
                    m_ComboPgc.Add(csAux);
                }
            }
            else
            {
                for (k = 0; k < PgcDemuxApp.theApp.m_MADT_Cell_list.GetSize(); k++)
                {
                    csAux = $"{(k + 1):00} ({PgcDemuxApp.theApp.m_MADT_Cell_list[k].VID:00}/{PgcDemuxApp.theApp.m_MADT_Cell_list[k].CID:00})--> ";
                    csAux += Util.FormatDuration(PgcDemuxApp.theApp.m_MADT_Cell_list[k].dwDuration);
                    m_ComboPgc.Add(csAux);
                }
            }
        }

        //m_ComboPgc.SetCurSel(0);
    }

    protected void FillAnglePgc()
    {
        int k;
        string csAux;
        object pWnd;
        int nSelPGC;

        m_ComboAng.Clear();
        if (m_iRadioMode != ModeType.PGC) return;

        csAux = $"1";
        m_ComboAng.Add(csAux);

        nSelPGC = 0; //m_ComboPgc.GetCurSel();

        if (m_iRadioT == DomainType.Titles && nSelPGC >= 0 && PgcDemuxApp.theApp.m_nAngles[nSelPGC] > 1)
            for (k = 1; k < PgcDemuxApp.theApp.m_nAngles[nSelPGC]; k++)
            {
                csAux = $"{k + 1}";
                m_ComboAng.Add(csAux);
            }

        //m_ComboAng.SetCurSel(0);

        if (m_iRadioT == DomainType.Menus)
        {
            /*pWnd = GetDlgItem(IDC_COMBOANG);
            pWnd->ShowWindow(SW_HIDE);
            pWnd = GetDlgItem(IDC_STATICANG);
            pWnd->ShowWindow(SW_HIDE);*/
        }
        else
        {
            /*pWnd = GetDlgItem(IDC_STATICANG);
            pWnd->ShowWindow(SW_SHOW);
            pWnd = GetDlgItem(IDC_COMBOANG);
            pWnd->ShowWindow(SW_SHOW);

            if (m_ComboAng.GetCount() > 1)
                pWnd->EnableWindow(TRUE);
            else
                pWnd->EnableWindow(FALSE);*/
        }
    }

    // TODO onRadiopgc() and onRadiovob() and onRadiocell()
    // ShowControls()
    // FillComboPgc();
    // onSelchangeCombopgc()

    protected void OnSelchangeCombopgc()
    {
        // TODO: Add your control notification handler code here
        int nSelPGC;

        nSelPGC = 0; //m_ComboPgc.GetCurSel();

        if (m_iRadioMode == ModeType.PGC)
        {
            if (nSelPGC >= 0)
            {
                if (m_iRadioT == DomainType.Titles)
                    m_csEditCells = $"{PgcDemuxApp.theApp.m_nCells[nSelPGC]}";
                else
                    m_csEditCells = $"{PgcDemuxApp.theApp.m_nMCells[nSelPGC]}";
            }
            else
                m_csEditCells = "";
        }
        if (m_iRadioMode == ModeType.VID)
        {
            if (nSelPGC >= 0)
            {
                if (m_iRadioT == DomainType.Titles)
                    m_csEditCells = $"{PgcDemuxApp.theApp.m_AADT_Vid_list[nSelPGC].nCells}";
                else
                    m_csEditCells = $"{PgcDemuxApp.theApp.m_MADT_Vid_list[nSelPGC].nCells}";
            }
            else
                m_csEditCells = "";
        }
        if (m_iRadioMode == ModeType.CID)
            m_csEditCells = "";

        if (m_iRadioMode == ModeType.PGC)
            FillAnglePgc();
    }

    protected void OnButtondelay()
    {
        // TODO: Add your control notification handler code here
        int k, AudDelay, nSelPGC;
        int iRet;

        string csInfo, csAux;

        nSelPGC = 0;  //m_ComboPgc.GetCurSel();


        if (m_iRadioMode == ModeType.PGC)
        {
            if ((m_iRadioT == DomainType.Titles && PgcDemuxApp.theApp.m_nCells[nSelPGC] == 0) ||
                (m_iRadioT == DomainType.Menus && PgcDemuxApp.theApp.m_nMCells[nSelPGC] == 0))
            {
                Util.MyErrorBox("Select a PGC with at least one cell");
                return;
            }
        }
        if (m_iRadioMode == ModeType.VID)
        {
            if ((m_iRadioT == DomainType.Titles && PgcDemuxApp.theApp.m_AADT_Cell_list.GetSize() == 0) ||
                (m_iRadioT == DomainType.Menus && PgcDemuxApp.theApp.m_MADT_Cell_list.GetSize() == 0))
            {
                Util.MyErrorBox("Select a VOB id with at least one cell");
                return;
            }
        }

        //SetCursor(AfxGetApp()->LoadStandardCursor(IDC_WAIT));

        // Read PTSs from VOB
        if (m_iRadioT == DomainType.Titles)
            iRet = PgcDemuxApp.theApp.GetAudioDelay(m_iRadioMode, nSelPGC);
        else
            iRet = PgcDemuxApp.theApp.GetMAudioDelay(m_iRadioMode, nSelPGC);

        //SetCursor(AfxGetApp()->LoadStandardCursor(IDC_ARROW));

        if (iRet != 0) return;
        // Now calculate and write the delays
        csInfo = "";
        for (k = 0; k < 8; k++)
        {
            if (PgcDemuxApp.theApp.m_iFirstAudPTS[k] != 0)
            {
                //			AudDelay=theApp.m_iFirstAudPTS[k]-theApp.m_iFirstVidPTS;
                AudDelay = PgcDemuxApp.theApp.m_iFirstAudPTS[k] - PgcDemuxApp.theApp.m_iFirstNavPTS0;

                if (AudDelay < 0)
                    AudDelay -= 44;
                else
                    AudDelay += 44;
                AudDelay /= 90;
                csAux = $"Audio_{k + 1}: 0x{PgcDemuxApp.theApp.m_iAudIndex[k]:X2} --> {AudDelay} msecs. ";
            }
            else
                csAux = $"Audio_{k + 1}: None.";

            csInfo += csAux + "\r\n";

        }

        Util.MyInfoBox(csInfo);
    }

    protected bool FilesAlreadyExist()
    {
        bool bAudio, bSubs, bVideo, bVob;
        string csFile, csAux;
        int i;

        bAudio = bSubs = bVideo = bVob = false;

        csFile = m_csEditOutput + '\\' + "VideoFile.m2v";
        if (_access(csFile)) bVideo = true;


        for (i = 0; i < 8 && bAudio == false; i++)
        {
            // ac3
            csAux = $"AudioFile_{(i + 0x80):X2}.ac3";
            csFile = m_csEditOutput + '\\' + csAux;
            if (_access(csFile)) bAudio = true;
            //dts
            csAux = $"AudioFile_{(i + 0x88):X2}.dts";
            csFile = m_csEditOutput + '\\' + csAux;
            if (_access(csFile)) bAudio = true;
            //dds
            csAux = $"AudioFile_{(i + 0x90):X2}.dds";
            csFile = m_csEditOutput + '\\' + csAux;
            if (_access(csFile)) bAudio = true;
            //pcm
            csAux = $"AudioFile_{(i + 0xa0):X2}.wav";
            csFile = m_csEditOutput + '\\' + csAux;
            if (_access(csFile)) bAudio = true;
            //mpeg1
            csAux = $"AudioFile_{(i + 0xc0):X2}.mpa";
            csFile = m_csEditOutput + '\\' + csAux;
            if (_access(csFile)) bAudio = true;
            //mpeg2
            csAux = $"AudioFile_{(i + 0xd0):X2}.mpa";
            csFile = m_csEditOutput + '\\' + csAux;
            if (_access(csFile)) bAudio = true;
        }

        for (i = 0; i < 32 && bSubs == false; i++)
        {
            csAux = $"Subpictures_{(i + 0x20):X2}.sup";
            csFile = m_csEditOutput + '\\' + csAux;
            if (_access(csFile)) bSubs = true;
        }

        csFile = m_csEditOutput + '\\' + "VTS_*.VOB";

        if (AnyFileExists(csFile)) bVob = true;

        return (bAudio || bSubs || bVideo || bVob);

    }

    protected bool AnyFileExists(string csFile)
    {
        return File.Exists(csFile);
    }

    private static bool _access(string file) {
        return File.Exists(file);
    }
}