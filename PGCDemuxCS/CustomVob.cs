namespace PgcDemuxCS
{
    internal class CCustomVob
    {
        public bool m_bCheckIFrame { set => PgcDemuxApp.theApp.m_bCheckIFrame = value; }
        public bool m_bCheckVideoPack { set => PgcDemuxApp.theApp.m_bCheckVideoPack = value; }
        public bool m_bCheckAudioPack { set => PgcDemuxApp.theApp.m_bCheckAudioPack = value; }
        public bool m_bCheckNavPack { set => PgcDemuxApp.theApp.m_bCheckNavPack = value; }
        public bool m_bCheckSubPack { set => PgcDemuxApp.theApp.m_bCheckSubPack = value; }
        public bool m_bCheckLBA { set => PgcDemuxApp.theApp.m_bCheckLBA = value; }

        public CCustomVob()
        {
            m_bCheckIFrame = false;
            m_bCheckVideoPack = true;
            m_bCheckAudioPack = true;
            m_bCheckNavPack = true;
            m_bCheckSubPack = true;
            m_bCheckLBA = true;
        }
    }
}