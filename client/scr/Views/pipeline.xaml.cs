using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Text.RegularExpressions;
using System.Xml;
using System.Windows.Threading;
using helpers.extensions;

using g = globalization;

namespace scr.Views
{
	public partial class pipeline : Page
	{
		static string _sFileNameEmpty = g.SCR.sNoticePipeline1;
		class Channel
		{
			public Page cPage;
			public string sURL;
			public string sUUID;
			public string sFileName;
			public TextBox ui_tb;
			public Button ui_btnStart;
			public Button ui_btnStop;
			public DataGrid ui_dgStatus;
			public DispatcherTimer cTimer;

			public Channel()
			{
				cTimer = new System.Windows.Threading.DispatcherTimer();
				cTimer.Tick += new EventHandler(Timer_Tick);
				cTimer.Interval = new TimeSpan(0, 0, 0, 5);  // период проверки статуса темплейта
			}

			public void RecordStart()
			{
				HttpWebRequest cHttpWebRequest = HttpWebRequest.CreateHttp(sURL + "Record/Start?name=" + sFileName);
				cHttpWebRequest.BeginGetResponse(HttpResponseForStart, cHttpWebRequest);
			}
			public void RecordStop()
			{
				HttpWebRequest cHttpWebRequest = HttpWebRequest.CreateHttp(sURL + "Record/Stop?uuid={" + sUUID + "}");
				cHttpWebRequest.BeginGetResponse(HttpResponseForStop, cHttpWebRequest);
			}
			private void Timer_Tick(object s, EventArgs args)
			{
				cTimer.Stop();
				HttpWebRequest cHttpWebRequest = HttpWebRequest.CreateHttp(sURL + "Record/Status?uuid={" + sUUID + "}");
				cHttpWebRequest.BeginGetResponse(HttpResponseForStatus, cHttpWebRequest);
			}
			private void HttpResponseForStart(IAsyncResult iAsynchronousResult)
			{
				try
				{
					HttpWebResponse cHttpWebResponse = (HttpWebResponse)((HttpWebRequest)iAsynchronousResult.AsyncState).EndGetResponse(iAsynchronousResult);
					XmlReader cXmlReader = XmlReader.Create(cHttpWebResponse.GetResponseStream());
					cXmlReader.ReadToFollowing("UUID");
					sUUID = cXmlReader.ReadElementContentAsString();
					cPage.Dispatcher.BeginInvoke(() => { ui_btnStop.Visibility = Visibility.Visible; cTimer.Start(); });
				}
				catch
				{
					cPage.Dispatcher.BeginInvoke(() => { ui_dgStatus.ItemsSource = new object[]{new KeyValuePair<string, string>(g.Common.sStatus.ToLower() + ":", g.Common.sErrorConnection.ToLower())}; });
				}

			}
			private void HttpResponseForStop(IAsyncResult iAsynchronousResult)
			{
				cPage.Dispatcher.BeginInvoke(() => { cTimer.Stop(); sFileName = ""; ui_tb.Text = _sFileNameEmpty; ui_tb.IsEnabled = true; });
			}
			private void HttpResponseForStatus(IAsyncResult iAsynchronousResult)
			{
				try
				{
					HttpWebResponse cHttpWebResponse = (HttpWebResponse)((HttpWebRequest)iAsynchronousResult.AsyncState).EndGetResponse(iAsynchronousResult);
					System.IO.Stream cStream = cHttpWebResponse.GetResponseStream();
					string sXML = (new System.IO.StreamReader(cStream)).ReadToEnd();
					XmlReader cXmlReader = XmlReader.Create(new System.IO.StringReader(sXML));
					List<KeyValuePair<string, string>> aValues = new List<KeyValuePair<string, string>>();
					TimeSpan ts;
					cXmlReader.ReadToFollowing("Status");
					string sValue = cXmlReader.ReadElementContentAsString();
					aValues.Add(new KeyValuePair<string, string>(g.Common.sStatus.ToLower() + ":", ("200" == sValue ? g.Common.sOk.ToLower() : g.Common.sError.ToLower() + "(" + sValue + ")")));
					string sTS = cXmlReader.ReadElementContentAsString();
					ts = TimeSpan.Parse(sTS.Substring(0, sTS.Length - 3) + "." + (sTS.Substring(sTS.Length - 2, 2).ToInt32() * 40).ToString("000"));
					aValues.Add(new KeyValuePair<string, string>("UUID:", cXmlReader.ReadElementContentAsString()));
					sTS = cXmlReader.ReadElementContentAsString();
					aValues.Add(new KeyValuePair<string, string>(g.Helper.sDuration.ToLower() + ":", ts.Subtract(TimeSpan.Parse(sTS.Substring(0, 8) + "." + (sTS.Substring(9, 2).ToInt32() * 40).ToString("000"))).ToString()));
					cXmlReader.ReadToFollowing("Name");
					sValue = cXmlReader.ReadElementContentAsString();
					if (sFileName != sValue)
						cPage.Dispatcher.BeginInvoke(() => { ui_tb.Text = sFileName = sValue; });
					aValues.Add(new KeyValuePair<string, string>(g.Common.sPath.ToLower() + ":", cXmlReader.ReadElementContentAsString()));
					cXmlReader.ReadToFollowing("VideoFormat");
					aValues.Add(new KeyValuePair<string, string>(g.SCR.sNoticePipeline2, cXmlReader.ReadElementContentAsString()));
					aValues.Add(new KeyValuePair<string, string>(g.SCR.sNoticePipeline3, cXmlReader.ReadElementContentAsString()));
					aValues.Add(new KeyValuePair<string, string>(g.SCR.sNoticePipeline4, cXmlReader.ReadElementContentAsString()));
					aValues.Add(new KeyValuePair<string, string>(g.SCR.sNoticePipeline5, cXmlReader.ReadElementContentAsString()));
					aValues.Add(new KeyValuePair<string, string>(g.SCR.sNoticePipeline6, cXmlReader.ReadElementContentAsString()));
					aValues.Add(new KeyValuePair<string, string>(g.SCR.sNoticePipeline7, cXmlReader.ReadElementContentAsString()));

					cPage.Dispatcher.BeginInvoke(() => { ui_dgStatus.ItemsSource = aValues; cTimer.Start(); });
				}
				catch
				{
					cPage.Dispatcher.BeginInvoke(() => { ui_dgStatus.ItemsSource = null; });
				}
			}
		}
		public pipeline()
		{
			InitializeComponent();

			_ui_gChannelFirst.Tag = new Channel() {
				cPage = this,
				sURL = "http://127.0.0.1:8080/",
				sFileName = "",
				ui_tb = _ui_tbChannelFirstFileName,
				ui_btnStart = _ui_btnChannelFirstStart,
				ui_btnStop = _ui_btnChannelFirstStop,
				ui_dgStatus = _ui_dgChannelFirstStatus
			};
			_ui_tbChannelFirstFileName.Text = _sFileNameEmpty;
			_ui_gChannelSecond.Tag = new Channel() {
				cPage = this,
				sURL = "http://127.0.0.1:8181/",
				sFileName = "",
				ui_tb = _ui_tbChannelSecondFileName,
				ui_btnStart = _ui_btnChannelSecondStart,
				ui_btnStop = _ui_btnChannelSecondStop,
				ui_dgStatus = _ui_dgChannelSecondStatus
			};
			_ui_tbChannelSecondFileName.Text = _sFileNameEmpty;
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
		}

