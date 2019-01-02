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

using controls.childs.sl;
using controls.sl;
using replica.sl;
using helpers.replica.services.dbinteract;
using helpers.extensions;
using g = globalization;

namespace controls.childs.replica.sl
{
	public partial class PlaylistImport : ChildWindow
	{
		internal class UploadInfo
		{
			static public ushort nUploadCompleted = 0;
			static public ushort nParsingCompleted = 0;
			public int nHandle;
			public System.IO.FileStream cFS;
			public string sRemoteFile;
		}
		private Progress _dlgProgress;
        private MsgBox _dlgMsg;
		private DBInteract _cDBI;
		private DateTime _dtDateTimeLastSelected;

		public PlaylistItem[] aPlaylistItems { get; private set; }

		public PlaylistImport()
		{
			InitializeComponent();
			Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.Name);
            Title = g.Helper.sPlaylistImport.ToLower();

			_cDBI = new DBInteract();
			_dlgProgress = new Progress();
            _dlgMsg = new MsgBox();

            _cDBI.UploadFileBeginCompleted += new EventHandler<UploadFileBeginCompletedEventArgs>(_cDBI_UploadFileBeginCompleted);
			_cDBI.UploadFileContinueCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cDBI_UploadFileContinueCompleted);
			_cDBI.UploadFileEndCompleted += new EventHandler<UploadFileEndCompletedEventArgs>(_cDBI_UploadFileEndCompleted);

			_cDBI.PowerGoldFileParseCompleted += new EventHandler<PowerGoldFileParseCompletedEventArgs>(_cDBI_PowerGoldFileParseCompleted);
			_cDBI.VideoInternationalFileParseCompleted += new EventHandler<VideoInternationalFileParseCompletedEventArgs>(_cDBI_VideoInternationalFileParseCompleted);
			_cDBI.DesignFileParseCompleted += new EventHandler<DesignFileParseCompletedEventArgs>(_cDBI_DesignFileParseCompleted);
			_cDBI.PlaylistsMergeCompleted += new EventHandler<PlaylistsMergeCompletedEventArgs>(_cDBI_PlaylistsMergeCompleted);

			_cDBI.ErrorsGetCompleted += new EventHandler<ErrorsGetCompletedEventArgs>(_cDBI_ErrorsGetCompleted);
			_cDBI.ImportLogGetCompleted += _cDBI_ImportLogGetCompleted;
			
