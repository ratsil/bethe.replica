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
using helpers.replica.pl;

namespace webservice.ia
{
	public partial class pl : System.Web.UI.Page
	{
		private class Logger : webservice.ia.Logger
		{
			public Logger()
				: base("playlist")
			{ }
		}
		protected void Page_Load(object sender, EventArgs e)
		{
			try
			{
				Response.ContentType = "application/octet-stream";
				Response.AddHeader("content-disposition", "filename = pl.xml");
				DBInteract cDBI = new DBInteract("replica_ia", "");

				string sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><playlist>";
				if (!cDBI.IsThereAnyStartedLiveBroadcast())
				{
					Queue<PlaylistItem> aqPLIs = cDBI.ComingUpWithAssetsResolvedGet(TimeSpan.FromHours(1));
					while (0 < aqPLIs.Count)
						sXML += XML.PlaylistItemGet(aqPLIs.Dequeue());
				}
				else
					sXML += "<live id=\"000\" />";
				sXML += "</playlist>" + Environment.NewLine;

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
				//Response.StatusCode = 503; // служба недоступна (временно)
			}
		}
	}
}
