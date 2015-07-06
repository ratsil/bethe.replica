using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using helpers;
using helpers.extensions;
using helpers.replica.pl;
using helpers.replica;

namespace webservice
{
    public partial class current : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                //(new Logger(eLevel, "current.aspx.cs")).WriteDebug2("Page_Load:begin");
                //Response.AppendHeader("Expires", "0");
                //Response.AppendHeader("Cache-Control", "must-revalidate, post-check=0, pre-check=0");
                Response.ContentType = "application/octet-stream";
                Response.AddHeader("content-disposition", "filename = data.xml");
				DBInteract cDBI = new DBInteract("replica_client", "");
                //(new Logger(eLevel, "current.aspx.cs")).WriteDebug2("Page_Load:cdbi_init");
                PlaylistItem cCurrentPLI = null;
                cCurrentPLI = cDBI.ComingUpGet(0, 1).Dequeue();
                string sTitle, sAbbr, sArtistID, sAlbum, sCode, sLastPlayed, sFramesQty, sArtist;


                sLastPlayed = cCurrentPLI.dtStartReal.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
                string sType = cCurrentPLI.cClass.sName.ToLower();
                if (sType.Contains("program"))
                    sType = "program";
                else if (sType.Contains("advertisement"))
                    sType = "advertisement";
                else if (sType.Contains("clip"))
                    sType = "clip";
                else if (sType.Contains("design"))
                    sType = "design";
                else
                    sType = "unknown";

                String sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><current>"; //<current>
                switch (sType)
                {
                    case "program":
                        sXML += "<program id=\"000\" /></current>" + Environment.NewLine;
                        break;
                    case "advertisement":
                        sXML += "<advertisement id=\"000\" /></current>" + Environment.NewLine;
                        break;
                    case "clip":
                        helpers.replica.mam.Clip cClip = helpers.replica.mam.Clip.Load(cCurrentPLI.cAsset.nID);
                        sTitle = cClip.stCues.sSong;
                        sAbbr = cClip.sName;
                        sArtistID = cClip.stCues.nID.ToString();
                        sAlbum = "";
                        sCode = cClip.stCues.sAlbum;
                        sFramesQty = (cCurrentPLI.nFrameStop - cCurrentPLI.nFrameStart).ToString();
                        sArtist = cClip.stCues.sArtist;

                        try
                        {
                            if (null == cClip.aCustomValues)
                                cClip.aCustomValues = cDBI.CustomsLoad(cClip.nID);
                            cClip.PersonsLoad();
                            sXML += "<clip";
                            sXML += " id=\"" + cClip.nID + "\"";
                            sXML += " name=\"" + cClip.sName.ForXML() + "\"";
                            sXML += ">";
                            sXML += "<cues";
                            sXML += " id=\"" + cClip.stCues.nID + "\"";
                            sXML += " artist=\"" + cClip.stCues.sArtist.ForXML() + "\"";
                            sXML += " song=\"" + cClip.stCues.sSong.ForXML() + "\"";
                            if (null != cClip.stCues.sAlbum)
                                sXML += " album=\"" + cClip.stCues.sAlbum.ForXML() + "\"";
                            sXML += " />";
                            sXML += "<persons>";
                            foreach (helpers.replica.mam.Person ePerson in cClip.aPersons)
                            {
								sXML += "<person id=\"" + ePerson.nID + "\" name=\"" + ePerson.sName.ForXML() + "\" />";
                            }
                            sXML += "</persons>";
                            sXML += "</clip>";
                            sXML += "</current>" + Environment.NewLine;
                        }
                        catch (Exception ex)
                        {
                            (new Logger("current.aspx.cs")).WriteDebug2("ComingUpGet:catch2");
                            (new Logger("current.aspx.cs")).WriteError(ex);
                            throw;
                        }
                        break;
                    case "design":
                        sXML += "<design id=\"000\" /></current>" + Environment.NewLine;
                        break;
                    default:
                        throw new Exception();
                }
                Response.Write(sXML);
            }
            catch (Exception ex)
            {
                (new Logger("current.aspx.cs")).WriteDebug2("ComingUpGet:catch2");
                (new Logger("current.aspx.cs")).WriteError(ex);
                //Response.Write(ex.Message + Environment.NewLine + ex.StackTrace);
                //Response.StatusCode = 501; // метод не поддерживается
                //Response.StatusCode = 500; // ошибка сервера
                Response.StatusCode = 503; // служба недоступна (временно)
            }
        }
    }
}
