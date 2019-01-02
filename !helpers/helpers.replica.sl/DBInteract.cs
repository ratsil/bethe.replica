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
using System.ServiceModel;

namespace helpers.replica.services.dbinteract
{
	public class DBInteract : DBInteractSoapClient
	{
		static bool _bSessionInited = false;
		static public BasicHttpBinding EndPointGet()
		{
			BasicHttpBinding binding = new BasicHttpBinding(
		 Application.Current.Host.Source.Scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase)
		 ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None);
			binding.MaxReceivedMessageSize = int.MaxValue;
			binding.MaxBufferSize = int.MaxValue;
			binding.OpenTimeout =
			binding.CloseTimeout =
			binding.SendTimeout =
			binding.ReceiveTimeout = TimeSpan.FromHours(5);
			return binding;
		}
		public static string GetUrl(string sUrl)
		{
			string sRetVal = sUrl.Replace("/ig", "").Replace("/fake_ig", "").Replace("/fake_replica", "").Replace("/dev/", "/");
			if (sRetVal.StartsWith("file"))
				sRetVal = "http://replica/";
			return sRetVal;
		}
		public DBInteract()
			: base(EndPointGet(), new EndpointAddress(new Uri(new Uri(GetUrl(Application.Current.Host.Source.AbsoluteUri)), "../services/DBInteract.asmx")))
		{
			if (!_bSessionInited)
			{
				_bSessionInited = true;
				InitSessionAsync();
			}
		}
		public DBInteract(string sAnotherServerName)
		: base(EndPointGet(), new EndpointAddress(new Uri(new Uri("http://" + sAnotherServerName + "/"), "../services/DBInteract.asmx")))
		//: base(EndPointGet(), new EndpointAddress("http://" + sAnotherServerName + "/services/DBInteract.asmx"))
		{
			if (!_bSessionInited)
			{
				_bSessionInited = true;
				InitSessionAsync();
			}
		}
	}
}