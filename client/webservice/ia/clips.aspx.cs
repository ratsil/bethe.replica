using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using helpers;
using helpers.extensions;
using helpers.replica.mam;

namespace webservice.ia
{
	public partial class clips : System.Web.UI.Page
	{
		private class Logger : webservice.ia.Logger
		{
			public Logger()
				: base("clips")
			{ }
		}
		protected void Page_Load(object sender, EventArgs e)
		{
			try
			{
				Response.ContentType = "application/octet-stream";
				Response.AddHeader("content-disposition", "filename = clips.xml");
				DBInteract cDBI = new DBInteract("replica_ia", "");
				Queue<Clip> aqClips = cDBI.ClipsGet();
                String sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><clips>";
				while (0 < aqClips.Count)
				{
					try
					{
						sXML += XML.ClipGet(aqClips.Dequeue());
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex);
					}
				}
				sXML += "</clips>";

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
