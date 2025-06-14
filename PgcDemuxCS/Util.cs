namespace PgcDemuxCS
{
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

        public static int GetNbytes(int nNumber, Ref<byte> address)
        {
            int ret, i;

            for (i = ret = 0; i < nNumber; i++)
                ret = ret * 256 + address[i];
            return ret;
        }

        public static int BCD2Dec(int BCD)
        {
            int ret;
            ret = (BCD / 0x10) * 10 + (BCD % 0x10);
            return ret;
        }

        public static int DurationInFrames(TimeSpan duration, double fps)
        {
            return (int)(duration.TotalSeconds * fps);
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
    }
}