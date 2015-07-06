using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace webservice
{
	public class Profile
	{
		public string sUsername;
		public string sHomePage;

		public Profile()
		{
		}
		public Profile(string sName, string sPassword)
			: this()
		{
			sUsername = sName;
			sHomePage = "/" + (new DBInteract(sName, sPassword)).UserHomePageGet();
		}
	}
}