using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Text;
using helpers;
using helpers.extensions;

namespace webservice.ia
{
	public partial class vjstatus : System.Web.UI.Page
	{
		private class Logger : webservice.ia.Logger
		{
			public Logger()
				: base("vjstatus")
			{ }
		}
		protected void Page_Load(object sender, EventArgs e)
		{
			try
			{
				if (XML.Redirect(this))
					return;
				if (null == Request.Params["text"])
                    throw new Exception("parameter text is missing");
				DBInteract cDBI = new DBInteract(Preferences.sIaUsername, Preferences.sIaPass);
                string sText = HttpUtility.ParseQueryString(Request.RawUrl, Encoding.GetEncoding("utf-8"))["text"];
				if (null == sText || 1 > sText.Length)
				{
					if(null != cDBI.VJMessageCurrentGet())
					{
						cDBI.VJMessageCurrentStop();
						Response.Write("current vj message stopped");
					}
					else
						Response.Write("there was an attempt to stop current vj message, but there aren't any active ones");
				}
				else
					cDBI.VJMessageAdd(sText);
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