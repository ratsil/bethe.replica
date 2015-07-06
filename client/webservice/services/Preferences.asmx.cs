using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Xml;
using System.Xml.Serialization;

namespace webservice.services
{
	/// <summary>
	/// Summary description for preferences
	/// </summary>
	[WebService(Namespace = "http://replica/services/Preferences.asmx")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	// [System.Web.Script.Services.ScriptService]
	public class Preferences : System.Web.Services.WebService
	{
		[WebMethod(EnableSession = true)]
		public webservice.Preferences.Clients.Replica ReplicaGet()
		{
			return webservice.Preferences.cClientReplica;
		}
		//public webservice.Preferences.Clients.Replica ReplicaGet(string sClientType)
		//{
		//    return webservice.Preferences.cClientReplica;
		//}
	}
}
