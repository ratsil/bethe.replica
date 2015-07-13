using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using helpers;
using helpers.extensions;
using helpers.replica.pl;
using helpers.replica.mam;

namespace webservice.ia
{
	public partial class current : System.Web.UI.Page
	{
		private class Logger : webservice.ia.Logger
		{
			public Logger()
				: base("current")
			{ }
		}
		protected void Page_Load(object sender, EventArgs e)
		{
			try
			{
				Response.ContentType = "application/octet-stream";
				Response.AddHeader("content-disposition", "filename = current.xml");
				DBInteract cDBI = new DBInteract("replica_ia", "");

				string sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><current>";
				if (!cDBI.IsThereAnyStartedLiveBroadcast())
					sXML += XML.PlaylistItemGet(cDBI.ComingUpWithAssetsResolvedGet(0, 1).Dequeue());
				else
					sXML += XML.SCRItemGet(cDBI.SCRAssetCurrentGet());
				sXML += "</current>" + Environment.NewLine;

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