			_ui_dpAdvertisementBind.SelectedDate = DateTime.Now.AddDays(1);
			_dlgProgress.Close();
			_ui_btnLog.IsEnabled = false;
			_ui_btnExport.IsEnabled = false;
            _ui_btnOK.Background = Coloring.Notifications.cButtonNormal;
            btnErrorOn(false);
		}


		#region event handlers
		#region UI
		private void _ui_btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (_ui_btnErrors.Tag == null)
                this.DialogResult = true;
            else
            {
                _dlgMsg.Closed += _dlgMsg_Closed;
                _dlgMsg.Show(g.Replica.sWarningPlaylistImport1.Fmt(Environment.NewLine), g.Common.sAttention, MsgBox.MsgBoxButton.OKCancel);
            }
		}
        private void _dlgMsg_Closed(object sender, EventArgs e)
        {
            _dlgMsg.Closed -= _dlgMsg_Closed;
            if (_dlgMsg.enMsgResult == MsgBox.MsgBoxButton.OK)
                this.DialogResult = true;
        }

        private void _ui_btnCancel_Click(object sender, RoutedEventArgs e)
		{
			aPlaylistItems = null;
			this.DialogResult = false;
		}

		private void _ui_btnFile_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog cOFD = new OpenFileDialog();
			cOFD.Multiselect = true;
			if ((bool)cOFD.ShowDialog())
			{
				TextBox ui_tb = null;
				UploadInfo cUploadInfo = null;
				bool bSecondDecision = false;
				foreach (System.IO.FileInfo cFI in cOFD.Files)
				{
					cUploadInfo = new UploadInfo();
					try
					{
						cUploadInfo.cFS = cFI.OpenRead();
					}
					catch
					{
                        MessageBox.Show(g.Common.sErrorFile.ToLower() + ":" + cFI.Name);
						_ui_tbClipsFile.Tag = _ui_tbAdvertisementsFile.Tag = _ui_tbDesignsFile.Tag = null;
						_ui_tbClipsFile.Text = _ui_tbAdvertisementsFile.Text = _ui_tbDesignsFile.Text = "";
						break;
					}
					if (3 > cOFD.Files.Count())
					{
						if (_ui_btnClipsFile == sender)
							ui_tb = _ui_tbClipsFile;
						else if (_ui_btnAdvertisementsFile == sender)
							ui_tb = _ui_tbAdvertisementsFile;
						else if (_ui_btnDesignsFile == sender)
							ui_tb = _ui_tbDesignsFile;
					}
					else
					{
						if (cFI.Extension.ToLower().Contains("txt"))
							ui_tb = _ui_tbClipsFile;
						else if (bSecondDecision)
						{
							if (((UploadInfo)_ui_tbAdvertisementsFile.Tag).cFS.Length < cUploadInfo.cFS.Length)
							{
								_ui_tbDesignsFile.Tag = _ui_tbAdvertisementsFile.Tag;
								_ui_tbDesignsFile.Text = _ui_tbAdvertisementsFile.Text;
								ui_tb = _ui_tbAdvertisementsFile;
							}
							else
								ui_tb = _ui_tbDesignsFile;
						}
						else
						{
							bSecondDecision = true;
							ui_tb = _ui_tbAdvertisementsFile;
						}
					}
					ui_tb.Tag = cUploadInfo;
					ui_tb.Text = cFI.Name;
				}
				if (null != _ui_tbClipsFile.Tag && null != _ui_tbAdvertisementsFile.Tag && null != _ui_tbDesignsFile.Tag && null != _ui_dpAdvertisementBind.SelectedDate)
					_ui_btnMerge.IsEnabled = true;
				else
					_ui_btnMerge.IsEnabled = false;
			}
		}
		private void _ui_btnMerge_Click(object sender, RoutedEventArgs e)
		{
			if (null == _ui_tbClipsFile.Tag || null == _ui_tbAdvertisementsFile.Tag || null == _ui_tbDesignsFile.Tag)
			{
				btnErrorOn(true);
				_ui_btnErrors.Tag = g.Replica.sErrorPlaylistImport1;
				return;
			}
			_dlgProgress.Show();
			_dlgProgress.Tag = true;
			ProgressUpdate();

			btnErrorOn(false);
			_ui_btnErrors.Tag = null;

			byte[] aBytes = null;
			int nBytesReaded = -1;
			UploadInfo[] aUploadInfos = new UploadInfo[3];
			UploadInfo.nUploadCompleted = 0;
			UploadInfo.nParsingCompleted = 0;
			aUploadInfos[0] = (UploadInfo)_ui_tbClipsFile.Tag;
			if (null == aUploadInfos[0].sRemoteFile)
			{
				if (null == aBytes)
					aBytes = new byte[1024 * 1024];
				nBytesReaded = aUploadInfos[0].cFS.Read(aBytes, 0, aBytes.Length);
				_cDBI.UploadFileBeginAsync(aBytes.Take(nBytesReaded).ToArray(), aUploadInfos[0]);
			}
			else
				_cDBI.PowerGoldFileParseAsync(aUploadInfos[0].sRemoteFile);

			aUploadInfos[1] = (UploadInfo)_ui_tbAdvertisementsFile.Tag;
			if (null == aUploadInfos[1].sRemoteFile)
			{
				if (null == aBytes)
					aBytes = new byte[1024 * 1024];
				nBytesReaded = aUploadInfos[1].cFS.Read(aBytes, 0, aBytes.Length);
				_cDBI.UploadFileBeginAsync(aBytes.Take(nBytesReaded).ToArray(), aUploadInfos[1]);
			}
			else
				_cDBI.VideoInternationalFileParseAsync(aUploadInfos[1].sRemoteFile);

			aUploadInfos[2] = (UploadInfo)_ui_tbDesignsFile.Tag;
			if (null == aUploadInfos[2].sRemoteFile)
			{
				if (null == aBytes)
					aBytes = new byte[1024 * 1024];
				nBytesReaded = aUploadInfos[2].cFS.Read(aBytes, 0, aBytes.Length);
				_cDBI.UploadFileBeginAsync(aBytes.Take(nBytesReaded).ToArray(), aUploadInfos[2]);
			}
			else
				_cDBI.DesignFileParseAsync(aUploadInfos[2].sRemoteFile);
		}
		private void _ui_btnErrors_Click(object sender, RoutedEventArgs e)
		{
            string sErrors = g.Common.sErrorUnknown;
			if (null != _ui_btnErrors.Tag)
				sErrors = _ui_btnErrors.Tag.ToString();
			MsgBox cMsg = new MsgBox();
			cMsg.bTextIsReadOnly = true;
            cMsg.Show(g.Common.sErrors, g.Common.sError, MsgBox.MsgBoxButton.OK, sErrors);
		}
		private void _ui_btnLog_Click(object sender, RoutedEventArgs e)
		{
			string sLog = "Import Log";
			if (null != _ui_btnLog.Tag)
				sLog = _ui_btnLog.Tag.ToString();
			MsgBox cMsg = new MsgBox();
			cMsg.bTextIsReadOnly = true;
			cMsg.Show("", g.Common.sInformation, MsgBox.MsgBoxButton.OK, sLog);
		}
		private void _ui_btnExport_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog cSFD = new SaveFileDialog();
			//cSFD.Filter = "XML files | *.xml";
			cSFD.DefaultExt = ".xls";
			if ((bool)cSFD.ShowDialog())
			{
				#region excel
				String sExcelHeader = "<?xml version=\"1.0\"?>\r\n";
				#region Excel header
				sExcelHeader += "<?mso-application progid=\"Excel.Sheet\"?>\r\n";
				sExcelHeader += "<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"\r\n";
				sExcelHeader += " xmlns:o=\"urn:schemas-microsoft-com:office:office\"\r\n";
				sExcelHeader += " xmlns:x=\"urn:schemas-microsoft-com:office:excel\"\r\n";
                sExcelHeader += " xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"\r\n";
				sExcelHeader += " xmlns:html=\"http://www.w3.org/TR/REC-html40\">\r\n";
				sExcelHeader += " <DocumentProperties xmlns=\"urn:schemas-microsoft-com:office:office\">\r\n";
				sExcelHeader += "  <Author>Microsoft Corporation</Author>\r\n";
				sExcelHeader += "  <LastAuthor>bethe script</LastAuthor>\r\n";
				//        sExcelHeader+="  <LastPrinted>2005-02-28T08:42:44Z</LastPrinted>\r\n";
				sExcelHeader += "  <Created>1996-10-08T23:32:33Z</Created>\r\n";
				//        sExcelHeader+="  <LastSaved>2007-08-20T07:59:16Z</LastSaved>\r\n";
				sExcelHeader += "  <Version>11.5606</Version>\r\n";
				sExcelHeader += " </DocumentProperties>\r\n";
				sExcelHeader += " <ExcelWorkbook xmlns=\"urn:schemas-microsoft-com:office:excel\">\r\n";
				sExcelHeader += "  <WindowHeight>12135</WindowHeight>\r\n";
				sExcelHeader += "  <WindowWidth>9090</WindowWidth>\r\n";
				sExcelHeader += "  <WindowTopX>9855</WindowTopX>\r\n";
				sExcelHeader += "  <WindowTopY>-90</WindowTopY>\r\n";
				sExcelHeader += "  <ProtectStructure>False</ProtectStructure>\r\n";
				sExcelHeader += "  <ProtectWindows>False</ProtectWindows>\r\n";
				sExcelHeader += "  <DisplayInkNotes>False</DisplayInkNotes>\r\n";
				sExcelHeader += " </ExcelWorkbook>\r\n";
				#endregion

				String sExcelStyle = "<Styles>\r\n";
				#region Excel styles

				sExcelStyle += "  <Style ss:ID=\"header\">\r\n";
				sExcelStyle += "   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\"/>\r\n";
				sExcelStyle += "   <Borders>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "   </Borders>\r\n";
				sExcelStyle += "   <Font ss:FontName=\"Arial\" x:CharSet=\"204\" x:Family=\"Swiss\" ss:Size=\"12\" ss:Bold=\"1\"/>\r\n";
				sExcelStyle += "   <Interior ss:Color=\"#FFFFFF\" ss:Pattern=\"Solid\"/>\r\n";
				sExcelStyle += "   <NumberFormat ss:Format=\"[$-F400]h:mm:ss\\ AM/PM\"/>\r\n";
				sExcelStyle += "   <Protection/>\r\n";
				sExcelStyle += "  </Style>\r\n";

				sExcelStyle += " <Style ss:ID=\"dt\">\r\n";
				sExcelStyle += "   <Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Bottom\"/>\r\n";
				sExcelStyle += "    <Borders>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "   </Borders>\r\n";
				sExcelStyle += "   <Font ss:FontName=\"Arial\" x:CharSet=\"204\" x:Family=\"Swiss\" ss:Size=\"12\" ss:Bold=\"1\"/>\r\n";
				sExcelStyle += "   <Interior ss:Color=\"#D8D8D8\" ss:Pattern=\"Solid\"/>\r\n";
				sExcelStyle += "   <NumberFormat ss:Format=\"yyyy\\-mm\\-dd\\ hh:mm:ss\"/>\r\n";
				sExcelStyle += "   <Protection/>\r\n";
				sExcelStyle += " </Style>\r\n";

				sExcelStyle += " <Style ss:ID=\"pli\">\r\n";
				sExcelStyle += "   <Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Bottom\"/>\r\n";
				sExcelStyle += "   <Borders>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "   </Borders>\r\n";
				sExcelStyle += "   <Font ss:FontName=\"Arial\" x:CharSet=\"204\" x:Family=\"Swiss\" ss:Size=\"12\" ss:Bold=\"1\"/>\r\n";
				sExcelStyle += "   <Interior ss:Color=\"#DBE5F1\" ss:Pattern=\"Solid\"/>\r\n";
				sExcelStyle += "   <NumberFormat/>\r\n";
				sExcelStyle += "   <Protection/>\r\n";
				sExcelStyle += " </Style>\r\n";

				sExcelStyle += " </Styles>\r\n";
				#endregion

				if (DateTime.MaxValue == _dtDateTimeLastSelected)
					_dtDateTimeLastSelected = DateTime.Now;
				String sExcelBody = "<Worksheet ss:Name=\"replica_pl_export_" + _dtDateTimeLastSelected.ToString("yyyy-MM-dd") + "\">\r\n";
				sExcelBody += "<Table>\r\n";
				sExcelBody += "<Row>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Helper.sEventStart + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Common.sName.ToLower() + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Helper.sFramesQty + "</Data></Cell>\r\n";
				sExcelBody += "</Row>\r\n";

				DateTime dt = DateTime.MaxValue;
				foreach (PlaylistItem cPLI in _ui_dgPlaylist.ItemsSource)
				{
					if (DateTime.MaxValue > cPLI.dtStartHard || DateTime.MaxValue > cPLI.dtStartSoft)
					{
						sExcelBody += "<Row>\r\n";
						if (cPLI.dtStartPlanned < DateTime.Parse("1899-12-31"))
							cPLI.dtStartPlanned = cPLI.dtStartPlanned.AddYears(1899);
						sExcelBody += "<Cell ss:StyleID=\"dt\">";
						if (DateTime.MaxValue > cPLI.dtStartHard)
							sExcelBody += "<Data ss:Type=\"DateTime\">" + cPLI.dtStartHard.ToString("yyyy-MM-ddTHH:mm:ss.000") + "</Data>";
						else if (5 < cPLI.dtStartSoft.Subtract(dt).TotalMinutes)
							sExcelBody += "<Data ss:Type=\"DateTime\">" + cPLI.dtStartSoft.ToString("yyyy-MM-ddTHH:mm:ss.000") + "</Data>";
						sExcelBody += "</Cell>\r\n";
						sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cPLI.sName.Replace("&", "&amp;") + "</Data></Cell>\r\n";
						sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cPLI.nFramesQty.ToString() + "</Data></Cell>\r\n";
						sExcelBody += "</Row>\r\n";

						if (DateTime.MaxValue > cPLI.dtStartHard)
							dt = cPLI.dtStartHard;
						else
							dt = cPLI.dtStartSoft;
					}
				}

				String sExcelFooter = "  </Table>\r\n";
				sExcelFooter += " </Worksheet>\r\n";
				sExcelFooter += "</Workbook>\r\n";
				String sExcel = sExcelHeader + sExcelStyle + sExcelBody + sExcelFooter;
				#endregion

				System.Text.Encoding encoding = System.Text.Encoding.GetEncoding("utf-8");
				byte[] aBuffer = encoding.GetBytes(sExcel);
				System.IO.StreamWriter cStreamWriter = new System.IO.StreamWriter(cSFD.OpenFile());
				cStreamWriter.Write(sExcel);
				cStreamWriter.Flush();
				cStreamWriter.Close();
			}
		}

		private void _ui_dpAdvertisementBind_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			bool bMergeButtonIsEnabled = false;
			if (null != _ui_dpAdvertisementBind.SelectedDate)
			{
				_ui_dpAdvertisementBind.SelectedDate = _ui_dpAdvertisementBind.SelectedDate.Value.Subtract(_ui_dpAdvertisementBind.SelectedDate.Value.TimeOfDay);
				if (null != _ui_tbClipsFile.Tag && null != _ui_tbAdvertisementsFile.Tag && null != _ui_tbDesignsFile.Tag)
					bMergeButtonIsEnabled = true;
			}
			_ui_btnMerge.IsEnabled = bMergeButtonIsEnabled;
		}
		#endregion
		#region DBI
		void _cDBI_UploadFileBeginCompleted(object sender, UploadFileBeginCompletedEventArgs e)
		{
			if (null != e.UserState)
			{
				UploadInfo cUploadInfo = (UploadInfo)e.UserState;
				cUploadInfo.nHandle = e.Result;
				UploadFileContinue(cUploadInfo);
			}
		}
		void _cDBI_UploadFileContinueCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (null != e.UserState)
				UploadFileContinue((UploadInfo)e.UserState);
		}
		void _cDBI_UploadFileEndCompleted(object sender, UploadFileEndCompletedEventArgs e)
		{
			UploadInfo.nUploadCompleted++;
			if (null != e.UserState)
			{
				ProgressUpdate();
				UploadInfo cUploadInfo = (UploadInfo)e.UserState;
				cUploadInfo.cFS.Close();
				cUploadInfo.sRemoteFile = e.Result;
				cUploadInfo.nHandle = -1;
				if (cUploadInfo == _ui_tbClipsFile.Tag)
					_cDBI.PowerGoldFileParseAsync(cUploadInfo.sRemoteFile, cUploadInfo);
				else if (cUploadInfo == _ui_tbAdvertisementsFile.Tag)
					_cDBI.VideoInternationalFileParseAsync(cUploadInfo.sRemoteFile, cUploadInfo);
				else if (cUploadInfo == _ui_tbDesignsFile.Tag)
					_cDBI.DesignFileParseAsync(cUploadInfo.sRemoteFile, cUploadInfo);
			}
		}
		void _cDBI_PowerGoldFileParseCompleted(object sender, PowerGoldFileParseCompletedEventArgs e)
		{
			try
			{
				UploadInfo.nParsingCompleted++;
				if (null != e.UserState)
				{
					UploadInfo cUploadInfo = (UploadInfo)e.UserState;
					cUploadInfo.nHandle = e.Result;
					if (2 < UploadInfo.nParsingCompleted)
						_cDBI.PlaylistsMergeAsync(((UploadInfo)_ui_tbClipsFile.Tag).nHandle, ((UploadInfo)_ui_tbAdvertisementsFile.Tag).nHandle, _ui_dpAdvertisementBind.SelectedDate.Value, ((UploadInfo)_ui_tbDesignsFile.Tag).nHandle);
					ProgressUpdate();
				}
			}
			catch { }
		}
		void _cDBI_VideoInternationalFileParseCompleted(object sender, VideoInternationalFileParseCompletedEventArgs e)
		{
			try
			{
				UploadInfo.nParsingCompleted++;
				if (null != e.UserState)
				{
					UploadInfo cUploadInfo = (UploadInfo)e.UserState;
					cUploadInfo.nHandle = e.Result;
					if (2 < UploadInfo.nParsingCompleted)
						_cDBI.PlaylistsMergeAsync(((UploadInfo)_ui_tbClipsFile.Tag).nHandle, ((UploadInfo)_ui_tbAdvertisementsFile.Tag).nHandle, _ui_dpAdvertisementBind.SelectedDate.Value, ((UploadInfo)_ui_tbDesignsFile.Tag).nHandle);
					ProgressUpdate();
				}
			}
			catch { }
		}
		void _cDBI_DesignFileParseCompleted(object sender, DesignFileParseCompletedEventArgs e)
		{
			try
			{
				UploadInfo.nParsingCompleted++;
				if (null != e.UserState)
				{
					UploadInfo cUploadInfo = (UploadInfo)e.UserState;
					cUploadInfo.nHandle = e.Result;
					if (2 < UploadInfo.nParsingCompleted)
						_cDBI.PlaylistsMergeAsync(((UploadInfo)_ui_tbClipsFile.Tag).nHandle, ((UploadInfo)_ui_tbAdvertisementsFile.Tag).nHandle, _ui_dpAdvertisementBind.SelectedDate.Value, ((UploadInfo)_ui_tbDesignsFile.Tag).nHandle);
					ProgressUpdate();
				}
			}
			catch { }
		}
		void _cDBI_PlaylistsMergeCompleted(object sender, PlaylistsMergeCompletedEventArgs e)
		{
			try
			{
                _dlgProgress.sText = g.Helper.sFinishing + "...";
				UploadInfo.nUploadCompleted = 0;
				UploadInfo.nParsingCompleted = 0;
				_ui_dgPlaylist.ItemsSource = e.Result;
				if (0 < e.Result.Length)
				{
					aPlaylistItems = e.Result;
					_ui_btnOK.IsEnabled = true;
                    _ui_btnOK.Background = Coloring.Notifications.cButtonNormal;
                    _ui_btnExport.IsEnabled = true;
				}
			}
			catch { }
			_ui_tbClipsFile.Tag = null;
			_ui_tbClipsFile.Text = "";

			_ui_tbAdvertisementsFile.Tag = null;
			_ui_tbAdvertisementsFile.Text = "";

			if (_ui_dpAdvertisementBind.SelectedDate.HasValue)
				_dtDateTimeLastSelected = _ui_dpAdvertisementBind.SelectedDate.Value;
			else
				_dtDateTimeLastSelected = DateTime.MaxValue;
			_ui_dpAdvertisementBind.SelectedDate = null;

			_ui_tbDesignsFile.Tag = null;
			_ui_tbDesignsFile.Text = "";

			_ui_btnMerge.IsEnabled = false;

			_cDBI.ErrorsGetAsync();

		}
		void _cDBI_ErrorsGetCompleted(object sender, ErrorsGetCompletedEventArgs e)
		{
			_cDBI.ImportLogGetAsync();
			if (null != e.Result && 0 < e.Result.Length)
			{
				_cDBI.ErrorsClearAsync();
				btnErrorOn(true);
				string sErrors = "";
				for (int nIndx = 0; e.Result.Length > nIndx; nIndx++)
					sErrors += e.Result[nIndx].sMessage.Remove("<br>");
				_ui_btnErrors.Tag = sErrors;
				//_ui_btnOK.IsEnabled = true;
                _ui_btnOK.Background = Coloring.Notifications.cButtonError;
            }
			_dlgProgress.Close();
		}
		private void _cDBI_ImportLogGetCompleted(object sender, ImportLogGetCompletedEventArgs e)
		{
			_ui_btnLog.Tag = e.Result;
			if (e.Result != null && e.Result.Length > 0)
				_ui_btnLog.IsEnabled = true;
			else
				_ui_btnLog.IsEnabled = false;
		}

		//void _cDBI_DBCredentialsUnsetCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		//{
		//    try
		//    {
		//        base.OnClosed((EventArgs)e.UserState);
		//        _dlgProgress.Close();
		//    }
		//    catch { }
		//}
		#endregion
		#endregion
		private void btnErrorOn(bool bOn)
		{
			if (bOn)
			{
				_ui_btnErrors.IsEnabled = true;
				_ui_btnErrors.Background = controls.sl.Coloring.Notifications.cButtonError;
				_ui_btnErrors.Foreground = controls.sl.Coloring.Notifications.cErrorForeground;
			}
			else
			{
				_ui_btnErrors.IsEnabled = false;
				_ui_btnErrors.Background = controls.sl.Coloring.Notifications.cButtonInactive;
				_ui_btnErrors.Foreground = controls.sl.Coloring.Notifications.cNormalForeground;
			}
		}
		private void UploadFileContinue(UploadInfo cUploadInfo)
		{
			byte[] aBytes = new byte[1024 * 1024];
			int nBytesReaded = cUploadInfo.cFS.Read(aBytes, 0, aBytes.Length);
			if (0 < nBytesReaded)
				_cDBI.UploadFileContinueAsync(cUploadInfo.nHandle, aBytes.Take(nBytesReaded).ToArray(), cUploadInfo);
			else
				_cDBI.UploadFileEndAsync(cUploadInfo.nHandle, cUploadInfo);
		}
		private void ProgressUpdate()
		{
			lock (_dlgProgress.Tag)
			{
				if (3 > UploadInfo.nUploadCompleted)
                    _dlgProgress.sText = g.Common.sFilesUploading + ": " + (UploadInfo.nUploadCompleted * 33) + "%";
				else if (3 > UploadInfo.nParsingCompleted)
                    _dlgProgress.sText = g.Helper.sUploadedFilesAnalyzing + ": " + (UploadInfo.nParsingCompleted * 33) + "%";
				else
                    _dlgProgress.sText = g.Helper.sPlaylistsIntegration + "...";
			}
		}

	}
}

