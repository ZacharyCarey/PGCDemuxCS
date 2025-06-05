using System.Numerics;

public class PgcDemuxApp
{
    public const string PGCDEMUX_VERSION = "1.2.0.5";
    internal const int MAXLENGTH = 20 * 1024 * 1024;
    internal const int MAX_PGC = 32768;
    internal const int MAX_LU = 100;
    internal const int MAX_MPGC = 32768;
    internal const int MODUPDATE = 100;
    internal const int MAXLOOKFORAUDIO = 10000;

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

    private PgcDemuxApp() {
        
    }

    public static bool Run(string command)
    {
        return theApp.InitInstance(command);
    }

    public Ref<byte> m_pIFO = Ref<byte>.Null;
    public Ref<byte> m_buffer = new ByteArrayRef(new byte[2050], 0);

    public bool m_bInProcess, m_bAbort, m_bCLI;
    public int m_iRet, nResponse;

    public bool m_bCheckIFrame;
    public bool m_bCheckVideoPack;
    public bool m_bCheckAudioPack;
    public bool m_bCheckNavPack;
    public bool m_bCheckSubPack;
    public bool m_bCheckLBA;

    public bool m_bCheckAud;
    public bool m_bCheckSub;
    public bool m_bCheckVid;
    public bool m_bCheckVob;
    public bool m_bCheckVob2;
    public bool m_bCheckLog;
    public bool m_bCheckCellt;
    public bool m_bCheckEndTime;

    public CString m_csInputIFO;
    public CString m_csInputPath;
    public CString m_csOutputPath;
    public int m_nPGCs;
    public int m_iIFOlen;
    public int m_nSelPGC;
    public int m_nSelAng;
    public int m_nVid, m_nCid;
    public ModeType m_iMode;
    public long m_i64OutputLBA;
    public int m_nVobout, m_nVidout, m_nCidout;
    public int m_nCurrVid;      // Used to write in different VOB files, one per VID
    public DomainType m_iDomain;
    public int m_nVidPacks, m_nAudPacks, m_nSubPacks, m_nNavPacks, m_nPadPacks, m_nUnkPacks;
    public bool m_bVMGM;
    public int m_nTotalFrames;
    public bool bNewCell;
    public int m_nLastVid, m_nLastCid;

    public CArray<ADT_CELL_LIST> m_AADT_Cell_list;
    public CArray<ADT_CELL_LIST> m_MADT_Cell_list;
    public CArray<ADT_VID_LIST> m_AADT_Vid_list;
    public CArray<ADT_VID_LIST> m_MADT_Vid_list;

    public int m_iVTS_PTT_SRPT, m_iVTS_PGCI, m_iVTS_C_ADT;
    public int m_iVTS_VOBU_ADMAP, m_iVTS_TMAPTI;
    public int m_iVTSM_PGCI, m_iVTSM_C_ADT, m_iVTSM_VOBU_ADMAP;
    public readonly int[] m_iVTS_PGC = new int[MAX_PGC];
    public readonly int[] m_C_PBKT = new int[MAX_PGC];
    public readonly int[] m_C_POST = new int[MAX_PGC];
    public readonly int[] m_nCells = new int[MAX_PGC];
    public readonly int[] m_nAngles = new int[MAX_PGC];

    public readonly int[] m_iVTSM_LU = new int[MAX_LU];
    public readonly int[] m_nIniPGCinLU = new int[MAX_LU];
    public readonly int[] m_nPGCinLU = new int[MAX_LU];
    public readonly int[] m_iMENU_PGC = new int[MAX_MPGC];
    public readonly int[] m_M_C_PBKT = new int[MAX_MPGC];
    public readonly int[] m_M_C_POST = new int[MAX_MPGC];
    public readonly int[] m_nMCells = new int[MAX_MPGC];
    public readonly int[] m_nLU_MPGC = new int[MAX_MPGC];

    public readonly ulong[] m_dwDuration = new ulong[MAX_PGC];
    public readonly ulong[] m_dwMDuration = new ulong[MAX_MPGC];

    public readonly long[] m_i64VOBSize = new long[10];
    public int m_nVobFiles;

    public readonly CFILE[] fsub = new CFILE[32];
    public readonly CFILE[] faud = new CFILE[8];
    public CFILE fvid = null;
    public CFILE fvob = null;
	public readonly AudioFormat[] m_audfmt = new AudioFormat[8];
    public readonly CString[] m_csAudname = new CString[8];
    public readonly int[] m_iFirstSubPTS = new int[32];
    public readonly int[] m_iFirstAudPTS = new int[8];
    public int m_iFirstVidPTS = 0;
    public int m_iFirstNavPTS0 = 0;
    public readonly int[] m_iAudIndex = new int[8];
    public int m_iSubPTS, m_iAudPTS, m_iVidPTS, m_iNavPTS0_old, m_iNavPTS0, m_iNavPTS1_old, m_iNavPTS1;
    public int m_iOffsetPTS;

    public int nLU, nAbsPGC;
    public int m_nLUs, m_nMPGCs;

    // Only in PCM
    public readonly int[] nbitspersample = new int[8];
    public readonly int[] nchannels = new int[8];
    public readonly int[] fsample = new int[8];

    public virtual bool InitInstance(string command) {
        string[] _argv = command.Split();
        int _argc = _argv.Length;

        int i,k;
        int nSelVid,nSelCid;

        m_pIFO = null;

    //	SetRegistryKey( "jsoto's tools" );
    //	WriteProfileInt("MySection", "My Key",123);

        for (i=0;i<32;i++) fsub[i]=null;
        for (i=0;i<8;i++) 
            faud[i]=null;
        fvob=fvid=null;

        m_bInProcess=false;
        m_bCLI=false;
        m_bAbort=false;
        m_bVMGM=false;

        m_bCheckAud=m_bCheckSub=m_bCheckLog=m_bCheckCellt=true;
        m_bCheckVid=m_bCheckVob=m_bCheckVob2=m_bCheckEndTime=false;

        m_bCheckIFrame=false;
        m_bCheckLBA=m_bCheckVideoPack=m_bCheckAudioPack=m_bCheckNavPack=m_bCheckSubPack=true;


        if ( _argc > 2 ) 
            // CLI mode
        {
            m_bCLI=true;
            if (ParseCommandLine(_argv, _argc) ==true)
            {
                m_bInProcess=true;
                m_iRet=ReadIFO();
                if (m_iRet==0)
                {
                    if (m_iMode==ModeType.PGC)
                    {
    // Check if PGC exists in done in PgcDemux
                        if (m_iDomain==DomainType.Titles)
                            m_iRet=PgcDemux (m_nSelPGC,m_nSelAng, null);
                        else
                            m_iRet=PgcMDemux(m_nSelPGC,null);
                    }
                    if (m_iMode== ModeType.VID)
                    {
    // Look for nSelVid
                        nSelVid=-1;
                        if (m_iDomain==DomainType.Titles)
                        {
                            for (k=0;k<m_AADT_Vid_list.GetSize() && nSelVid==-1; k++)
                                if (m_AADT_Vid_list[k].VID==m_nVid)
                                    nSelVid=k;
                        }
                        else
                        {
                            for (k=0;k<m_MADT_Vid_list.GetSize() && nSelVid==-1; k++)
                                if (m_MADT_Vid_list[k].VID==m_nVid)
                                    nSelVid=k;
                        }
                        if ( nSelVid==-1) 
                        {
                            m_iRet=-1;
                            Util.MyErrorBox( "Selected Vid not found!");
                        }
                        if (m_iRet==0)
                        {
                            if (m_iDomain== DomainType.Titles)
                                m_iRet=VIDDemux(nSelVid, null);
                            else
                                m_iRet=VIDMDemux(nSelVid,null);
                        }
                    }
                    if (m_iMode== ModeType.CID)
                    {
    // Look for nSelVid
                        nSelCid=-1;
                        if (m_iDomain== DomainType.Titles)
                        {
                            for (k=0;k<m_AADT_Cell_list.GetSize() && nSelCid==-1; k++)
                                if (m_AADT_Cell_list[k].VID==m_nVid && m_AADT_Cell_list[k].CID==m_nCid)
                                    nSelCid=k;
                        }
                        else
                        {
                            for (k=0;k<m_MADT_Cell_list.GetSize() && nSelCid==-1; k++)
                                if (m_MADT_Cell_list[k].VID==m_nVid && m_MADT_Cell_list[k].CID==m_nCid)
                                    nSelCid=k;
                        }
                        if ( nSelCid==-1) 
                        {
                            m_iRet=-1;
                            Util.MyErrorBox( "Selected Vid/Cid not found!");
                        }
                        if (m_iRet==0)
                        {
                            if (m_iDomain== DomainType.Titles)
                                m_iRet=CIDDemux(nSelCid, null);
                            else
                                m_iRet=CIDMDemux(nSelCid,null);
                        }
                    }


                }
                m_bInProcess=false;
            }
            else
                m_iRet=-1;

            //  return false so that we exit the
            //  application, rather than start the application's message pump.
            return false;
        }



    // Dlg mode	
        /*CPgcDemuxDlg dlg;
        m_pMainWnd = &dlg;
        int nResponse = dlg.DoModal();
        if (nResponse == IDOK)
        {
            // TODO: Place code here to handle when the dialog is
            //  dismissed with OK
        }
        else if (nResponse == IDCANCEL)
        {
            // TODO: Place code here to handle when the dialog is
            //  dismissed with Cancel
        }*/

        // Since the dialog has been closed, return false so that we exit the
        //  application, rather than start the application's message pump.
        return false;
    }

