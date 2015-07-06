using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace replica.sl.lib
{
	public class Profile
	{
		public string sUsername { get; private set; }
		public Uri cHomePage { get; private set; }
		public Profile(helpers.replica.services.dbinteract.Profile cProfile)
		{
			sUsername = cProfile.sUsername;
			if (null == cProfile.sHomePage || 2 > cProfile.sHomePage.Length)
				cProfile.sHomePage = "/profile";
			cHomePage = new Uri(cProfile.sHomePage, UriKind.Relative);
		}
	}
}
