using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using helpers;
using helpers.extensions;
using helpers.replica.logs;


namespace webservice.ia
{
    public partial class chat : System.Web.UI.Page
    {
        private class Logger : webservice.ia.Logger
        {
            public Logger()
                : base("chat")
            { }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            try  
            {
                Response.ContentType = "application/octet-stream";
                Response.AddHeader("content-disposition", "filename = chat.xml");
                DBInteract cDBI = new DBInteract(Preferences.sIaUsername, Preferences.sIaPass);
                ChatLog cChat = cDBI.ChatLastLogGet();
                String sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><chat";
                DateTime dtRetVal = DateTime.Now;

                if ("start" == Request.Params["p"])
                {
                    if (cChat == null)
                        dtRetVal = DateTime.MaxValue;
                    else
                        dtRetVal = cChat.dtStart;
                    sXML += " start_utc=\"" + dtRetVal.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss:fff") + "\"";
                }
                else if ("stop" == Request.Params["p"])
                {
                    if (cChat == null)
                        dtRetVal = DateTime.MinValue;
                    else
                        dtRetVal = cChat.dtStop;
                    sXML += " stop_utc=\"" + dtRetVal.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss:fff") + "\"";
                }
                else
                {
                    sXML += "><error description=\"invalid parameter 'p' = " + (Request.Params["p"] == null ? "NULL" : "'" + Request.Params["p"] + "'") + "\" /></chat>";
                    Response.Write(sXML);
                    return;
                }

                sXML += " />";
                Response.Write(sXML);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                string sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><error description=\"" + ex.Message.ForXML() + "\"";
                if (null != ex.InnerException)
                {
                    sXML += "<inner description=\"" + ex.InnerException.Message.ForXML() + "\" />" + Environment.NewLine;
                    sXML += "</error>" + Environment.NewLine;
                }
                else
                    sXML += " />" + Environment.NewLine;
                Response.Write(sXML);
                //Response.StatusCode = 503;
            }
        }
    }
}