		private void _ui_btnStart_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Button ui_btn = (Button)sender;
				Channel cChannel = (Channel)((FrameworkElement)ui_btn.Parent).Tag;
				cChannel.RecordStart();
				cChannel.ui_tb.IsEnabled = false;
				ui_btn.Visibility = Visibility.Collapsed;
			}
			catch { }
		}
		private void _ui_btnStop_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Button ui_btn = (Button)sender;
				((Channel)((FrameworkElement)ui_btn.Parent).Tag).RecordStop();
				ui_btn.Visibility = Visibility.Collapsed;
			}
			catch { }
		}

		private void _ui_tbFileName_GotFocus(object sender, RoutedEventArgs e)
		{
			TextBox ui_tb = (TextBox)sender;
			if (_sFileNameEmpty == ui_tb.Text)
			{
				ui_tb.Text = "";
				ui_tb.FontStyle = FontStyles.Normal;
				ui_tb.Foreground = new SolidColorBrush(Colors.Black);
			}
		}

		private void _ui_tbFileName_LostFocus(object sender, RoutedEventArgs e)
		{
			TextBox ui_tb = (TextBox)sender;
			if (1 > ui_tb.Text.Length)
			{
				ui_tb.Text = _sFileNameEmpty;
				((Channel)((FrameworkElement)ui_tb.Parent).Tag).sFileName = "";
				ui_tb.FontStyle = FontStyles.Italic;
				ui_tb.Foreground = new SolidColorBrush(Colors.Gray);
			}
		}

		private void _ui_tbFileName_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox ui_tb = (TextBox)sender;
			if (_sFileNameEmpty != ui_tb.Text && ui_tb.IsEnabled)
			{
				Channel cChannel = (Channel)((FrameworkElement)ui_tb.Parent).Tag;
				if (1 > ui_tb.Text.Length || Regex.IsMatch(ui_tb.Text, @"^[_a-zA-Z0-9]+$"))
				{
					ui_tb.Text = ui_tb.Text.ToLower();
					cChannel.sFileName = ui_tb.Text;
				}
				else
				{
					ui_tb.Text = cChannel.sFileName;
					ui_tb.Select(ui_tb.Text.Length - 1, 0);
				}
				if (0 < cChannel.sFileName.Length)
					cChannel.ui_btnStart.Visibility = Visibility.Visible;
				else
					cChannel.ui_btnStart.Visibility = Visibility.Collapsed;
			}
		}
	}
}
