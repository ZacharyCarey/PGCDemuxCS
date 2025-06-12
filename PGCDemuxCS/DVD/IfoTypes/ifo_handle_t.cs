

using PgcDemuxCS;
using PgcDemuxCS.DVD;
using PgcDemuxCS.DVD.IfoTypes.Common;
using PgcDemuxCS.DVD.IfoTypes.VMGI;
using PgcDemuxCS.DVD.IfoTypes.VTS;

/**
 * The following structure defines an IFO file.  The structure is divided into
 * two parts, the VMGI, or Video Manager Information, which is read from the
 * VIDEO_TS.[IFO,BUP] file, and the VTSI, or Video Title Set Information, which
 * is read in from the VTS_XX_0.[IFO,BUP] files.
 */
internal class ifo_handle_t
{
    /* VMGI */
    internal vmgi_mat_t? vmgi_mat = null;
    internal tt_srpt_t? tt_srpt = null;
    internal pgc_t? first_play_pgc = null;
    internal ptl_mait_t? ptl_mait = null;
    internal vts_atrt_t? vts_atrt = null;
    internal txtdt_mgi_t? txtdt_mgi = null;

    /* Common */
    internal pgci_ut_t? pgci_ut = null;
    internal c_adt_t? menu_c_adt = null;
    internal vobu_admap_t? menu_vobu_admap = null;

    /* VTSI */
    internal vtsi_mat_t? vtsi_mat = null;
    internal vts_ptt_srpt_t? vts_ptt_srpt = null;
    internal pgcit_t? vts_pgcit = null;
    internal vts_tmapt_t? vts_tmapt = null;
    internal c_adt_t? vts_c_adt = null;
    internal vobu_admap_t? vts_vobu_admap = null;

    private ifo_handle_t(IIfoFileReader reader, int title, bool backup)
    {
        string filename;
        if (title == 0) filename = $"VIDEO_TS.{(backup ? "BUP" : "IFO")}";
        else filename = $"VTS_{title:00}_0.{(backup ? "BUP" : "IFO")}";

        Stream file = reader.OpenFile(filename);

        /* First check if this is a VMGI file. */
        if (vmgi_mat_t.ifoRead_VMG(file, out vmgi_mat))
        {

            /* These are both mandatory. */
            if (!pgc_t.ifoRead_FP_PGC(this, file, out first_play_pgc) || !tt_srpt_t.ifoRead_TT_SRPT(this, file, out tt_srpt))
                goto ifoOpen_fail;

            pgci_ut_t.ifoRead_PGCI_UT(this, file, out pgci_ut);
            ptl_mait_t.ifoRead_PTL_MAIT(this, file, out ptl_mait);

            /* This is also mandatory. */
            if (!vts_atrt_t.ifoRead_VTS_ATRT(file, vmgi_mat.vts_atrt, out vts_atrt))
                goto ifoOpen_fail;

            if (vmgi_mat.txtdt_mgi != 0) txtdt_mgi_t.ifoRead_TXTDT_MGI(file, vmgi_mat.txtdt_mgi, out txtdt_mgi);
            if (vmgi_mat.vmgm_c_adt != 0) c_adt_t.ifoRead_C_ADT(file, vmgi_mat.vmgm_c_adt, out menu_c_adt);
            if (vmgi_mat.vmgm_vobu_admap != 0) vobu_admap_t.ifoRead_VOBU_ADMAP(file, vmgi_mat.vmgm_vobu_admap, out menu_vobu_admap);

            return;
        }

        if (vtsi_mat_t.ifoRead_VTS(file, out vtsi_mat))
        {

            if (!vts_ptt_srpt_t.ifoRead_VTS_PTT_SRPT(file, vtsi_mat.vts_ptt_srpt, out vts_ptt_srpt) || !pgcit_t.ifoRead_PGCIT(file, vtsi_mat.vts_pgcit, out vts_pgcit))
                goto ifoOpen_fail;

            pgci_ut_t.ifoRead_PGCI_UT(this, file, out pgci_ut);
            if (vtsi_mat.vts_tmapt != 0) vts_tmapt_t.ifoRead_VTS_TMAPT(file, vtsi_mat.vts_tmapt, out vts_tmapt);
            if (vtsi_mat.vtsm_c_adt != 0) c_adt_t.ifoRead_C_ADT(file, vtsi_mat.vtsm_c_adt, out menu_c_adt);
            if (vtsi_mat.vtsm_vobu_admap != 0) vobu_admap_t.ifoRead_VOBU_ADMAP(file, vtsi_mat.vtsm_vobu_admap, out menu_vobu_admap);

            if (!c_adt_t.ifoRead_C_ADT(file, vtsi_mat.vts_c_adt, out vts_c_adt) || !vobu_admap_t.ifoRead_VOBU_ADMAP(file, vtsi_mat.vts_vobu_admap, out vts_vobu_admap))
                goto ifoOpen_fail;

            return;
        }

    ifoOpen_fail:
        throw new IOException($"Invalid IFO for title {title}.");
    }

    /// <summary>
    /// Opens an IFO and reads in all the data for the corresponding file.
    /// Accepted ifo file names:
    /// "VIDEO_TS" (VMG ifo)
    /// "VTS_xx_0" (VTS xx ifo)
    /// </summary>
    public static ifo_handle_t? Open(IIfoFileReader reader, string ifoName)
    {
        ifoName = ifoName.ToUpper();

        // Try and load from the ifo file first
        try
        {
            if (ifoName == "VIDEO_TS")
            {
                return new ifo_handle_t(reader, 0, false);
            }

            int title = int.Parse(ifoName[4..6]);
            return new ifo_handle_t(reader, title, false);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        // Failed to parse ifo, try BUP (the backup file)
        try
        {
            if (ifoName == "VIDEO_TS")
            {
                return new ifo_handle_t(reader, 0, true);
            }

            int title = int.Parse(ifoName[4..6]);
            return new ifo_handle_t(reader, title, true);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine();
        }

        // Failed to parse both ifo and bup
        return null;
    }

    /// <summary>
    /// Opens an IFO and reads in all the data for the corresponding file.
    /// Accepted ifo file names:
    /// "VIDEO_TS" (VMG ifo)
    /// "VTS_xx_0" (VTS xx ifo)
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static ifo_handle_t? Open(string DvdRoot, string ifoName)
    {
        return Open(new SimpleIfoReader(DvdRoot), ifoName);
    }
}