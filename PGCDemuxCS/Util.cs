internal static class Util
{
    public static void MyErrorBox(string text)
    {
        // TODO error message
        //AfxMessageBox(text, MB_OK | MB_ICONEXCLAMATION, 0);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void MyInfoBox(string text)
    {
        // TODO info message
        //AfxMessageBox(text, MB_OK | MB_ICONINFORMATION, 0);
        Console.WriteLine(text);
    }

    public static int readpts(Ref<byte> buf)
    {
        int a1, a2, a3;
        int pts;

        a1 = (buf[0] & 0xe) >> 1;
        a2 = ((buf[1] << 8) | buf[2]) >> 1;
        a3 = ((buf[3] << 8) | buf[4]) >> 1;
        pts = (int)(((long)a1 << 30) | ((long)a2 << 15) | (long)a3);
        return pts;
    }

    public static CString Size_MBytes(long i64size)
    {
        CString csAux;

        if (i64size < 9999)
            csAux = $"{i64size} By";
        else if (i64size < 9999 * 1024)
            csAux = $"{i64size / 1024} KB";
        else if (i64size < (long)9999 * 1024 * 1024)
            csAux = $"{i64size / 1024 / 1024} MB";
        else
            csAux = $"{i64size / 1024 / 1024 / 1024} GB";

        return csAux;
    }

    public static CString FormatDuration(ulong duration)
    {
        CString csAux;

        if (duration < 0)
            csAux = "Unknown";
        else
        {
            var arg1 = duration / (256 * 256 * 256);
            var arg2 = (duration / (256 * 256)) % 256;
            var arg3 = (duration / 256) % 256;
            var arg4 = (duration % 256) & 0x3f;
            csAux = $"{arg1:X2}:{arg2:X2}:{arg3:X2}.{arg4:X2}";
        }
        return csAux;
    }

    public static int GetNbytes(int nNumber, Ref<byte> address)
    {
        int ret, i;

        for (i = ret = 0; i < nNumber; i++)
            ret = ret * 256 + address[i];
        return ret;
    }

    public static void Put4bytes(long i64Number, Ref<byte> address)
    {

        address[0] = (byte)(i64Number / (256 * 256 * 256));
        address[1] = (byte)((i64Number / (256 * 256)) % 256);
        address[2] = (byte)((i64Number / 256) % 256);
        address[3] = (byte)(i64Number % 256);

    }

    public static void Put2bytes(long i64Number, Ref<byte> address)
    {

        address[0] = (byte)((i64Number / 256) % 256);
        address[1] = (byte)(i64Number % 256);

    }

    public static int BCD2Dec(int BCD)
    {
        int ret;
        ret = (BCD / 0x10) * 10 + (BCD % 0x10);
        return ret;
    }

    public static int Dec2BCD(int Dec)
    {
        int ret;
        ret = (Dec / 10) * 0x10 + (Dec % 10);
        return ret;
    }

    public static int DurationInFrames(ulong dwDuration)
    {

        int ifps, ret;
        long i64Dur;

        if (((dwDuration % 256) & 0x0c0) == 0x0c0)
            ifps = 30;
        else
            ifps = 25;

        i64Dur = BCD2Dec((int)((dwDuration % 256) & 0x3f));
        i64Dur += BCD2Dec((int)((dwDuration / 256) % 256)) * ifps;
        i64Dur += BCD2Dec((int)((dwDuration / (256 * 256)) % 256)) * ifps * 60;
        i64Dur += BCD2Dec((int)(dwDuration / (256 * 256 * 256))) * ifps * 60 * 60;

        ret = (int)(i64Dur);

        return ret;
    }

    public static int DurationInSecs(ulong dwDuration)
    {

        int ifps, ret;
        long i64Dur;

        if (((dwDuration % 256) & 0x0c0) == 0x0c0)
            ifps = 30;
        else
            ifps = 25;

        i64Dur = BCD2Dec((int)((dwDuration % 256) & 0x3f));
        i64Dur += BCD2Dec((int)((dwDuration / 256) % 256)) * ifps;
        i64Dur += BCD2Dec((int)((dwDuration / (256 * 256)) % 256)) * ifps * 60;
        i64Dur += BCD2Dec((int)(dwDuration / (256 * 256 * 256))) * ifps * 60 * 60;

        ret = (int)(i64Dur / ifps);

        return ret;
    }

    public static ulong AddDuration(ulong dwDuration1, ulong dwDuration2)
    {
        ulong ret;
        int ifps, hh, mm, ss, ff;
        long i64Dur1, i64Dur2, i64DurT;

        if (((dwDuration1 % 256) & 0x0c0) == 0x0c0)
            ifps = 30;
        else
            ifps = 25;

        i64Dur1 = BCD2Dec((int)((dwDuration1 % 256) & 0x3f));
        i64Dur1 += BCD2Dec((int)((dwDuration1 / 256) % 256)) * ifps;
        i64Dur1 += BCD2Dec((int)((dwDuration1 / (256 * 256)) % 256)) * ifps * 60;
        i64Dur1 += BCD2Dec((int)(dwDuration1 / (256 * 256 * 256))) * ifps * 60 * 60;

        i64Dur2 = BCD2Dec((int)((dwDuration2 % 256) & 0x3f));
        i64Dur2 += BCD2Dec((int)((dwDuration2 / 256) % 256)) * ifps;
        i64Dur2 += BCD2Dec((int)((dwDuration2 / (256 * 256)) % 256)) * ifps * 60;
        i64Dur2 += BCD2Dec((int)(dwDuration2 / (256 * 256 * 256))) * ifps * 60 * 60;

        i64DurT = i64Dur2 + i64Dur1;

        ff = Dec2BCD((int)(i64DurT % ifps));
        ss = Dec2BCD((int)((i64DurT / ifps) % 60));
        mm = Dec2BCD((int)((i64DurT / ifps / 60) % 60));
        hh = Dec2BCD((int)(i64DurT / ifps / 60 / 60));

        ret = (ulong)ff + (ulong)ss * 256 + (ulong)mm * 256 * 256 + (ulong)hh * 256 * 256 * 256;

        if (ifps == 30)
            ret += 0x0c0;
        else
            ret += 0x040;

        return ret;
    }

    public static ulong SubDuration(ulong dwDuration1, ulong dwDuration2)
    {
        ulong ret;
        int ifps, hh, mm, ss, ff;
        long i64Dur1, i64Dur2, i64DurT;

        if (((dwDuration1 % 256) & 0x0c0) == 0x0c0)
            ifps = 30;
        else
            ifps = 25;

        i64Dur1 = BCD2Dec((int)((dwDuration1 % 256) & 0x3f));
        i64Dur1 += BCD2Dec((int)((dwDuration1 / 256) % 256)) * ifps;
        i64Dur1 += BCD2Dec((int)((dwDuration1 / (256 * 256)) % 256)) * ifps * 60;
        i64Dur1 += BCD2Dec((int)(dwDuration1 / (256 * 256 * 256))) * ifps * 60 * 60;

        i64Dur2 = BCD2Dec((int)((dwDuration2 % 256) & 0x3f));
        i64Dur2 += BCD2Dec((int)((dwDuration2 / 256) % 256)) * ifps;
        i64Dur2 += BCD2Dec((int)((dwDuration2 / (256 * 256)) % 256)) * ifps * 60;
        i64Dur2 += BCD2Dec((int)(dwDuration2 / (256 * 256 * 256))) * ifps * 60 * 60;

        i64DurT = i64Dur1 - i64Dur2;

        ff = Dec2BCD((int)(i64DurT % ifps));
        ss = Dec2BCD((int)((i64DurT / ifps) % 60));
        mm = Dec2BCD((int)((i64DurT / ifps / 60) % 60));
        hh = Dec2BCD((int)(i64DurT / ifps / 60 / 60));

        ret = (ulong)ff + (ulong)ss * 256 + (ulong)mm * 256 * 256 + (ulong)hh * 256 * 256 * 256;

        if (ifps == 30)
            ret += 0x0c0;
        else
            ret += 0x040;

        return ret;
    }

    // Sorry, a dirty trick to pass the folder 
    private static CString csGlobalStartFolder;

    public static int readbuffer(Ref<byte> caracter, CFILE fin)
    {
        int j;

        if (fin == null) return -1;
        j = fin.fread(caracter, sizeof(byte), 2048);

        return j;
    }


    public static void writebuffer(Ref<byte> caracter, CFILE fout, int nbytes)
    {
        fout.fwrite(caracter, sizeof(byte), nbytes);
    }

    public static bool IsPad(Ref<byte> buffer)
    {

        int startcode;

        startcode = GetNbytes(4, buffer.AtIndex(14));

        if (startcode == 446) return true;
        else return false;

    }

    public static bool IsNav(Ref<byte> buffer)
    {

        int startcode;

        startcode = GetNbytes(4, buffer.AtIndex(14));

        if (startcode == 443) return true;
        else return false;

    }

    public static bool IsVideo(Ref<byte> buffer)
    {

        int startcode;

        startcode = GetNbytes(4, buffer.AtIndex(14));

        if (startcode == 480) return true;
        else return false;

    }

    public static bool IsGOP(Ref<byte> buffer)
    {

        int startcode, startmpeg;

        startmpeg = 0x17 + buffer[0x16];
        startcode = GetNbytes(4, buffer.AtIndex(startmpeg));

        if (startcode == 0x1b3)
        {
            startcode = GetNbytes(4, buffer.AtIndex(startmpeg + 0x58));
            if (startcode == 0x1b8)
                return true;
        }

        return false;
    }

    public static bool IsClosedGOP(Ref<byte> buffer)
    {

        int startcode, startmpeg;

        startmpeg = 0x17 + buffer[0x16];
        startcode = GetNbytes(4, buffer.AtIndex(startmpeg));

        if (startcode == 0x1b3)
        {
            startcode = GetNbytes(4, buffer.AtIndex(startmpeg + 0x58));
            if (startcode == 0x1b8)
            {
                if ((buffer[startmpeg + 0x5f] & 0x40) != 0)
                    return true;
            }
        }
        return false;
    }

    public static bool IsAudio(Ref<byte> buffer)
    {
        int startcode, st_i;

        startcode = GetNbytes(4, buffer.AtIndex(14));
        st_i = 0x17 + buffer[0x16];
        /*
        0x80-0x87: ac3
        0x88-0x8f: dts
        0x90-0x97: dds
        0x98-0x9f: unknown
        0xa0-0xa7: lpcm

        --------------------------------------------------------------------------------
        SDSS   AC3   DTS   LPCM   MPEG-1   MPEG-2

         90    80    88     A0     C0       D0
         91    81    89     A1     C1       D1
         92    82    8A     A2     C2       D2
         93    83    8B     A3     C3       D3
         94    84    8C     A4     C4       D4
         95    85    8D     A5     C5       D5
         96    86    8E     A6     C6       D6
         97    87    8F     A7     C7       D7
        --------------------------------------------------------------------------------
        */
        if ((startcode == 445 && buffer[st_i] > 0x7f && buffer[st_i] < 0x98) ||
            (startcode == 445 && buffer[st_i] > 0x9f && buffer[st_i] < 0xa8) ||
            (startcode >= 0x1c0 && startcode <= 0x1c7) ||
            (startcode >= 0x1d0 && startcode <= 0x1d7))
            return true;
        else return false;

    }

    public static bool IsAudMpeg(Ref<byte> buffer)
    {

        int startcode;

        startcode = GetNbytes(4, buffer.AtIndex(14));

        if ((startcode >= 0x1c0 && startcode <= 0x1c7) ||
            (startcode >= 0x1d0 && startcode <= 0x1d7))
            return true;
        else return false;

    }

    public static bool IsSubs(Ref<byte> buffer)
    {

        int startcode, st_i;

        startcode = GetNbytes(4, buffer.AtIndex(14));
        st_i = 0x17 + buffer[0x16];


        if (startcode == 445 && buffer[st_i] > 0x1f && buffer[st_i] < 0x40)
            return true;
        else return false;

    }


    public static bool IsSynch(Ref<byte> buffer)
    {

        int startcode;

        startcode = GetNbytes(4, buffer.AtIndex(0));

        if (startcode == 0x1BA) return true;
        else return false;

    }
    public static int getAudId(Ref<byte> buffer)
    {
        int AudId;


        if (!IsAudio(buffer)) return -1;

        if (IsAudMpeg(buffer))
            AudId = buffer[0x11];
        else
            AudId = buffer[0x17 + buffer[0x16]];

        return AudId;
    }

    public static int getSubId(Ref<byte> buffer)
    {
        int SubId;

        if (!IsSubs(buffer)) return -1;

        SubId = buffer[0x17 + buffer[0x16]];

        return SubId;
    }

    public static long MPEGVideoAttr(Ref<byte> buffer)
    {
        long ret;


        if (GetNbytes(4, buffer.AtIndex(0x17 + buffer[0x16])) != 0x000001b3)
            return 0;

        ret = GetNbytes(4, buffer.AtIndex(0x17 + buffer[0x16] + 4));

        return ret;

    }

    public static int MPEGvert(long i64Attr)
    {
        return (int)(i64Attr & 0x000FFF00) >> 8;
    }

    public static int MPEGhoriz(long i64Attr)
    {
        return (int)(i64Attr & 0xFFF00000) >> 20;
    }

    public static int MPEGDAR(long i64Attr)
    {
        return (int)(i64Attr & 0x0F0) >> 4;

        // 0= forbidden
        // 1= 1:1
        // 2= 4:3
        // 3= 16:9
        // 4= 2,21:1
    }

    public static void ModifyLBA(Ref<byte> buffer, long m_i64OutputLBA)
    {
        // 1st lba number
        buffer[0x30] = (byte)(m_i64OutputLBA % 256);
        buffer[0x2F] = (byte)((m_i64OutputLBA / 256) % 256);
        buffer[0x2E] = (byte)((m_i64OutputLBA / 256 / 256) % 256);
        buffer[0x2D] = (byte)(m_i64OutputLBA / 256 / 256 / 256);

        // 2nd lba number
        buffer[0x040E] = buffer[0x30];
        buffer[0x040D] = buffer[0x2F];
        buffer[0x040C] = buffer[0x2E];
        buffer[0x040B] = buffer[0x2D];
    }

    public static void ModifyCID(Ref<byte> buffer, int VobId, int CellId)
    {
        // VobID
        buffer[0x420] = (byte)(VobId % 256);
        buffer[0x41F] = (byte)(VobId / 256);
        //CellID
        buffer[0x422] = (byte)CellId;
    }

    public static void CleanILV(Ref<byte> buffer)
    {
        // Flags
        buffer[0x427] = buffer[0x428] = (byte)0;
        //End Address
        buffer[0x429] = buffer[0x42a] = buffer[0x42b] = buffer[0x42c] = (byte)0;
        //Next start
        buffer[0x42d] = buffer[0x42e] = buffer[0x42f] = buffer[0x430] = (byte)0;
        // Size
        buffer[0x431] = buffer[0x432] = (byte)0;
    }


    public static void ModifyPUOPS(Ref<byte> buffer)
    {
        buffer[0x35] = 0;
        buffer[0x36] = 0;
        buffer[0x37] = 0;
        buffer[0x38] = 0;
    }



    private static uint[] ac3bitrate =   {32,32,40,40,48,48,56,56,64,64,
        80,80,96,96,112,112,128,128,160,160,192,192,224,224,256,256,
        320,320,384,384,448,448,512,512,576,576,640,640,
        64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64 };

    private static uint[] dtsbitrate =   {32,56,64,96,112,128,192,224,256,320,384,448,512,576,640,768,
        960,1024,1152,1280,1344,1408,1411,1472,1536,1920,2048,3072,3840,0,0,0};

    private static ulong p_bit;
    ////////////////////////////////////////////////////////////////////////
    //      getbits
    ////////////////////////////////////////////////////////////////////////
    public static uint getbits(int number, Ref<byte> p_frame)
    {
        uint bit_ini, byte_ini, bit_end, byte_end, output;

        byte_ini = (uint)(p_bit / 8);
        bit_ini = (uint)(p_bit % 8);

        p_bit += (uint)number;

        byte_end = (uint)(p_bit / 8);
        bit_end = (uint)(p_bit % 8);

        if (byte_end == byte_ini)
            output = p_frame[byte_end];
        else if (byte_end == byte_ini + 1)
            output = p_frame[byte_end - 1] * 256u + p_frame[byte_end];
        else
            output = p_frame[byte_end - 2] * 256u * 256u + p_frame[byte_end - 1] * 256u +
                  p_frame[byte_end];

        output = (output) >> (int)(8 - bit_end);
        output = output & ((1u << (int)number) - 1u);

        return output;
    }

    public static int getac3rate(Ref<byte> buffer)
    {
        int first_acc_p, rate;

        p_bit = 0;

        first_acc_p = GetNbytes(2, buffer.AtIndex(0x19 + buffer[0x16]));
        first_acc_p += (0x1A + buffer[0x16]);

        if (buffer[first_acc_p + 0] != 0x0B || buffer[first_acc_p + 1] != 0x77)
            return 0;

        rate = (int)ac3bitrate[buffer[first_acc_p + 4] & 0x3F];
        return rate;

    }

    public static int getdtsrate(Ref<byte> buffer)
    {

        int first_acc_p, rate;
        uint unused;

        p_bit = 0;

        first_acc_p = GetNbytes(2, buffer.AtIndex(0x19 + buffer[0x16]));
        first_acc_p += (0x1A + buffer[0x16]);

        if (buffer[first_acc_p + 0] != 0x7F || buffer[first_acc_p + 1] != 0xFE ||
            buffer[first_acc_p + 2] != 0x80 || buffer[first_acc_p + 3] != 0x01)
            return 0;

        unused = getbits(32, buffer.AtIndex(first_acc_p));
        unused = getbits(1, buffer.AtIndex(first_acc_p));
        unused = getbits(5, buffer.AtIndex(first_acc_p));
        unused = getbits(1, buffer.AtIndex(first_acc_p));
        unused = getbits(7, buffer.AtIndex(first_acc_p));
        unused = getbits(14, buffer.AtIndex(first_acc_p));
        unused = getbits(6, buffer.AtIndex(first_acc_p));
        unused = getbits(4, buffer.AtIndex(first_acc_p));

        rate = (int)getbits(5, buffer.AtIndex(first_acc_p));
        rate = (int)dtsbitrate[rate];


        return rate;


    }

    public static bool _stati64(string path, out FileInfo result) {
        try
        {
            result = new FileInfo(path);
            return File.Exists(path);
        }
        catch (Exception)
        {
            result = null;
            return false;
        }
    }
}