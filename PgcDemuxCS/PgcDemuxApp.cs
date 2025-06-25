using PgcDemuxCS;
using PgcDemuxCS.DVD;
using PgcDemuxCS.DVD.IfoTypes.Common;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace PgcDemuxCS
{
    internal class PgcDemuxApp
    {
        public const string PGCDEMUX_VERSION = "1.2.0.5";
        internal const int MODUPDATE = 100;
        internal const int MAXLOOKFORAUDIO = 10000;
        internal IIfoFileReader FileReader;
        Action<double>? progressCallback = null;

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

        internal PgcDemuxApp(IIfoFileReader reader, PgcDemuxOptions options)
        {
            FileReader = reader;

            int i, k;
            int nSelVid;


            //	SetRegistryKey( "jsoto's tools" );
            //	WriteProfileInt("MySection", "My Key",123);

            for (i = 0; i < 32; i++) fsub[i] = null;
            for (i = 0; i < 8; i++)
                faud[i] = null;
            fvob = fvid = null;

            this.Options = options;
            this.Options.VerifyInputs();

            this.FileInfo = new IfoData(FileReader, this.Options); // Read IFO data
        }

        internal bool Run(Action<double>? progressCallback = null)
        {
            this.progressCallback = progressCallback;
            if (Options.m_iMode == ModeType.PGC)
            {
                // Check if PGC exists is done in PgcDemux
                return PgcDemux(Options.m_nSelPGC, null, (Options.m_iDomain == DomainType.Menus) ? FileInfo.MenuInfo : FileInfo.TitleInfo);
            }
            if (Options.m_iMode == ModeType.VID)
            {
                // Look for nSelVid
                int nSelVid = -1;
                DomainInfo domainInfo = (Options.m_iDomain == DomainType.Titles) ? FileInfo.TitleInfo : FileInfo.MenuInfo;

                for (int k = 0; k < domainInfo.m_AADT_Vid_list.GetSize(); k++)
                {
                    if (domainInfo.m_AADT_Vid_list[k].VID == Options.m_nVid)
                    {
                        nSelVid = k;
                        break;
                    }
                }

                if (nSelVid == -1)
                {
                    Util.MyErrorBox("Selected Vid not found!");
                    return false;
                }

                return VIDDemux(nSelVid, null, (Options.m_iDomain == DomainType.Menus) ? FileInfo.MenuInfo : FileInfo.TitleInfo);

            }
            if (Options.m_iMode == ModeType.CID)
            {
                // Look for nSelVid
                CellID nSelCid = new();
                if (Options.m_iDomain == DomainType.Titles)
                {
                    nSelCid = GetAllCells(FileInfo.TitleInfo).SelectCID(Options.m_nVid, Options.m_nCid).FirstOrDefault(nSelCid);
                }
                else
                {
                    nSelCid = GetAllCells(FileInfo.MenuInfo).SelectCID(Options.m_nVid, Options.m_nCid).FirstOrDefault(nSelCid);
                }
                if (nSelCid.Index == -1)
                {
                    Util.MyErrorBox("Selected Vid/Cid not found!");
                    return false;
                }

                return CIDDemux(nSelCid, null, (Options.m_iDomain == DomainType.Menus) ? FileInfo.MenuInfo : FileInfo.TitleInfo);

            }

            return false;
        }


        public Ref<byte> m_buffer = new ByteArrayRef(new byte[2050], 0);

        public int nResponse;

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
        private IfoData FileInfo;

        public virtual void UpdateProgress(object pDlg, double nPerc)
        {
            progressCallback?.Invoke(nPerc);
        }

        public long GetPgcBytes()
        {
            int nPGC = Options.m_nSelPGC;
            DomainInfo domainInfo = (Options.m_iDomain == DomainType.Menus) ? FileInfo.MenuInfo : FileInfo.TitleInfo;
            if (nPGC >= domainInfo.m_nPGCs)
            {
                Util.MyErrorBox("Error: PGC does not exist");
                return 0;
            }

            IniDemuxGlobalVars();

            // Calculate  the total number of sectors
            var iArraysize = domainInfo.m_AADT_Cell_list.GetSize();
            IEnumerable<CellID> cells = GetPgcCells(nPGC, domainInfo);
            if (Options.m_iDomain == DomainType.Titles)
            {
                cells = cells.SelectAngle(nPGC, Options.m_nSelAng, domainInfo);
            }

            return cells.Sum(x => x.Size) * DvdUtils.DVD_BLOCK_LEN;
        }

        public bool PgcDemux(int nPGC, object pDlg, DomainInfo domainInfo)
        {
            int nTotalSectors;
            int nSector;
            int iArraysize;
            string csAux;
            VobStream vobStream = new VobStream(FileInfo, FileReader, Options.m_iDomain);
            CFILE fout;
            long i64;
            bool bMyCell;
            int nFrames;

            if (nPGC >= domainInfo.m_nPGCs)
            {
                Util.MyErrorBox("Error: PGC does not exist");
                return false;
            }

            IniDemuxGlobalVars();
            if (OpenVideoFile()) return false;

            // Calculate  the total number of sectors
            iArraysize = domainInfo.m_AADT_Cell_list.GetSize();
            IEnumerable<CellID> cells = GetPgcCells(nPGC, domainInfo);
            if (Options.m_iDomain == DomainType.Titles)
            {
                cells = cells.SelectAngle(nPGC, Options.m_nSelAng, domainInfo);
            }

            nTotalSectors = cells.Sum(x => x.Size);

            nSector = 0;

            foreach (var cell in cells)
            {
                vobStream.Seek(cell.StartSector * 2048, SeekOrigin.Begin);

                for (i64 = 0, bMyCell = true; i64 < (cell.EndSector - cell.StartSector + 1); i64++)
                {
                    //readpack
                    if ((i64 % MODUPDATE) == 0) UpdateProgress(pDlg, ((double)nSector / nTotalSectors));
                    if (m_buffer.ReadFromStream(vobStream, 2048) != 2048)
                    {
                        Util.MyErrorBox("Input error: Reached end of VOB too early");
                        return false;
                    }

                    if (Util.IsSynch(m_buffer) != true)
                    {
                        Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                        return false;
                    }
                    if (Util.IsNav(m_buffer))
                    {
                        if (m_buffer[0x420] == (byte)(cell.VID % 256) &&
                            m_buffer[0x41F] == (byte)(cell.VID / 256) &&
                            m_buffer[0x422] == (byte)cell.CID)
                            bMyCell = true;
                        else
                            bMyCell = false;
                    }

                    if (bMyCell)
                    {
                        nSector++;
                        if (!ProcessPack(true))
                        {
                            return false;
                        }
                    }

                } // For readpacks
            }

            if (vobStream != null) vobStream.Close();
            vobStream = null;

            CloseAndNull();

            nFrames = 0;

            if (Options.m_bCheckCellt)
            {
                csAux = Path.Combine(Options.m_csOutputPath, "Celltimes.txt");
                fout = CFILE.OpenWrite(csAux);
                foreach (var cell in cells)
                {
                    nFrames += Util.DurationInFrames(cell.Duration, 25);
                    if (cell.Index != (domainInfo.m_nCells[nPGC] - 1) || Options.m_bCheckEndTime)
                        fout.fprintf($"{nFrames}\n");
                }
                fout.fclose();
            }

            m_nTotalFrames = nFrames;

            if (Options.m_bCheckLog) OutputLog(nPGC, Options.m_nSelAng, Options.m_iDomain);

            return true;
        }

        public long GetVidBytes()
        {
            // Look for nSelVid
            int nSelVid = -1;
            DomainInfo domainInfo = (Options.m_iDomain == DomainType.Titles) ? FileInfo.TitleInfo : FileInfo.MenuInfo;

            for (int k = 0; k < domainInfo.m_AADT_Vid_list.GetSize(); k++)
            {
                if (domainInfo.m_AADT_Vid_list[k].VID == Options.m_nVid)
                {
                    nSelVid = k;
                    break;
                }
            }

            if (nSelVid == -1)
            {
                Util.MyErrorBox("Selected Vid not found!");
                return 0;
            }

            var nVid = nSelVid;

            if (nVid >= domainInfo.m_AADT_Vid_list.GetSize())
            {
                Util.MyErrorBox("Error: Selected Vid does not exist");
                return 0;
            }

            IniDemuxGlobalVars();

            int DemuxedVID = domainInfo.m_AADT_Vid_list[nVid].VID; // TODO why?? is the input the menu index, not actual VID??
            IEnumerable<CellID> cells = GetAllCells(domainInfo).SelectVID(DemuxedVID);

            // Calculate  the total number of sectors
            return domainInfo.m_AADT_Vid_list[nVid].iSize;
        }

        public bool VIDDemux(int nVid, object pDlg, DomainInfo domainInfo)
        {
            int nTotalSectors;
            int nSector;
            string csAux;
            VobStream vobStream = new VobStream(FileInfo, FileReader, Options.m_iDomain);
            CFILE fout;
            long i64;
            bool bMyCell;
            int nFrames;

            if (nVid >= domainInfo.m_AADT_Vid_list.GetSize())
            {
                Util.MyErrorBox("Error: Selected Vid does not exist");
                return false;
            }

            IniDemuxGlobalVars();
            if (OpenVideoFile()) return false;

            int DemuxedVID = domainInfo.m_AADT_Vid_list[nVid].VID; // TODO why?? is the input the menu index, not actual VID??
            IEnumerable<CellID> cells = GetAllCells(domainInfo).SelectVID(DemuxedVID);

            // Calculate  the total number of sectors
            nTotalSectors = domainInfo.m_AADT_Vid_list[nVid].iSize;
            nSector = 0;


            foreach (var cell in cells)
            {
                vobStream.Seek((long)(cell.StartSector * 2048), SeekOrigin.Begin);
                for (i64 = 0, bMyCell = true; i64 < (cell.EndSector - cell.StartSector + 1); i64++)
                {
                    //readpack
                    if ((i64 % MODUPDATE) == 0) UpdateProgress(pDlg, ((double)nSector / nTotalSectors));
                    if (m_buffer.ReadFromStream(vobStream, 2048) != 2048)
                    {

                        Util.MyErrorBox("Input error: Reached end of VOB too early");
                        return false;
                    }

                    if (Util.IsSynch(m_buffer) != true)
                    {
                        Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                        return false;
                    }
                    if (Util.IsNav(m_buffer))
                    {
                        if (m_buffer[0x420] == (byte)(cell.VID % 256) &&
                            m_buffer[0x41F] == (byte)(cell.VID / 256) &&
                            m_buffer[0x422] == (byte)cell.CID)
                            bMyCell = true;
                        else
                            bMyCell = false;
                    }

                    if (bMyCell)
                    {
                        nSector++;
                        if (!ProcessPack(true))
                        {
                            return false;
                        }
                    }

                } // For readpacks
            }

            vobStream.Close();
            vobStream = null;
            CloseAndNull();
            nFrames = 0;

            if (Options.m_bCheckCellt)
            {
                csAux = Path.Combine(Options.m_csOutputPath, "Celltimes.txt");
                fout = CFILE.OpenWrite(csAux);

                CellID nLastCell = cells.Last();

                foreach (var cell in cells)
                {
                    nFrames += Util.DurationInFrames(cell.Duration, 25);
                    if (cell.Index != nLastCell.Index || Options.m_bCheckEndTime)
                        fout.fprintf($"{nFrames}\n");
                }
                fout.fclose();
            }

            m_nTotalFrames = nFrames;

            if (Options.m_bCheckLog) OutputLog(nVid, 1, Options.m_iDomain);

            return true;
        }

        public long GetCidBytes()
        {
            // Look for nSelVid
            CellID nSelCid = new();
            if (Options.m_iDomain == DomainType.Titles)
            {
                nSelCid = GetAllCells(FileInfo.TitleInfo).SelectCID(Options.m_nVid, Options.m_nCid).FirstOrDefault(nSelCid);
            }
            else
            {
                nSelCid = GetAllCells(FileInfo.MenuInfo).SelectCID(Options.m_nVid, Options.m_nCid).FirstOrDefault(nSelCid);
            }
            if (nSelCid.Index == -1)
            {
                Util.MyErrorBox("Selected Vid/Cid not found!");
                return 0;
            }

            CellID nCell = nSelCid;
            DomainInfo domainInfo = (Options.m_iDomain == DomainType.Menus) ? FileInfo.MenuInfo : FileInfo.TitleInfo;

            if (nCell.Index >= domainInfo.m_AADT_Cell_list.GetSize())
            {
                Util.MyErrorBox("Error: Selected Cell does not exist");
                return 0;
            }

            IniDemuxGlobalVars();

            return nCell.Size;
        }

        public bool CIDDemux(CellID nCell, object pDlg, DomainInfo domainInfo)
        {
            int nSector;
            int k;
            int nVobin = 0;
            string csAux, csAux2;
            VobStream vobStream = new VobStream(FileInfo, FileReader, Options.m_iDomain);
            CFILE fout;
            long i64;
            bool bMyCell;
            int nFrames;

            if (nCell.Index >= domainInfo.m_AADT_Cell_list.GetSize())
            {
                Util.MyErrorBox("Error: Selected Cell does not exist");
                return false;
            }

            IniDemuxGlobalVars();
            if (OpenVideoFile()) return false;

            // Calculate  the total number of sectors
            nSector = 0;

            vobStream.Seek((long)(nCell.StartSector * 2048), SeekOrigin.Begin);
            for (i64 = 0, bMyCell = true; i64 < (nCell.EndSector - nCell.StartSector + 1); i64++)
            {
                //readpack
                if ((i64 % MODUPDATE) == 0) UpdateProgress(pDlg, ((double)nSector / nCell.Size));
                if (m_buffer.ReadFromStream(vobStream, 2048) != 2048)
                {
                    Util.MyErrorBox("Input error: Reached end of VOB too early");
                    return false;
                }


                if (Util.IsSynch(m_buffer) != true)
                {
                    Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                    return false;
                }
                if (Util.IsNav(m_buffer))
                {
                    if (m_buffer[0x420] == (byte)(nCell.VID % 256) &&
                        m_buffer[0x41F] == (byte)(nCell.VID / 256) &&
                        m_buffer[0x422] == (byte)nCell.CID)
                        bMyCell = true;
                    else
                        bMyCell = false;
                }

                if (bMyCell)
                {
                    nSector++;
                    if (!ProcessPack(true))
                    {
                        return false;
                    }
                }
            } // For readpacks

            vobStream.Close();
            vobStream = null;
            CloseAndNull();

            nFrames = 0;

            if (Options.m_bCheckCellt)
            {
                csAux = Path.Combine(Options.m_csOutputPath, "Celltimes.txt");
                fout = CFILE.OpenWrite(csAux);
                nFrames = Util.DurationInFrames(nCell.Duration, 25);
                if (Options.m_bCheckEndTime)
                    fout.fprintf($"{nFrames}\n");
                fout.fclose();
            }

            m_nTotalFrames = nFrames;

            if (Options.m_bCheckLog) OutputLog(nCell.Index, 1, Options.m_iDomain);

            return true;
        }
        public void demuxvideo(Ref<byte> buffer)
        {
            int start, nbytes;

            start = 0x17 + buffer[0x16];
            nbytes = buffer[0x12] * 256 + buffer[0x13] + 0x14;

            Util.writebuffer(buffer.AtIndex(start), fvid, nbytes - start);
        }
        public void demuxaudio(Ref<byte> buffer, int nBytesOffset)
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

            if (streamID != Options.m_bCheckAud)
            {
                return;
            }

            // Open File descriptor if it isn't open
            if (check_aud_open(streamID) == false)
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
        public void demuxsubs(Ref<byte> buffer)
        {
            int start, nbytes;
            byte streamID;
            int k;
            Ref<byte> mybuff = new ByteArrayRef(new byte[10], 0);
            int iPTS;

            start = 0x17 + buffer[0x16];
            nbytes = buffer[0x12] * 256 + buffer[0x13] + 0x14;
            streamID = buffer[start];

            if (streamID != Options.m_bCheckSub)
            {
                return;
            }

            if (check_sub_open(streamID) == false)
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
        public bool WritePack(Ref<byte> buffer)
        {
            string csAux;

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
                    csAux = Path.Combine(Options.m_csOutputPath, csAux);
                    fvob = CFILE.OpenWrite(csAux);
                    if (fvob == null) return false;
                }
            }
            else
            {
                if (fvob == null/* || ((m_i64OutputLBA) % (512 * 1024 - 1)) == 0*/)
                {
                    if (fvob != null) fvob.fclose();
                    fvob = CFILE.OpenWrite(Options.m_csOutputPath);
                    if (fvob == null) return false;
                }
            }

            if (fvob != null) Util.writebuffer(buffer, fvob, 2048);
            m_i64OutputLBA++;
            return true;
        }
        public void CloseAndNull()
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

                        faud[i] = CFILE.OpenReadWrite(m_csAudname[i]);

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
        public bool check_sub_open(byte i)
        {
            string csAux;

            i -= 0x20;

            if (i > 31) return false;

            if (fsub[i] == null)
            {
                //csAux = $"Subpictures_{(i + 0x20):X2}.sup";
                //csAux = Path.Combine(Options.m_csOutputPath, csAux);
                if ((fsub[i] = CFILE.OpenWrite(Options.m_csOutputPath)) == null)
                {
                    Util.MyErrorBox("Error opening output subs file:" + Options.m_csOutputPath);
                    return false;
                }
                else return true;
            }
            else
                return true;
        }
        public bool check_aud_open(byte i)
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

            if (ii < 0x80) return false;

            i = (byte)(i & 0x7);

            if (faud[i] == null)
            {
                /*if (ii >= 0x80 && ii <= 0x87)
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

                csAux = Path.Combine(Options.m_csOutputPath, csAux);
                m_csAudname[i] = csAux;*/

                if ((faud[i] = CFILE.OpenWrite(Options.m_csOutputPath)) == null)
                {
                    Util.MyErrorBox("Error opening output audio file:" + Options.m_csOutputPath);
                    return false;
                }

                if (m_audfmt[i] == AudioFormat.WAV)
                {
                    faud[i].fwrite(pcmheader, sizeof(byte), 44);
                }
            }

            return true;
        }

        private static int nPack = 0;
        private static int nFirstRef = 0;

        public bool ProcessPack(bool bWrite)
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
                {
                    if (!WritePack(m_buffer)) return false;
                }
                else if (Util.IsVideo(m_buffer) && Options.m_bCheckVideoPack)
                {
                    if (!Options.m_bCheckIFrame)
                    {
                        if (!WritePack(m_buffer)) return false;
                    }
                    else
                    {
                        //				if (nFirstRef == nPack)  
                        //					if ( ! PatchEndOfSequence(m_buffer))
                        //						WritePack (Pad_pack);
                        if (bNewCell && nFirstRef >= nPack)
                        {
                            if (!WritePack(m_buffer)) return false;
                        }
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

                if (bWrite && (Options.m_bCheckAud >= 0) && m_iFirstAudPTS[sID] != 0)
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
                if (bWrite && (Options.m_bCheckSub >= 0)) demuxsubs(m_buffer);
            }
            else if (Util.IsPad(m_buffer))
            {
                m_nPadPacks++;
            }
            else
            {
                m_nUnkPacks++;
            }
            return true;
        }
        public void OutputLog(int nItem, int nAng, DomainType iDomain)
        {

        }

        public void IniDemuxGlobalVars()
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
        public bool OpenVideoFile()
        {
            string csAux;

            if (Options.m_bCheckVid)
            {
                fvid = CFILE.OpenWrite(Options.m_csOutputPath);
                if (fvid == null) return true;
            }

            return false;
        }
        public bool GetAudioDelay(ModeType iMode, int nSelection, DomainInfo domainInfo)
        {
            int VID = 0, CID = 0;
            int k, nCell;
            long i64IniSec, i64EndSec;
            long i64sectors;
            int nVobin = 0;
            string csAux, csAux2;
            VobStream inFile = new VobStream(FileInfo, FileReader, Options.m_iDomain);
            long i64;
            bool bMyCell;

            IniDemuxGlobalVars();

            if (iMode == ModeType.PGC)
            {
                if (nSelection >= domainInfo.m_nPGCs)
                {
                    Util.MyErrorBox("Error: PGC does not exist");
                    return false;
                }
                nCell = 0;
                VID = domainInfo.m_C_POST[nSelection][nCell].VobID;
                CID = domainInfo.m_C_POST[nSelection][nCell].CellID;
            }
            else if (iMode == ModeType.VID)
            {
                if (nSelection >= domainInfo.m_AADT_Vid_list.GetSize())
                {
                    Util.MyErrorBox("Error: VID does not exist");
                    return false;
                }
                VID = domainInfo.m_AADT_Vid_list[nSelection].VID;
                CID = -1;
                for (k = 0; k < domainInfo.m_AADT_Cell_list.GetSize() && CID == -1; k++)
                {
                    if (VID == domainInfo.m_AADT_Cell_list[k].VID)
                        CID = domainInfo.m_AADT_Cell_list[k].CID;
                }

            }
            else if (iMode == ModeType.CID)
            {
                if (nSelection >= domainInfo.m_AADT_Cell_list.GetSize())
                {
                    Util.MyErrorBox("Error: CID does not exist");
                    return false;
                }
                VID = domainInfo.m_AADT_Cell_list[nSelection].VID;
                CID = domainInfo.m_AADT_Cell_list[nSelection].CID;
            }

            for (k = 0, nCell = -1; k < domainInfo.m_AADT_Cell_list.GetSize() && nCell == -1; k++)
            {
                if (VID == domainInfo.m_AADT_Cell_list[k].VID &&
                    CID == domainInfo.m_AADT_Cell_list[k].CID)
                    nCell = k;
            }

            if (nCell < 0)
            {
                Util.MyErrorBox("Error: VID/CID not found!.");
                return false;
            }
            //
            // Now we have VID; CID; and the index in Cell Array "nCell".
            // So we are going to open the VOB and read the delays using ProcessPack(false)
            i64IniSec = domainInfo.m_AADT_Cell_list[nCell].iIniSec;
            i64EndSec = domainInfo.m_AADT_Cell_list[nCell].iEndSec;

            inFile.Seek((long)(i64IniSec * 2048), SeekOrigin.Begin);

            for (i64 = 0, bMyCell = true; i64 < (i64EndSec - i64IniSec + 1) && i64 < MAXLOOKFORAUDIO; i64++)
            {
                //readpack
                if (m_buffer.ReadFromStream(inFile, 2048) != 2048)
                {
                    Util.MyErrorBox("Input error: Reached end of VOB too early");
                    return false;
                }

                if (Util.IsSynch(m_buffer) != true)
                {
                    Util.MyErrorBox("Error reading input VOB: Unsynchronized");
                    return false;
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
                    if (!ProcessPack(false))
                    {
                        return false;
                    }
                }
            } // For readpacks

            return true;
        }

        // Returns the number of bytes from audio start until first header
        // If no header found  returns -1
        public int GetAudHeader(Ref<byte> buffer)
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

        private IEnumerable<CellID> GetPgcCells(int nPGC, DomainInfo domainInfo)
        {
            for (int nCell = 0; nCell < domainInfo.m_nCells[nPGC]; nCell++)
            {
                int VID = domainInfo.m_C_POST[nPGC][nCell].VobID;
                int CID = domainInfo.m_C_POST[nPGC][nCell].CellID;
                long iIniSec = domainInfo.m_C_PBKT[nPGC][nCell].FirstSector;
                long iEndSec = domainInfo.m_C_PBKT[nPGC][nCell].LastSector;
                int iSize = GetAllCells(domainInfo).SelectCID(VID, CID).Sum(x => x.Size);
                TimeSpan dwDuration = domainInfo.m_C_PBKT[nPGC][nCell].PlaybackTime;
                yield return new CellID(nCell, VID, CID, iSize, iIniSec, iEndSec, dwDuration);
            }
        }

        private IEnumerable<CellID> GetAllCells(DomainInfo domainInfo)
        {
            int iArraySize = domainInfo.m_AADT_Cell_list.GetSize();
            for (int nCell = 0; nCell < iArraySize; nCell++)
            {
                yield return new CellID(nCell, domainInfo.m_AADT_Cell_list[nCell]);
            }
        }
    }

    internal static class PgcDemuxExtensions
    {
        // TODO only run on titles/PGC
        public static IEnumerable<CellID> SelectAngle(this IEnumerable<CellID> cells, int nPGC, int angle, DomainInfo domainInfo)
        {
            int nCurrAngle = 0;
            foreach (CellID cell in cells)
            {
                // Check angle info
                var cellInfo = domainInfo.m_C_PBKT[nPGC][cell.Index];

                if (cellInfo.IsFirstAngle)
                    nCurrAngle = 1;
                else if ((cellInfo.IsMiddleAngle || cellInfo.IsLastAngle) && nCurrAngle != 0)
                    nCurrAngle++;

                if (cellInfo.IsNormal || (angle + 1) == nCurrAngle)
                {
                    yield return cell;
                }
                if (cellInfo.IsLastAngle) nCurrAngle = 0;
            }
        }

        public static IEnumerable<CellID> SelectVID(this IEnumerable<CellID> cells, int VID)
        {
            foreach (CellID cell in cells)
            {
                if (cell.VID == VID)
                {
                    yield return cell;
                }
            }
        }

        public static IEnumerable<CellID> SelectCID(this IEnumerable<CellID> cells, int VID, int CID)
        {
            foreach (CellID cell in cells)
            {
                if (cell.VID == VID && cell.CID == CID)
                {
                    yield return cell;
                }
            }
        }
    }

    internal struct CellID
    {
        public int Index;
        public int VID;
        public int CID;
        public int Size;
        public long StartSector;
        public long EndSector;
        public TimeSpan Duration;
        public int Angle = 1;

        public CellID()
        {
            Index = -1;
            VID = -1;
            CID = -1;
            Size = 0;
            StartSector = 0;
            EndSector = 0;
            Duration = TimeSpan.Zero;
        }

        public CellID(int index, int vid, int cid, int size, long startSec, long endSec, TimeSpan duration)
        {
            this.Index = index;
            this.VID = vid;
            this.CID = cid;
            this.Size = size;
            this.StartSector = startSec;
            this.EndSector = endSec;
            this.Duration = duration;
        }

        public CellID(int index, ADT_CELL_LIST cell)
        {
            this.Index = index;
            this.VID = cell.VID;
            this.CID = cell.CID;
            this.Size = cell.iSize;
            this.StartSector = cell.iIniSec;
            this.EndSector = cell.iEndSec;
            this.Duration = cell.dwDuration;
        }
    }
}