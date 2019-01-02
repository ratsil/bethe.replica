using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace SCRTimer
{
	public partial class Form1 : Form
	{
		private DateTime _dtPlayerStopPlanned;
		private DateTime _dtNextAdvertsStart;
		private System.Net.WebClient _cWC_Player;
		private System.Net.WebClient _cWC_DBI;
		public Form1()
		{
			InitializeComponent();
			TopMost = true; //поверх всех окон

			_ui_tmrPlayerView.Enabled = true;
			_ui_tmrPlayerView.Interval = 200;
			_ui_tmrPlayerGet.Enabled = true;
			_ui_tmrPlayerGet.Interval = 1000;
			_ui_tmrAdvertGet.Enabled = true;
			_ui_tmrAdvertGet.Interval = 5000;
			_dtPlayerStopPlanned = DateTime.MinValue;
			_dtNextAdvertsStart = DateTime.MinValue;
			_cWC_Player = new System.Net.WebClient();
			_cWC_Player.BaseAddress = "http://player.scr.replica";
			_cWC_DBI = new System.Net.WebClient();
			_cWC_DBI.BaseAddress = "http://web.channel.replica";
		}

		private void _ui_tmrPlayerGet_Tick(object sender, EventArgs e)
		{
			try
			{
				byte[] aDD = _cWC_Player.DownloadData("IG/TimerPlayer.aspx");
				string sXML = Encoding.UTF8.GetString(aDD);
				XmlDocument cXMLDocument = new XmlDocument();
				cXMLDocument.LoadXml(sXML);
				XmlNode cXmlNode = cXMLDocument.GetElementsByTagName("PlaylistStopPlanned").Item(0);
				_dtPlayerStopPlanned = DateTime.Parse(cXmlNode.ChildNodes[0].Value, new System.Globalization.CultureInfo("ru-RU", false));
				if (_dtPlayerStopPlanned.Date == DateTime.MaxValue.Date)
					_dtPlayerStopPlanned = DateTime.MaxValue;
			}
			catch
			{
				_dtPlayerStopPlanned = DateTime.MaxValue;
			}
		}

		private void _ui_tmrPlayerView_Tick(object sender, EventArgs e)
		{
			DateTime dtDiff;
			if (DateTime.Now > _dtPlayerStopPlanned)
				_ui_lblPlayerTimer.Text = "STOPPED";
			else if (DateTime.MaxValue == _dtPlayerStopPlanned)
				_ui_lblPlayerTimer.Text = "ERROR";
			else
			{
				dtDiff = new DateTime(_dtPlayerStopPlanned.Ticks - DateTime.Now.Ticks);
				_ui_lblPlayerTimer.Text = dtDiff.ToString("HH:mm:ss");
			}

			if (DateTime.Now > _dtNextAdvertsStart)
				_ui_lblAdvertTimer.Text = "UNKNOWN";
			else if (DateTime.MaxValue == _dtNextAdvertsStart)
				_ui_lblAdvertTimer.Text = "ERROR";
			else
			{
				dtDiff = new DateTime(_dtNextAdvertsStart.Ticks - DateTime.Now.Ticks);
				_ui_lblAdvertTimer.Text = dtDiff.ToString("HH:mm:ss");
			}
		}

		private void _ui_tmrAdvertGet_Tick(object sender, EventArgs e)
		{
			try
			{
				byte[] aDD = _cWC_DBI.DownloadData("NearestAdvertsBlock.aspx");
				string sXML = Encoding.UTF8.GetString(aDD);
				XmlDocument cXMLDocument = new XmlDocument();
				cXMLDocument.LoadXml(sXML);
				XmlNode cXmlNode = cXMLDocument.GetElementsByTagName("NearestAdvertsBlock").Item(0);
				_dtNextAdvertsStart = DateTime.Parse(cXmlNode.ChildNodes[0].Value, new System.Globalization.CultureInfo("ru-RU", false));
				if (_dtNextAdvertsStart.Date == DateTime.MaxValue.Date)
					_dtNextAdvertsStart = DateTime.MaxValue;
			}
			catch
			{
				_dtNextAdvertsStart = DateTime.MaxValue;
			}
		}
	}
}
