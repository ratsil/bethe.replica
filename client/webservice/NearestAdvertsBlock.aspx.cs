using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace webservice
{
	public partial class NearestAdvertsBlock : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			(new webservice.services.Logger()).WriteDebug2("nearest_block: " + webservice.services.DBInteract.dtNearestAdvertsBlock);
            String sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + "<NearestAdvertsBlock>";
			sXML += webservice.services.DBInteract.dtNearestAdvertsBlock.ToString("dd.MM.yyyy HH:mm:ss") + "</NearestAdvertsBlock>" + Environment.NewLine;
			Response.Write(sXML);
		}
	}
}