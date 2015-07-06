using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using helpers;
using helpers.extensions;
using helpers.replica.mam;

namespace webservice.ia
{
	public partial class cliporder : System.Web.UI.Page
	{
		private class Logger : webservice.ia.Logger
		{
			public Logger()
				: base("cliporder")
			{ }
		}
		protected void Page_Load(object sender, EventArgs e)
		{
			try
			{
				throw new NotImplementedException();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				string sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><current>";
				sXML += "<error description=\"" + ex.Message.ForXML() + "\" />";
                sXML += "</current>" + Environment.NewLine;
				Response.Write(sXML);
				Response.StatusCode = 503; // служба недоступна (временно)
			}
		}
	}
}