    public virtual void UpdateProgress(object pDlg, int nPerc) {
        // TODO update progress
    }
    public virtual int ExitInstance() {
        return m_iRet;
    }
    public virtual bool ParseCommandLine(string[] __argv, int __argc) {
    /*
    PgcDemux [option1] [option2] ... [option12] <ifo_input_file> <destination_folder>
    option1: [-pgc, <pgcnumber>].      Selects the PGC number (from 1 to nPGCs). Default 1
    option2: [-ang, <angnumber>].      Selects the Angle number (from 1 to n). Default 1
    option3: [-vid, <vobid>].          Selects the Angle number (from 1 to n). Default 1
    option4: [-cid, <vobid> <cellid>]. Selects a cell vobid (from 1 to n). Default 1
    option5: {-m2v, -nom2v}. Extracts/No extracts video file. Default NO
    option6: {-aud, -noaud}. Extracts/No extracts audio streams. Default YES
    option7: {-sub, -nosub}. Extracts/No extracts subs streams. Default YES
    option8: {-vob, -novob}. Generates a single PGC VOB. Default NO
    option9: {-customvob <flags>}. Generates a custom VOB file. Flags:
            b: split VOB: one file per vob_id
            n: write nav packs
            v: write video packs
            a: write audio packs
            s: write subs packs
            i: only first Iframe
            l: patch LBA number
    option10:{-cellt, -nocellt}. Generates a Celltimes.txt file. Default YES
    option10:{-endt, -noendt}. Includes Last end time in Celltimes.txt. Default NO
    option11:{-log, -nolog}. Generates a log file. Default YES
    option12:{-menu, -title}. Domain. Default Title (except if filename is VIDEO_TS.IFO)
    */
        int i;
        CString csPar,csPar2;
        bool bRet;
        CString csAux,csAux1,csAux2;


        bRet=true;

    //	m_bCheckAud=m_bCheckSub=m_bCheckLog=m_bCheckCellt=true;
    //	m_bCheckVid=m_bCheckVob=m_bCheckVob2=m_bCheckEndTime=false;

    //	m_bCheckIFrame=false;
    //	m_bCheckLBA=m_bCheckVideoPack=m_bCheckAudioPack=m_bCheckNavPack=m_bCheckSubPack=true;


        m_nSelPGC=m_nSelAng=0;
        m_iMode= ModeType.PGC;
        m_iDomain= DomainType.Titles;

        if (__argc < 3) return false;
        
        for (i =1 ; i<(__argc)-1 ; i++)
        {
            csPar = $"{__argv[i]}";
            csPar.MakeLower();

            if ( csPar=="-pgc" && __argc >i+1 )	
            {
                if (!int.TryParse(__argv[i+1], out m_nSelPGC) ||m_nSelPGC <1 || m_nSelPGC >255)
                {
                    Util.MyErrorBox( "Invalid pgc number!");
                    return false;
                }
                m_iMode= ModeType.PGC;
                i++;
                m_nSelPGC--; // internally from 0 to nPGCs-1.
            }
            else if ( csPar=="-ang" && __argc >i+1 )	
            {
                if (!int.TryParse(__argv[i+1], out m_nSelAng) || m_nSelAng <1 || m_nSelAng >9)
                {
                    Util.MyErrorBox( "Invalid angle number!");
                    return false;
                }
                i++;
                m_nSelAng--; // internally from 0 to nAngs-1.
            }
            else if ( csPar=="-vid" && __argc >i+1 )	
            {
                if (!int.TryParse(__argv[i+1], out m_nVid) || m_nVid <1 || m_nVid >32768)
                {
                    Util.MyErrorBox( "Invalid Vid number!");
                    return false;
                }
                m_iMode= ModeType.VID;
                i++;
            }
            else if ( csPar=="-cid" && __argc >i+2 )	
            {
                if (!int.TryParse(__argv[i+1], out m_nVid) || m_nVid <1 || m_nVid >32768)
                {
                    Util.MyErrorBox( "Invalid Vid number!");
                    return false;
                }
                if (!int.TryParse(__argv[i+2], out m_nCid) || m_nCid <1 || m_nCid >255)
                {
                    Util.MyErrorBox( "Invalid Cid number!");
                    return false;
                }

                m_iMode= ModeType.CID;
                i+=2;
            }
            else if ( csPar=="-customvob" && __argc >i+1 )	
            {
                csPar2 = $"{__argv[i+1]}";
                csPar2.MakeLower();

                m_bCheckVob=true;
    // n: write nav packs
    // v: write video packs
    // a: write audio packs
    // s: write subs packs
    // i: only first Iframe
    // b: split file per vob_id
    // l: Patch LBA number

                if (csPar2.Find('b')!=-1)  m_bCheckVob2=true;
                else m_bCheckVob2=false;
                if (csPar2.Find('v')!=-1)  m_bCheckVideoPack=true;
                else m_bCheckVideoPack=false;
                if (csPar2.Find('a')!=-1)  m_bCheckAudioPack=true;
                else m_bCheckAudioPack=false;
                if (csPar2.Find('n')!=-1)  m_bCheckNavPack=true;
                else m_bCheckNavPack=false;
                if (csPar2.Find('s')!=-1)  m_bCheckSubPack=true;
                else m_bCheckSubPack=false;
                if (csPar2.Find('i')!=-1)  m_bCheckIFrame=true;
                else m_bCheckIFrame=false;
                if (csPar2.Find('l')!=-1)  m_bCheckLBA=true;
                else m_bCheckLBA=false;
                i++;
            }
            else if ( csPar=="-m2v" )  m_bCheckVid=true;   
            else if ( csPar=="-vob" )  m_bCheckVob=true;   
            else if ( csPar=="-aud" )  m_bCheckAud=true;   
            else if ( csPar=="-sub" )  m_bCheckSub=true;   
            else if ( csPar=="-log" )  m_bCheckLog=true;   
            else if ( csPar=="-cellt" )  m_bCheckCellt=true;   
            else if ( csPar=="-endt" )  m_bCheckEndTime=true;   
            else if ( csPar=="-nom2v" )  m_bCheckVid=false;   
            else if ( csPar=="-novob" )  m_bCheckVob=false;   
            else if ( csPar=="-noaud" )  m_bCheckAud=false;   
            else if ( csPar=="-nosub" )  m_bCheckSub=false;   
            else if ( csPar=="-nolog" )  m_bCheckLog=false;   
            else if ( csPar=="-nocellt" )  m_bCheckCellt=false;   
            else if ( csPar=="-noendt" )  m_bCheckEndTime=false;   
            else if ( csPar=="-menu" )	m_iDomain= DomainType.Menus;   
            else if ( csPar=="-title" )  m_iDomain= DomainType.Titles;   
        }
        m_csInputIFO=__argv[(__argc) -2];
        m_csOutputPath=__argv[(__argc) -1];

        m_csInputPath=m_csInputIFO.Left(m_csInputIFO.ReverseFind('\\'));

        m_csInputIFO.MakeUpper();
        m_csOutputPath.MakeUpper();
        m_csInputPath.MakeUpper();

        csAux=m_csInputIFO.Right(m_csInputIFO.GetLength()-m_csInputIFO.ReverseFind('\\')-1);
        csAux1=csAux.Left(4);
        csAux=m_csInputIFO.Right(6);
        csAux2=m_csInputIFO.Right(12);
        if ( (csAux!="_0.IFO" || csAux1 != "VTS_" ) && csAux2 !="VIDEO_TS.IFO")
        {
            Util.MyErrorBox( "Invalid input file!");
            return false;
        }
        
        if (csAux2=="VIDEO_TS.IFO")
        {
            m_bVMGM=true;
            m_iDomain= DomainType.Menus;
        }
        else m_bVMGM=false;

        return true;
    }
    public virtual int ReadIFO() {
        CString csAux,csAux2;
        int i,j,k,kk,nCell,nVIDs;
        kk = 0;
        ADT_CELL_LIST myADT_Cell = new();
        ADT_VID_LIST myADT_Vid = new();
        int nTotADT, nADT, VidADT,CidADT;
        int iArraysize;
        bool bAlready, bEndAngle;
        CFILE inFile = null;
        int iIniSec,iEndSec;
        FileInfo statbuf;
        int iSize,iCat;
        int iIFOSize = 0;


        if (Util._stati64 ( m_csInputIFO, out statbuf))
            iIFOSize= (int) statbuf.Length;
        
        if ( iIFOSize > MAXLENGTH)
        {
            csAux = $"IFO too big {m_csInputIFO}";
            Util.MyErrorBox (csAux);
            return -1;
        }

        inFile=CFILE.fopen(m_csInputIFO,"rb");
        if (inFile == null)
        {
            csAux = $"Unable to open {m_csInputIFO}";
            Util.MyErrorBox (csAux);
            return -1;
        }

        //if (m_pIFO!=null)  delete[]  m_pIFO;

        m_pIFO = new ByteArrayRef(new byte[iIFOSize+2048], 0);

    // Read IFO


        for (i=0;!inFile.feof() && i< MAXLENGTH ;i++)
            m_pIFO[i]=inFile.fgetc();
        m_iIFOlen=i-1;
        inFile.fclose();

        m_AADT_Cell_list.RemoveAll();
        m_MADT_Cell_list.RemoveAll();
        m_AADT_Vid_list.RemoveAll();
        m_MADT_Vid_list.RemoveAll();


    // Get Title Cells
        if (m_bVMGM) 
        {
            m_iVTS_PTT_SRPT=   0;
            m_iVTS_PGCI=       0;
            m_iVTSM_PGCI=      2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xC8));
            m_iVTS_TMAPTI=     0;
            m_iVTSM_C_ADT=     2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xD8));
            m_iVTSM_VOBU_ADMAP=2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xDC));
            m_iVTS_C_ADT=      0;
            m_iVTS_VOBU_ADMAP= 0;
        }
        else
        {
            m_iVTS_PTT_SRPT=   2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xC8));
            m_iVTS_PGCI=       2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xCC));
            m_iVTSM_PGCI=      2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xD0));
            m_iVTS_TMAPTI=     2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xD4));
            m_iVTSM_C_ADT=     2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xD8));
            m_iVTSM_VOBU_ADMAP=2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xDC));
            m_iVTS_C_ADT=      2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xE0));
            m_iVTS_VOBU_ADMAP= 2048*Util.GetNbytes(4,m_pIFO.AtIndex(0xE4));
        }
        if (m_bVMGM) 
            m_nPGCs=0;
        else
            m_nPGCs=Util.GetNbytes(2,m_pIFO.AtIndex(m_iVTS_PGCI));


    // Title PGCs	
        if (m_nPGCs > MAX_PGC)
        {
            csAux = $"ERROR: Max PGCs limit ({MAX_PGC}) has been reached.";
            Util.MyErrorBox (csAux);
            return -1;
        }
        for (k=0; k<m_nPGCs;k++)
        {
            m_iVTS_PGC[k]=Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTS_PGCI+0x04+(k+1)*8))+m_iVTS_PGCI;
            m_dwDuration[k]=(uint)Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTS_PGC[k]+4));

            m_C_PBKT[k]=Util.GetNbytes(2,m_pIFO.AtIndex(m_iVTS_PGC[k]+0xE8));
            if (m_C_PBKT[k]!=0  ) m_C_PBKT[k]+=m_iVTS_PGC[k];

            m_C_POST[k]=Util.GetNbytes(2,m_pIFO.AtIndex(m_iVTS_PGC[k]+0xEA));
            if (m_C_POST[k]!=0  ) m_C_POST[k]+=m_iVTS_PGC[k];

            m_nCells[k]=m_pIFO[m_iVTS_PGC[k]+3];


            m_nAngles[k]=1;

            for (nCell=0,bEndAngle=false; nCell<m_nCells[k] && bEndAngle==false; nCell++)
            {
                iCat=Util.GetNbytes(1,m_pIFO.AtIndex(m_C_PBKT[k]+24*nCell));
                iCat=iCat & 0xF0;
    //			0101=First; 1001=Middle ;	1101=Last
                if      (iCat == 0x50)
                    m_nAngles[k]=1;
                else if (iCat == 0x90)
                    m_nAngles[k]++;
                else if (iCat == 0xD0)
                {
                    m_nAngles[k]++;
                    bEndAngle=true;
                }
            }
        }


    // Menu PGCs
        if( m_iVTSM_PGCI==0 )
            m_nLUs=0;
        else
            m_nLUs=Util.GetNbytes(2,m_pIFO.AtIndex(m_iVTSM_PGCI));

        m_nMPGCs=0;
        if (m_nLUs > MAX_LU)
        {
            csAux = $"ERROR: Max LUs limit ({MAX_LU}) has been reached.";
            Util.MyErrorBox (csAux);
            return -1;
        }

        for (nLU=0; nLU<m_nLUs;nLU++)
        {
            m_iVTSM_LU[nLU]=   Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTSM_PGCI+0x04+(nLU+1)*8))+m_iVTSM_PGCI;
            m_nPGCinLU[nLU]=   Util.GetNbytes(2,m_pIFO.AtIndex(m_iVTSM_LU[nLU]));
            m_nIniPGCinLU[nLU]= m_nMPGCs;

            for (j=0; j < m_nPGCinLU[nLU]; j++)
            {
                if ((m_nMPGCs + m_nPGCinLU[nLU]) > MAX_MPGC)
                {
                    csAux = $"ERROR: Max MPGCs limit ({MAX_MPGC}) has been reached.";
                    Util.MyErrorBox (csAux);
                    return -1;
                }
                nAbsPGC=j+m_nMPGCs;
                m_nLU_MPGC[nAbsPGC]=nLU;
                m_iMENU_PGC[nAbsPGC]= Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTSM_LU[nLU]+0x04+(j+1)*8))+m_iVTSM_LU[nLU];

                m_M_C_PBKT[nAbsPGC]  =Util.GetNbytes(2,m_pIFO.AtIndex(m_iMENU_PGC[nAbsPGC]+0xE8));
                if (m_M_C_PBKT[nAbsPGC] !=0) m_M_C_PBKT[nAbsPGC] += m_iMENU_PGC[nAbsPGC];
                m_M_C_POST[nAbsPGC]  =Util.GetNbytes(2,m_pIFO.AtIndex(m_iMENU_PGC[nAbsPGC]+0xEA));
                if (m_M_C_POST[nAbsPGC] !=0) m_M_C_POST[nAbsPGC] +=m_iMENU_PGC[nAbsPGC];

                m_nMCells[nAbsPGC]=m_pIFO[m_iMENU_PGC[nAbsPGC]+3];

                if ( (m_M_C_PBKT[nAbsPGC]==0 || m_M_C_POST[nAbsPGC] ==0) &&  m_nMCells[nAbsPGC]!=0)
    // There is something wrong...
                {
                    m_nMCells[nAbsPGC]=0;
                    csAux = $"ERROR: There is something wrong in number of cells in LU {nLU:00}, Menu PGC {j:00}.";
                    Util.MyErrorBox (csAux);
                    return -1;
                }
                m_dwMDuration[nAbsPGC]=(uint)Util.GetNbytes(4,m_pIFO.AtIndex(m_iMENU_PGC[nAbsPGC]+4));

            } // For PGCs
            m_nMPGCs+=m_nPGCinLU[nLU];
        }


    ///////////// VTS_C_ADT  ///////////////////////
        if (m_iVTS_C_ADT==0) nTotADT=0;
        else
        {
            nTotADT=Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTS_C_ADT+4));
            nTotADT=(nTotADT-7)/12;
        }

    //Cells
        for (nADT=0; nADT <nTotADT; nADT++)
        {
            VidADT=Util.GetNbytes(2,m_pIFO.AtIndex(m_iVTS_C_ADT+8+12*nADT));
            CidADT=m_pIFO[m_iVTS_C_ADT+8+12*nADT+2];

            iArraysize=m_AADT_Cell_list.GetSize();
            for (k=0,bAlready=false; k< iArraysize ;k++)
            {
                if (CidADT==m_AADT_Cell_list[k].CID &&
                    VidADT==m_AADT_Cell_list[k].VID )
                {
                    bAlready=true;
                    kk=k;
                }
            }
            if (!bAlready)
            {
                myADT_Cell.CID=CidADT;
                myADT_Cell.VID=VidADT;
                myADT_Cell.iSize=0;
                myADT_Cell.iIniSec=0x7fffffff;
                myADT_Cell.iEndSec=0;
                kk=InsertCell (myADT_Cell, DomainType.Titles);
    //			m_AADT_Cell_list.SetAtGrow(iArraysize,myADT_Cell);
    //			kk=iArraysize;
            }
            iIniSec=Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTS_C_ADT+8+12*nADT+4));
            iEndSec=Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTS_C_ADT+8+12*nADT+8));
            if (iIniSec < m_AADT_Cell_list[kk].iIniSec) m_AADT_Cell_list[kk].iIniSec=iIniSec;
            if (iEndSec > m_AADT_Cell_list[kk].iEndSec) m_AADT_Cell_list[kk].iEndSec=iEndSec;
            iSize=(iEndSec-iIniSec+1);
            m_AADT_Cell_list[kk].iSize+=(iEndSec-iIniSec+1);
        }

    ///////////// VTSM_C_ADT  ///////////////////////
        if (m_iVTSM_C_ADT==0) nTotADT=0;
        else
        {
            nTotADT=Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTSM_C_ADT+4));
            nTotADT=(nTotADT-7)/12;
        }

    // Cells
        for (nADT=0; nADT <nTotADT; nADT++)
        {
            VidADT=Util.GetNbytes(2,m_pIFO.AtIndex(m_iVTSM_C_ADT+8+12*nADT));
            CidADT=m_pIFO[m_iVTSM_C_ADT+8+12*nADT+2];

            iArraysize=m_MADT_Cell_list.GetSize();
            for (k=0,bAlready=false; k< iArraysize ;k++)
            {
                if (CidADT==m_MADT_Cell_list[k].CID &&
                    VidADT==m_MADT_Cell_list[k].VID )
                {
                    bAlready=true;
                    kk=k;
                }
            }
            if (!bAlready)
            {
                myADT_Cell.CID=CidADT;
                myADT_Cell.VID=VidADT;
                myADT_Cell.iSize=0;
                myADT_Cell.iIniSec=0x7fffffff;
                myADT_Cell.iEndSec=0;
                kk=InsertCell (myADT_Cell, DomainType.Menus);
    //			m_MADT_Cell_list.SetAtGrow(iArraysize,myADT_Cell);
    //			kk=iArraysize;
            }
            iIniSec=Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTSM_C_ADT+8+12*nADT+4));
            iEndSec=Util.GetNbytes(4,m_pIFO.AtIndex(m_iVTSM_C_ADT+8+12*nADT+8));
            if (iIniSec < m_MADT_Cell_list[kk].iIniSec) m_MADT_Cell_list[kk].iIniSec=iIniSec;
            if (iEndSec > m_MADT_Cell_list[kk].iEndSec) m_MADT_Cell_list[kk].iEndSec=iEndSec;
            iSize=(iEndSec-iIniSec+1);
            m_MADT_Cell_list[kk].iSize+=(iEndSec-iIniSec+1);
        }

        FillDurations();

    //////////////////////////////////////////////////////////////	
    /////////////   VIDs
    // VIDs in Titles
        iArraysize=m_AADT_Cell_list.GetSize();
        for (i=0; i <iArraysize; i++)
        {
            VidADT=m_AADT_Cell_list[i].VID;

            nVIDs=m_AADT_Vid_list.GetSize();
            for (k=0,bAlready=false; k< nVIDs ;k++)
            {
                if (VidADT==m_AADT_Vid_list[k].VID )
                {
                    bAlready=true;
                    kk=k;
                }
            }
            if (!bAlready)
            {
                myADT_Vid.VID=VidADT;
                myADT_Vid.iSize=0;
                myADT_Vid.nCells=0;
                myADT_Vid.dwDuration=0;
                m_AADT_Vid_list.SetAtGrow(nVIDs,myADT_Vid);
                kk=nVIDs;
            }
            m_AADT_Vid_list[kk].iSize+=m_AADT_Cell_list[i].iSize;
            m_AADT_Vid_list[kk].nCells++;
            m_AADT_Vid_list[kk].dwDuration=Util.AddDuration(m_AADT_Cell_list[i].dwDuration,m_AADT_Vid_list[kk].dwDuration);
        }
        
    // VIDs in Menus
        iArraysize=m_MADT_Cell_list.GetSize();
        for (i=0; i <iArraysize; i++)
        {
            VidADT=m_MADT_Cell_list[i].VID;

            nVIDs=m_MADT_Vid_list.GetSize();
            for (k=0,bAlready=false; k< nVIDs ;k++)
            {
                if (VidADT==m_MADT_Vid_list[k].VID )
                {
                    bAlready=true;
                    kk=k;
                }
            }
            if (!bAlready)
            {
                myADT_Vid.VID=VidADT;
                myADT_Vid.iSize=0;
                myADT_Vid.nCells=0;
                myADT_Vid.dwDuration=0;
                m_MADT_Vid_list.SetAtGrow(nVIDs,myADT_Vid);
                kk=nVIDs;
            }
            m_MADT_Vid_list[kk].iSize+=m_MADT_Cell_list[i].iSize;
            m_MADT_Vid_list[kk].nCells++;
            m_MADT_Vid_list[kk].dwDuration=Util.AddDuration(m_MADT_Cell_list[i].dwDuration,m_MADT_Vid_list[kk].dwDuration);
        }
        
    // Fill VOB file size
        if (m_bVMGM)
        {
            m_nVobFiles=0;

            for (k=0; k<10; k++)
                m_i64VOBSize[k]=0;

            csAux2=m_csInputIFO.Left(m_csInputIFO.GetLength()-3);
            csAux=csAux2+"VOB";
            if (Util._stati64 ( csAux, out statbuf))
                m_i64VOBSize[0]= statbuf.Length;
        }
        else
        {
            for (k=0; k<10; k++)
            {
                csAux2=m_csInputIFO.Left(m_csInputIFO.GetLength()-5);
                csAux = $"{k}.VOB";
                csAux=csAux2+csAux;
                if (Util._stati64 ( csAux, out statbuf))
                {
                    m_i64VOBSize[k]= statbuf.Length;
                    m_nVobFiles=k;
                }
                else 
                    m_i64VOBSize[k]=0;
            }
        }

        return 0;
    }
    public virtual int PgcDemux(int nPGC, int nAng, object pDlg) {
        int nTotalSectors;
        int nSector, nCell;
        int k, iArraysize;
        int CID, VID;
        long i64IniSec, i64EndSec;
        long i64sectors;
        int nVobin = 0;
        CString csAux, csAux2;
        CFILE inFile, fout;
        long i64;
        bool bMyCell;
        int iRet;
        uint dwCellDuration;
        int nFrames;
        int nCurrAngle, iCat;

        if (nPGC >= theApp.m_nPGCs)
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
        iArraysize = m_AADT_Cell_list.GetSize();
        for (nCell = nCurrAngle = 0; nCell < m_nCells[nPGC]; nCell++)
        {
            VID = Util.GetNbytes(2, m_pIFO.AtIndex(m_C_POST[nPGC] + 4 * nCell));
            CID = m_pIFO[m_C_POST[nPGC] + 3 + 4 * nCell];

            iCat = m_pIFO[m_C_PBKT[nPGC] + 24 * nCell];
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
                    if (CID == m_AADT_Cell_list[k].CID &&
                        VID == m_AADT_Cell_list[k].VID)
                    {
                        nTotalSectors += m_AADT_Cell_list[k].iSize;
                    }
                }
            }
            if (iCat == 0xD0) nCurrAngle = 0;
        }

        nSector = 0;
        iRet = 0;
        for (nCell = nCurrAngle = 0; nCell < m_nCells[nPGC] && m_bInProcess == true; nCell++)
        {
            iCat = m_pIFO[m_C_PBKT[nPGC] + 24 * nCell];
            iCat = iCat & 0xF0;
            //		0101=First; 1001=Middle ;	1101=Last
            if (iCat == 0x50)
                nCurrAngle = 1;
            else if ((iCat == 0x90 || iCat == 0xD0) && nCurrAngle != 0)
                nCurrAngle++;
            if (iCat == 0 || (nAng + 1) == nCurrAngle)
            {

                VID = Util.GetNbytes(2, m_pIFO.AtIndex(m_C_POST[nPGC] + 4 * nCell));
                CID = m_pIFO[m_C_POST[nPGC] + 3 + 4 * nCell];

                i64IniSec = Util.GetNbytes(4, m_pIFO.AtIndex(m_C_PBKT[nPGC] + nCell * 24 + 8));
                i64EndSec = Util.GetNbytes(4, m_pIFO.AtIndex(m_C_PBKT[nPGC] + nCell * 24 + 0x14));
                for (k = 1, i64sectors = 0; k < 10; k++)
                {
                    i64sectors += (m_i64VOBSize[k] / 2048);
                    if (i64IniSec < i64sectors)
                    {
                        i64sectors -= (m_i64VOBSize[k] / 2048);
                        nVobin = k;
                        k = 20;
                    }
                }
                csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
                csAux = $"{nVobin}.VOB";
                csAux = csAux2 + csAux;
                inFile = CFILE.fopen(csAux, "rb");
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
                        csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
                        csAux = $"{nVobin}.VOB";
                        csAux = csAux2 + csAux;
                        inFile = CFILE.fopen(csAux, "rb");
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

        if (m_bCheckCellt && m_bInProcess == true)
        {
            csAux = m_csOutputPath + '\\' + "Celltimes.txt";
            fout = CFILE.fopen(csAux, "w");
            for (nCell = 0, nCurrAngle = 0; nCell < m_nCells[nPGC] && m_bInProcess == true; nCell++)
            {
                dwCellDuration = (uint)Util.GetNbytes(4, m_pIFO.AtIndex(m_C_PBKT[nPGC] + 24 * nCell + 4));

                iCat = m_pIFO[m_C_PBKT[nPGC] + 24 * nCell];
                iCat = iCat & 0xF0;
                //			0101=First; 1001=Middle ;	1101=Last
                if (iCat == 0x50)
                    nCurrAngle = 1;
                else if ((iCat == 0x90 || iCat == 0xD0) && nCurrAngle != 0)
                    nCurrAngle++;
                if (iCat == 0 || (nAng + 1) == nCurrAngle)
                {
                    nFrames += Util.DurationInFrames(dwCellDuration);
                    if (nCell != (m_nCells[nPGC] - 1) || m_bCheckEndTime)
                        fout.fprintf($"{nFrames}\n");
                }

                if (iCat == 0xD0) nCurrAngle = 0;
            }
            fout.fclose();
        }

        m_nTotalFrames = nFrames;

        if (m_bCheckLog && m_bInProcess == true) OutputLog(nPGC, nAng, DomainType.Titles);

        return iRet;
    }
    public virtual int PgcMDemux(int nPGC, object pDlg) {
        int nTotalSectors;
        int nSector, nCell;
        int k, iArraysize;
        int CID, VID;
        long i64IniSec, i64EndSec;
        CString csAux, csAux2;
        CFILE inFile;
        CFILE fout;
        long i64;
        bool bMyCell;
        int iRet;
        uint dwCellDuration;
        int nFrames;


        if (nPGC >= theApp.m_nMPGCs)
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
        iArraysize = m_MADT_Cell_list.GetSize();
        for (nCell = 0; nCell < m_nMCells[nPGC]; nCell++)
        {
            VID = Util.GetNbytes(2, m_pIFO.AtIndex(m_M_C_POST[nPGC] + 4 * nCell));
            CID = m_pIFO[m_M_C_POST[nPGC] + 3 + 4 * nCell];
            for (k = 0; k < iArraysize; k++)
            {
                if (CID == m_MADT_Cell_list[k].CID &&
                    VID == m_MADT_Cell_list[k].VID)
                {
                    nTotalSectors += m_MADT_Cell_list[k].iSize;
                }
            }
        }

        nSector = 0;
        iRet = 0;

        for (nCell = 0; nCell < m_nMCells[nPGC] && m_bInProcess == true; nCell++)
        {
            VID = Util.GetNbytes(2, m_pIFO.AtIndex(m_M_C_POST[nPGC] + 4 * nCell));
            CID = m_pIFO[m_M_C_POST[nPGC] + 3 + 4 * nCell];

            i64IniSec = Util.GetNbytes(4, m_pIFO.AtIndex(m_M_C_PBKT[nPGC] + nCell * 24 + 8));
            i64EndSec = Util.GetNbytes(4, m_pIFO.AtIndex(m_M_C_PBKT[nPGC] + nCell * 24 + 0x14));

            if (m_bVMGM)
            {
                csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 3);
                csAux = csAux2 + "VOB";
            }
            else
            {
                csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
                csAux = csAux2 + "0.VOB";
            }
            inFile = CFILE.fopen(csAux, "rb");
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

        if (m_bCheckCellt && m_bInProcess == true)
        {
            csAux = m_csOutputPath + '\\' + "Celltimes.txt";
            fout = CFILE.fopen(csAux, "w");
            for (nCell = 0; nCell < m_nMCells[nPGC] && m_bInProcess == true; nCell++)
            {
                dwCellDuration = (uint)Util.GetNbytes(4, m_pIFO.AtIndex(m_M_C_PBKT[nPGC] + 24 * nCell + 4));
                nFrames += Util.DurationInFrames(dwCellDuration);
                if (nCell != (m_nMCells[nPGC] - 1) || m_bCheckEndTime)
                    fout.fprintf($"{nFrames}\n");
            }
            fout.fclose();
        }

        m_nTotalFrames = nFrames;

        if (m_bCheckLog && m_bInProcess == true) OutputLog(nPGC, 1, DomainType.Menus);

        return iRet;
    }
    public virtual int VIDDemux(int nVid, object pDlg) {
        int nTotalSectors;
        int nSector, nCell;
        int k, iArraysize;
        int CID, VID, nDemuxedVID;
        long i64IniSec, i64EndSec;
        long i64sectors;
        int nVobin = 0;
        CString csAux, csAux2;
        CFILE inFile;
        CFILE fout;
        long i64;
        bool bMyCell;
        int iRet;
        int nFrames;
        int nLastCell;

        if (nVid >= m_AADT_Vid_list.GetSize())
        {
            Util.MyErrorBox("Error: Selected Vid does not exist");
            m_bInProcess = false;
            return -1;
        }

        IniDemuxGlobalVars();
        if (OpenVideoFile()) return -1;
        m_bInProcess = true;

        // Calculate  the total number of sectors
        nTotalSectors = m_AADT_Vid_list[nVid].iSize;
        nSector = 0;
        iRet = 0;
        nDemuxedVID = m_AADT_Vid_list[nVid].VID;

        iArraysize = m_AADT_Cell_list.GetSize();
        for (nCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
        {
            VID = m_AADT_Cell_list[nCell].VID;
            CID = m_AADT_Cell_list[nCell].CID;

            if (VID == nDemuxedVID)
            {
                i64IniSec = m_AADT_Cell_list[nCell].iIniSec;
                i64EndSec = m_AADT_Cell_list[nCell].iEndSec;
                for (k = 1, i64sectors = 0; k < 10; k++)
                {
                    i64sectors += (m_i64VOBSize[k] / 2048);
                    if (i64IniSec < i64sectors)
                    {
                        i64sectors -= (m_i64VOBSize[k] / 2048);
                        nVobin = k;
                        k = 20;
                    }
                }
                csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
                csAux = $"{nVobin}.VOB";
                csAux = csAux2 + csAux;
                inFile = CFILE.fopen(csAux, "rb");
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
                        csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
                        csAux = $"{nVobin}.VOB";
                        csAux = csAux2 + csAux;
                        inFile = CFILE.fopen(csAux, "rb");
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

        if (m_bCheckCellt && m_bInProcess == true)
        {
            csAux = m_csOutputPath + '\\' + "Celltimes.txt";
            fout = CFILE.fopen(csAux, "w");

            nDemuxedVID = m_AADT_Vid_list[nVid].VID;

            iArraysize = m_AADT_Cell_list.GetSize();
            for (nCell = nLastCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
            {
                VID = m_AADT_Cell_list[nCell].VID;
                if (VID == nDemuxedVID)
                    nLastCell = nCell;
            }

            for (nCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
            {
                VID = m_AADT_Cell_list[nCell].VID;

                if (VID == nDemuxedVID)
                {
                    nFrames += Util.DurationInFrames(m_AADT_Cell_list[nCell].dwDuration);
                    if (nCell != nLastCell || m_bCheckEndTime)
                        fout.fprintf($"{nFrames}\n");
                }
            }
            fout.fclose();
        }

        m_nTotalFrames = nFrames;

        if (m_bCheckLog && m_bInProcess == true) OutputLog(nVid, 1, DomainType.Titles);

        return iRet;
    }
    public virtual int VIDMDemux(int nVid, object pDlg) {
        int nTotalSectors;
        int nSector, nCell;
        int iArraysize;
        int CID, VID, nDemuxedVID;
        long i64IniSec, i64EndSec;
        CString csAux, csAux2;
        CFILE inFile;
        CFILE fout;
        long i64;
        bool bMyCell;
        int iRet;
        int nFrames;
        int nLastCell;

        if (nVid >= m_MADT_Vid_list.GetSize())
        {
            Util.MyErrorBox("Error: Selected Vid does not exist");
            m_bInProcess = false;
            return -1;
        }

        IniDemuxGlobalVars();
        if (OpenVideoFile()) return -1;
        m_bInProcess = true;

        // Calculate  the total number of sectors
        nTotalSectors = m_MADT_Vid_list[nVid].iSize;
        nSector = 0;
        iRet = 0;
        nDemuxedVID = m_MADT_Vid_list[nVid].VID;

        iArraysize = m_MADT_Cell_list.GetSize();
        for (nCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
        {
            VID = m_MADT_Cell_list[nCell].VID;
            CID = m_MADT_Cell_list[nCell].CID;

            if (VID == nDemuxedVID)
            {
                i64IniSec = m_MADT_Cell_list[nCell].iIniSec;
                i64EndSec = m_MADT_Cell_list[nCell].iEndSec;
                if (m_bVMGM)
                {
                    csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 3);
                    csAux = csAux2 + "VOB";
                }
                else
                {
                    csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
                    csAux = csAux2 + "0.VOB";
                }
                inFile = CFILE.fopen(csAux, "rb");
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

        if (m_bCheckCellt && m_bInProcess == true)
        {
            csAux = m_csOutputPath + '\\' + "Celltimes.txt";
            fout = CFILE.fopen(csAux, "w");

            nDemuxedVID = m_MADT_Vid_list[nVid].VID;

            iArraysize = m_MADT_Cell_list.GetSize();

            for (nCell = nLastCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
            {
                VID = m_MADT_Cell_list[nCell].VID;
                if (VID == nDemuxedVID) nLastCell = nCell;
            }


            for (nCell = 0; nCell < iArraysize && m_bInProcess == true; nCell++)
            {
                VID = m_MADT_Cell_list[nCell].VID;

                if (VID == nDemuxedVID)
                {
                    nFrames += Util.DurationInFrames(m_MADT_Cell_list[nCell].dwDuration);
                    if (nCell != nLastCell || m_bCheckEndTime)
                        fout.fprintf($"{nFrames}\n");
                }
            }
            fout.fclose();
        }

        m_nTotalFrames = nFrames;

        if (m_bCheckLog && m_bInProcess == true) OutputLog(nVid, 1, DomainType.Menus);

        return iRet;
    }
    public virtual int CIDDemux(int nCell, object pDlg) {
        int nTotalSectors;
        int nSector;
        int k;
        int CID, VID;
        long i64IniSec, i64EndSec;
        long i64sectors;
        int nVobin = 0;
        CString csAux, csAux2;
        CFILE inFile;
        CFILE fout;
        long i64;
        bool bMyCell;
        int iRet;
        int nFrames;

        if (nCell >= m_AADT_Cell_list.GetSize())
        {
            Util.MyErrorBox("Error: Selected Cell does not exist");
            m_bInProcess = false;
            return -1;
        }

        IniDemuxGlobalVars();
        if (OpenVideoFile()) return -1;
        m_bInProcess = true;

        // Calculate  the total number of sectors
        nTotalSectors = m_AADT_Cell_list[nCell].iSize;
        nSector = 0;
        iRet = 0;

        VID = m_AADT_Cell_list[nCell].VID;
        CID = m_AADT_Cell_list[nCell].CID;

        i64IniSec = m_AADT_Cell_list[nCell].iIniSec;
        i64EndSec = m_AADT_Cell_list[nCell].iEndSec;
        for (k = 1, i64sectors = 0; k < 10; k++)
        {
            i64sectors += (m_i64VOBSize[k] / 2048);
            if (i64IniSec < i64sectors)
            {
                i64sectors -= (m_i64VOBSize[k] / 2048);
                nVobin = k;
                k = 20;
            }
        }
        csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
        csAux = $"{nVobin}.VOB";
        csAux = csAux2 + csAux;
        inFile = CFILE.fopen(csAux, "rb");
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
                csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
                csAux = $"{nVobin}.VOB";
                csAux = csAux2 + csAux;
                inFile = CFILE.fopen(csAux, "rb");
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

        if (m_bCheckCellt && m_bInProcess == true)
        {
            csAux = m_csOutputPath + '\\' + "Celltimes.txt";
            fout = CFILE.fopen(csAux, "w");
            nFrames = Util.DurationInFrames(m_AADT_Cell_list[nCell].dwDuration);
            if (m_bCheckEndTime)
                fout.fprintf($"{nFrames}\n");
            fout.fclose();
        }

        m_nTotalFrames = nFrames;

        if (m_bCheckLog && m_bInProcess == true) OutputLog(nCell, 1, DomainType.Titles);

        return iRet;
    }
    public virtual int CIDMDemux(int nCell, object pDlg) {
        int nTotalSectors;
        int nSector;
        int CID, VID;
        long i64IniSec, i64EndSec;
        CString csAux, csAux2;
        CFILE inFile;
        CFILE fout;
        long i64;
        bool bMyCell;
        int iRet;
        int nFrames;

        if (nCell >= m_MADT_Cell_list.GetSize())
        {
            Util.MyErrorBox("Error: Selected Cell does not exist");
            m_bInProcess = false;
            return -1;
        }

        IniDemuxGlobalVars();
        if (OpenVideoFile()) return -1;
        m_bInProcess = true;

        // Calculate  the total number of sectors
        nTotalSectors = m_MADT_Cell_list[nCell].iSize;
        nSector = 0;
        iRet = 0;

        VID = m_MADT_Cell_list[nCell].VID;
        CID = m_MADT_Cell_list[nCell].CID;

        i64IniSec = m_MADT_Cell_list[nCell].iIniSec;
        i64EndSec = m_MADT_Cell_list[nCell].iEndSec;
        if (m_bVMGM)
        {
            csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 3);
            csAux = csAux2 + "VOB";
        }
        else
        {
            csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
            csAux = csAux2 + "0.VOB";
        }
        inFile = CFILE.fopen(csAux, "rb");
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

        if (m_bCheckCellt && m_bInProcess == true)
        {
            csAux = m_csOutputPath + '\\' + "Celltimes.txt";
            fout = CFILE.fopen(csAux, "w");
            nFrames = Util.DurationInFrames(m_MADT_Cell_list[nCell].dwDuration);
            if (m_bCheckEndTime)
                fout.fprintf($"{nFrames}\n");
            fout.fclose();
        }

        m_nTotalFrames = nFrames;

        if (m_bCheckLog && m_bInProcess == true) OutputLog(nCell, 1, DomainType.Menus);

        return iRet;
    }
    public virtual void demuxvideo(Ref<byte> buffer) {
        int start, nbytes;

        start = 0x17 + buffer[0x16];
        nbytes = buffer[0x12] * 256 + buffer[0x13] + 0x14;

        Util.writebuffer(buffer.AtIndex(start), fvid, nbytes - start);
    }
    public virtual void demuxaudio(Ref<byte> buffer, int nBytesOffset) {
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
    public virtual void demuxsubs(Ref<byte> buffer) {
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
    public virtual void WritePack(Ref<byte> buffer) {
        CString csAux;

        if (m_bInProcess == true)
        {
            if (m_bCheckVob2)
            {
                if (fvob == null || m_nVidout != m_nCurrVid)
                {
                    m_nCurrVid = m_nVidout;
                    if (fvob != null) fvob.fclose();
                    if (m_iDomain ==  DomainType.Titles)
                        csAux = $"VTS_01_1_{m_nVidout:000}.VOB";
                    else
                        csAux = $"VTS_01_0_{m_nVidout:000}.VOB";
                    csAux = m_csOutputPath + '\\' + csAux;
                    fvob = CFILE.fopen(csAux, "wb");
                }
            }
            else
            {
                if (fvob == null || ((m_i64OutputLBA) % (512 * 1024 - 1)) == 0)
                {
                    if (fvob != null) fvob.fclose();
                    if (m_iDomain ==  DomainType.Titles)
                    {
                        m_nVobout++;
                        csAux = $"VTS_01_{m_nVobout}.VOB";
                    }
                    else
                        csAux = "VTS_01_0.VOB";

                    csAux = m_csOutputPath + '\\' + csAux;
                    fvob = CFILE.fopen(csAux, "wb");
                }
            }

            if (fvob != null) Util.writebuffer(buffer, fvob, 2048);
            m_i64OutputLBA++;
        }
    }
    public virtual void CloseAndNull() {
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

                    if (Util._stati64(m_csAudname[i], out statbuf))
                        i64size = statbuf.Length;

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
    public virtual int check_sub_open(byte i) {
        CString csAux;

        i -= 0x20;

        if (i > 31) return -1;

        if (fsub[i] == null)
        {
            csAux = $"Subpictures_{(i + 0x20):X2}.sup";
            csAux = m_csOutputPath + '\\' + csAux;
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
    public virtual int check_aud_open(byte i) {
        CString csAux;
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

            csAux = m_csOutputPath + '\\' + csAux;
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

    public virtual int ProcessPack(bool bWrite) {
        int sID;
        bool bFirstAud;
        int nBytesOffset;

        if (bWrite && m_bCheckVob)
        {
            if (Util.IsNav(m_buffer))
            {
                if (m_bCheckLBA) Util.ModifyLBA(m_buffer, m_i64OutputLBA);
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
            if ((Util.IsNav(m_buffer) && m_bCheckNavPack) ||
                 (Util.IsAudio(m_buffer) && m_bCheckAudioPack) ||
                 (Util.IsSubs(m_buffer) && m_bCheckSubPack))
                WritePack(m_buffer);
            else if (Util.IsVideo(m_buffer) && m_bCheckVideoPack)
            {
                if (!m_bCheckIFrame)
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
            if (bWrite && m_bCheckVid) demuxvideo(m_buffer);
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

            if (bWrite && m_bCheckAud && m_iFirstAudPTS[sID] != 0)
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
            if (bWrite && m_bCheckSub) demuxsubs(m_buffer);
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
    public virtual void OutputLog(int nItem, int nAng, DomainType iDomain) {
        /*CString csFilePath, csAux, csAux1, csAux2;
        int k;
        int AudDelay;

        csFilePath = m_csOutputPath + '\\' + "LogFile.txt";

        try
        {
            File.Delete(csFilePath);
        }
        catch (Exception)
        {
            Util.MyErrorBox("Failed to delete file.");
        }

        csAux = $"{m_nPGCs}";
        WritePrivateProfileString("General", "Total Number of PGCs   in Titles", csAux, csFilePath);
        csAux = $"{m_nMPGCs}";
        WritePrivateProfileString("General", "Total Number of PGCs   in  Menus", csAux, csFilePath);

        csAux = $"{m_AADT_Vid_list.GetSize()}";
        WritePrivateProfileString("General", "Total Number of VobIDs in Titles", csAux, csFilePath);
        csAux = $"{m_MADT_Vid_list.GetSize()}";
        WritePrivateProfileString("General", "Total Number of VobIDs in  Menus", csAux, csFilePath);

        csAux = $"{m_AADT_Cell_list.GetSize()}";
        WritePrivateProfileString("General", "Total Number of Cells  in Titles", csAux, csFilePath);
        csAux = $"{m_MADT_Cell_list.GetSize()}";
        WritePrivateProfileString("General", "Total Number of Cells  in  Menus", csAux, csFilePath);

        if (m_iMode == ModeType.PGC) csAux = "by PGC";
        else if (m_iMode == ModeType.VID) csAux = "by VOB Id";
        else if (m_iMode == ModeType.CID) csAux = "Single Cell";
        WritePrivateProfileString("General", "Demuxing   Mode", csAux, csFilePath);

        if (iDomain == DomainType.Titles) csAux = "Titles";
        else csAux = "Menus";
        WritePrivateProfileString("General", "Demuxing Domain", csAux, csFilePath);

        csAux = $"{m_nTotalFrames}";
        WritePrivateProfileString("General", "Total Number of Frames", csAux, csFilePath);

        if (m_iMode == ModeType.PGC)
        {
            csAux = $"{nItem + 1}";
            WritePrivateProfileString("General", "Selected PGC", csAux, csFilePath);

            if (iDomain == DomainType.Titles)
                csAux = $"{m_nCells[nItem]}";
            else
                csAux = $"{m_nMCells[nItem]}";

            WritePrivateProfileString("General", "Number of Cells in Selected PGC", csAux, csFilePath);
            WritePrivateProfileString("General", "Selected VOBID", "None", csFilePath);
            WritePrivateProfileString("General", "Number of Cells in Selected VOB", "None", csFilePath);

        }
        if (m_iMode == ModeType.VID)
        {
            if (iDomain == DomainType.Titles)
                csAux = $"{m_AADT_Vid_list[nItem].VID}";
            else
                csAux = $"{m_MADT_Vid_list[nItem].VID}";

            WritePrivateProfileString("General", "Selected VOBID", csAux, csFilePath);

            if (iDomain == DomainType.Titles)
                csAux = $"{m_AADT_Vid_list[nItem].nCells}";
            else
                csAux = $"{m_MADT_Vid_list[nItem].nCells}";

            WritePrivateProfileString("General", "Number of Cells in Selected VOB", csAux, csFilePath);
            WritePrivateProfileString("General", "Selected PGC", "None", csFilePath);
            WritePrivateProfileString("General", "Number of Cells in Selected PGC", "None", csFilePath);
        }
        if (m_iMode == ModeType.CID)
        {
            WritePrivateProfileString("General", "Selected VOBID", "None", csFilePath);
            WritePrivateProfileString("General", "Number of Cells in Selected VOB", "None", csFilePath);
            WritePrivateProfileString("General", "Selected PGC", "None", csFilePath);
            WritePrivateProfileString("General", "Number of Cells in Selected PGC", "None", csFilePath);
        }

        csAux = $"{m_nVidPacks}";
        WritePrivateProfileString("Demux", "Number of Video Packs", csAux, csFilePath);
        csAux = $"{m_nAudPacks}";
        WritePrivateProfileString("Demux", "Number of Audio Packs", csAux, csFilePath);
        csAux = $"{m_nSubPacks}";
        WritePrivateProfileString("Demux", "Number of Subs  Packs", csAux, csFilePath);
        csAux = $"{m_nNavPacks}";
        WritePrivateProfileString("Demux", "Number of Nav   Packs", csAux, csFilePath);
        csAux = $"{m_nPadPacks}";
        WritePrivateProfileString("Demux", "Number of Pad   Packs", csAux, csFilePath);
        csAux = $"{m_nUnkPacks}";
        WritePrivateProfileString("Demux", "Number of Unkn  Packs", csAux, csFilePath);

        for (k = 0; k < 8; k++)
        {
            csAux = $"Audio_{k + 1}";
            if (m_iFirstAudPTS[k] != 0)
                csAux1 = $"0x{m_iAudIndex[k]:X2}";
            else
                csAux1 = "None";
            WritePrivateProfileString("Audio Streams", csAux, csAux1, csFilePath);
            if (m_iFirstAudPTS[k] != 0)
            {
                //			AudDelay=m_iFirstAudPTS[k]-m_iFirstVidPTS;
                AudDelay = m_iFirstAudPTS[k] - m_iFirstNavPTS0;

                if (AudDelay < 0)
                    AudDelay -= 44;
                else
                    AudDelay += 44;
                AudDelay /= 90;
                csAux2 = $"{AudDelay}";
                WritePrivateProfileString("Audio Delays", csAux, csAux2, csFilePath);
            }
        }
        for (k = 0; k < 32; k++)
        {
            csAux = $"Subs_{(k + 1):00}";
            if (m_iFirstSubPTS[k] != 0)
                csAux1 = $"0x{(k + 0x20):X2}";
            else
                csAux1 = "None";
            WritePrivateProfileString("Subs Streams", csAux, csAux1, csFilePath);
        }*/
    }
    public virtual int InsertCell(ADT_CELL_LIST myADT_Cell, DomainType iDomain) {
        int iArraysize,i,ii = 0;
        bool bIsHigher;

        if (iDomain==DomainType.Titles)
        {
            iArraysize=m_AADT_Cell_list.GetSize();
            ii=iArraysize;
            for (i=0,bIsHigher=true; i<iArraysize && bIsHigher ; i++)
            {
                if (myADT_Cell.VID < m_AADT_Cell_list[i].VID )  {ii=i; bIsHigher=false;}
                else if (myADT_Cell.VID > m_AADT_Cell_list[i].VID )  bIsHigher=true;
                else
                {
                    if (myADT_Cell.CID < m_AADT_Cell_list[i].CID ) {ii=i; bIsHigher=false;}
                    else if (myADT_Cell.CID > m_AADT_Cell_list[i].CID )  bIsHigher=true;
                }

            }
            m_AADT_Cell_list.InsertAt(ii,myADT_Cell);
        }
        if (iDomain== DomainType.Menus)
        {
            iArraysize=m_MADT_Cell_list.GetSize();
            ii=iArraysize;
            for (i=0,bIsHigher=true; i<iArraysize && bIsHigher ; i++)
            {
                if (myADT_Cell.VID < m_MADT_Cell_list[i].VID ) {ii=i; bIsHigher=false;}
                else if (myADT_Cell.VID > m_MADT_Cell_list[i].VID )  bIsHigher=true;
                else
                {
                    if (myADT_Cell.CID < m_MADT_Cell_list[i].CID )   {ii=i; bIsHigher=false;}
                    else if (myADT_Cell.CID > m_MADT_Cell_list[i].CID )  bIsHigher=true;
                }

            }
    //		if (i>0 && bIsHigher) i--;
            m_MADT_Cell_list.InsertAt(ii,myADT_Cell);
        }
        return ii;
    }
    public virtual void FillDurations() {
        int iArraysize;
        int i,j,k;
        int VIDa,CIDa,VIDb,CIDb;
        bool bFound;
        int iVideoAttr, iFormat; 


        iArraysize=m_AADT_Cell_list.GetSize();

        for (i=0; i<iArraysize; i++)
        {
            VIDb=m_AADT_Cell_list[i].VID;
            CIDb=m_AADT_Cell_list[i].CID;
            for (j=0,bFound=false;j<m_nPGCs && !bFound; j++)
            {
                for (k=0;k<m_nCells[j];k++)
                {
                    VIDa=Util.GetNbytes(2,m_pIFO.AtIndex(m_C_POST[j]+k*4));
                    CIDa=m_pIFO[m_C_POST[j]+k*4+3];
                    if (VIDa==VIDb && CIDa==CIDb)
                    {
                        bFound=true;
                        m_AADT_Cell_list[i].dwDuration=(ulong)Util.GetNbytes(4,m_pIFO.AtIndex(m_C_PBKT[j]+0x18*k+4));
                    }
                }
            }
            if (!bFound)
            {
                iVideoAttr=m_pIFO[0x200]*256+m_pIFO[0x201];
                iFormat=(iVideoAttr & 0x1000) >> 12;
                if (iFormat == 0 ) // NTSC
                    m_AADT_Cell_list[i].dwDuration=0xC0;
                else // PAL
                    m_AADT_Cell_list[i].dwDuration=0x40;
            }
        }

        iArraysize=m_MADT_Cell_list.GetSize();

        for (i=0; i<iArraysize; i++)
        {
            VIDb=m_MADT_Cell_list[i].VID;
            CIDb=m_MADT_Cell_list[i].CID;
            for (j=0,bFound=false;j<m_nMPGCs && !bFound; j++)
            {
                for (k=0;k<m_nMCells[j];k++)
                {
                    VIDa=Util.GetNbytes(2,m_pIFO.AtIndex(m_M_C_POST[j]+k*4));
                    CIDa=m_pIFO[m_M_C_POST[j]+k*4+3];
                    if (VIDa==VIDb && CIDa==CIDb)
                    {
                        bFound=true;
                        m_MADT_Cell_list[i].dwDuration=(ulong)Util.GetNbytes(4,m_pIFO.AtIndex(m_M_C_PBKT[j]+0x18*k+4));
                    }
                }
            }
            if (!bFound)
            {
                iVideoAttr=m_pIFO[0x100]*256+m_pIFO[0x101];
                iFormat=(iVideoAttr & 0x1000) >> 12;
                if (iFormat == 0 ) // NTSC
                    m_MADT_Cell_list[i].dwDuration=0xC0;
                else // PAL
                    m_MADT_Cell_list[i].dwDuration=0x40;
            }
        }
    }
    public virtual void IniDemuxGlobalVars() {
        int k;
        CString csAux;

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
    public virtual bool OpenVideoFile() {
        CString csAux;

        if (m_bCheckVid)
        {
            csAux = m_csOutputPath + '\\' + "VideoFile.m2v";
            fvid = CFILE.fopen(csAux, "wb");
            if (fvid == null) return true;
        }

        return false;
    }
    public virtual int GetAudioDelay(ModeType iMode, int nSelection) {
        int VID = 0, CID = 0;
        int k, nCell;
        long i64IniSec, i64EndSec;
        long i64sectors;
        int nVobin = 0;
        CString csAux, csAux2;
        CFILE inFile;
        long i64;
        bool bMyCell;
        int iRet;

        IniDemuxGlobalVars();

        if (iMode == ModeType.PGC)
        {
            if (nSelection >= m_nPGCs)
            {
                Util.MyErrorBox("Error: PGC does not exist");
                return -1;
            }
            nCell = 0;
            VID = Util.GetNbytes(2, m_pIFO.AtIndex(m_C_POST[nSelection] + 4 * nCell));
            CID = m_pIFO[m_C_POST[nSelection] + 3 + 4 * nCell];
        }
        else if (iMode == ModeType.VID)
        {
            if (nSelection >= m_AADT_Vid_list.GetSize())
            {
                Util.MyErrorBox("Error: VID does not exist");
                return -1;
            }
            VID = m_AADT_Vid_list[nSelection].VID;
            CID = -1;
            for (k = 0; k < m_AADT_Cell_list.GetSize() && CID == -1; k++)
            {
                if (VID == m_AADT_Cell_list[k].VID)
                    CID = m_AADT_Cell_list[k].CID;
            }

        }
        else if (iMode == ModeType.CID)
        {
            if (nSelection >= m_AADT_Cell_list.GetSize())
            {
                Util.MyErrorBox("Error: CID does not exist");
                return -1;
            }
            VID = m_AADT_Cell_list[nSelection].VID;
            CID = m_AADT_Cell_list[nSelection].CID;
        }

        for (k = 0, nCell = -1; k < m_AADT_Cell_list.GetSize() && nCell == -1; k++)
        {
            if (VID == m_AADT_Cell_list[k].VID &&
                CID == m_AADT_Cell_list[k].CID)
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
        i64IniSec = m_AADT_Cell_list[nCell].iIniSec;
        i64EndSec = m_AADT_Cell_list[nCell].iEndSec;

        iRet = 0;
        for (k = 1, i64sectors = 0; k < 10; k++)
        {
            i64sectors += (m_i64VOBSize[k] / 2048);
            if (i64IniSec < i64sectors)
            {
                i64sectors -= (m_i64VOBSize[k] / 2048);
                nVobin = k;
                k = 20;
            }
        }
        csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
        csAux = $"{nVobin}.VOB";
        csAux = csAux2 + csAux;
        inFile = CFILE.fopen(csAux, "rb");
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
                csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
                csAux = $"{nVobin}.VOB";
                csAux = csAux2 + csAux;
                inFile = CFILE.fopen(csAux, "rb");
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
    public virtual int GetMAudioDelay(ModeType iMode, int nSelection) {
        int VID = 0, CID = 0;
        int k, nCell;
        long i64IniSec, i64EndSec;
        CString csAux, csAux2;
        CFILE inFile;
        long i64;
        bool bMyCell;
        int iRet;

        IniDemuxGlobalVars();

        if (iMode == ModeType.PGC)
        {
            if (nSelection >= m_nMPGCs)
            {
                Util.MyErrorBox("Error: PGC does not exist");
                return -1;
            }
            nCell = 0;
            VID = Util.GetNbytes(2, m_pIFO.AtIndex(m_M_C_POST[nSelection] + 4 * nCell));
            CID = m_pIFO[m_M_C_POST[nSelection] + 3 + 4 * nCell];
        }
        else if (iMode == ModeType.VID)
        {
            if (nSelection >= m_MADT_Vid_list.GetSize())
            {
                Util.MyErrorBox("Error: VID does not exist");
                return -1;
            }
            VID = m_MADT_Vid_list[nSelection].VID;
            CID = -1;
            for (k = 0; k < m_MADT_Cell_list.GetSize() && CID == -1; k++)
            {
                if (VID == m_MADT_Cell_list[k].VID)
                    CID = m_MADT_Cell_list[k].CID;
            }

        }
        else if (iMode == ModeType.CID)
        {
            if (nSelection >= m_MADT_Cell_list.GetSize())
            {
                Util.MyErrorBox("Error: CID does not exist");
                return -1;
            }
            VID = m_MADT_Cell_list[nSelection].VID;
            CID = m_MADT_Cell_list[nSelection].CID;
        }

        for (k = 0, nCell = -1; k < m_MADT_Cell_list.GetSize() && nCell == -1; k++)
        {
            if (VID == m_MADT_Cell_list[k].VID &&
                CID == m_MADT_Cell_list[k].CID)
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
        i64IniSec = m_MADT_Cell_list[nCell].iIniSec;
        i64EndSec = m_MADT_Cell_list[nCell].iEndSec;

        iRet = 0;

        if (m_bVMGM)
        {
            csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 3);
            csAux = csAux2 + "VOB";
        }
        else
        {
            csAux2 = m_csInputIFO.Left(m_csInputIFO.GetLength() - 5);
            csAux = csAux2 + "0.VOB";
        }
        inFile = CFILE.fopen(csAux, "rb");
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
    public virtual int GetAudHeader(Ref<byte> buffer) {
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