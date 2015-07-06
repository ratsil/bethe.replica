using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

using controls.sl;
using controls.childs.sl;
using controls.childs.replica.sl;
using helpers.replica.services.dbinteract;
using controls.extensions.sl;
using helpers.extensions;
using g = globalization;

namespace replica.sl
{
	public partial class templates : Page
	{
		private DBInteract _cDBI;
		private MsgBox _cMsgBox;
		private Progress _dlgProgress;
		private List<TextBox> _aChangedTBs;
		private int _nTotalMessages, _nTotalCrawls;
		private Template[] _aMesages;
		private RegisteredTable _cCuesTemplate;
		private DataGrid _ui_dgTrails;
		private List<TemplateBind> _aTemplateBindsTrails;
		private List<TemplatesScheduleSL> _aTSISL_CurrentDG;
		private List<TemplatesScheduleSL> _aTSISL_ToDelete;
		private List<TemplatesScheduleSL> _aTSISL_Changed;
		private TemplatesScheduleSL _cTSICurrent;
		private MenuItem _ui_cmiTrails_Delete;
		private TextBox _tbSemafor;
		ContextMenu _ui_cmTrails;
		public templates()
		{
			InitializeComponent();

            Title = g.Helper.sTemplates;
            _dlgProgress = new Progress();
			_aChangedTBs = new List<TextBox>();
			_cDBI = new DBInteract();
			_aTemplateBindsTrails = new List<TemplateBind>();
			_aTSISL_ToDelete = new List<TemplatesScheduleSL>();
			_aTSISL_Changed = new List<TemplatesScheduleSL>();
			_cMsgBox = new MsgBox();
			_cDBI.MacrosCrawlsGetCompleted += new EventHandler<MacrosCrawlsGetCompletedEventArgs>(_cDBI_MacrosCrawlsGetCompleted);
			_cDBI.TemplatesScheduleGetCompleted += new EventHandler<TemplatesScheduleGetCompletedEventArgs>(_cDBI_TemplatesScheduleGetCompleted);
			_cDBI.TemplateBindsTrailsGetCompleted += new EventHandler<TemplateBindsTrailsGetCompletedEventArgs>(_cDBI_TemplateBindsTrailsGetCompleted);
			_cDBI.TemplatesScheduleAddCompleted += new EventHandler<TemplatesScheduleAddCompletedEventArgs>(_cDBI_TemplatesScheduleAddCompleted);
			_cDBI.TemplatesScheduleDeleteCompleted += new EventHandler<TemplatesScheduleDeleteCompletedEventArgs>(_cDBI_TemplatesScheduleDeleteCompleted);
			_cDBI.TempateMessagesGetCompleted += new EventHandler<TempateMessagesGetCompletedEventArgs>(_cDBI_TempateMessagesGetCompleted);
			_cDBI.TemplateMessagesTextGetCompleted += new EventHandler<TemplateMessagesTextGetCompletedEventArgs>(_cDBI_TemplateMessagesTextGetCompleted);
			_cDBI.TemplateMessagesTextSaveCompleted += new EventHandler<TemplateMessagesTextSaveCompletedEventArgs>(_cDBI_TemplateMessagesTextSaveCompleted);
			_cDBI.TemplateRegisteredTableGetCompleted += new EventHandler<TemplateRegisteredTableGetCompletedEventArgs>(_cDBI_TemplateRegisteredTableGetCompleted);

			_dlgProgress.Show();

			#region making of _ui_spTrails
			_ui_spTrails.Children.Remove(_ui_btnSaveTrails);
			_ui_spTrails.Children.Remove(_ui_spAddTrail);
			_ui_spTrails.Children.Remove(_ui_spFolder);
			_ui_spTrails.Children.Remove(_ui_spLine1);
			_ui_spTrails.Children.Remove(_ui_spLine2);

			System.Windows.Controls.Grid ui_gTrails;
			ui_gTrails = new System.Windows.Controls.Grid();
			ui_gTrails.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
			ui_gTrails.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
			_ui_spTrails.Children.Add(ui_gTrails);
			int ni = 1;
			int _nTotalMessages = 1;
			ui_gTrails.Margin = new Thickness(30, 6, 30, ni == _nTotalMessages ? 6 : 0);
			ui_gTrails.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

			_ui_dgTrails = new DataGrid();
			_ui_dgTrails.AutoGenerateColumns = false;
			_ui_dgTrails.CanUserReorderColumns = false;
			_ui_dgTrails.CanUserResizeColumns = true;
			_ui_dgTrails.CanUserSortColumns = false;
			_ui_dgTrails.IsReadOnly = true;
			_ui_dgTrails.MouseRightButtonDown += new MouseButtonEventHandler(ui_dgTrails_MouseRightButtonDown);
			_ui_dgTrails.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgTrails_LoadingRow);
			ui_gTrails.Children.Add(_ui_dgTrails);
			System.Windows.Controls.Grid.SetRow(_ui_dgTrails, 1);
			System.Windows.Controls.DataGridTextColumn column;

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Helper.sTemplateName;
			//column.HeaderStyle = BuildColumnHeaderStyle(description);
			//column.Binding.StringFormat = "0.00";
			//column.CellStyle = BuildFloatCellStyle(fieldName, description);
			column.Binding = new System.Windows.Data.Binding("cTemplatesBind.cTemplate.sName");
			column.Width = new DataGridLength(100, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Helper.sSequence;
			column.Binding = new System.Windows.Data.Binding("sFoldername");
			column.Width = new DataGridLength(120, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Helper.sLine1;
			column.Binding = new System.Windows.Data.Binding("sLine01");
			column.Width = new DataGridLength(300, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Helper.sLine2;
			column.Binding = new System.Windows.Data.Binding("sLine02");
			column.Width = new DataGridLength(300, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Helper.sStart;
			column.Binding = new System.Windows.Data.Binding("sdtStart");
			column.Width = new DataGridLength(150, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Common.sWeekLetter1;
			column.Binding = new System.Windows.Data.Binding("sMon");
			column.Width = new DataGridLength(28, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Common.sWeekLetter2;
			column.Binding = new System.Windows.Data.Binding("sTue");
			column.Width = new DataGridLength(28, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Common.sWeekLetter3;
			column.Binding = new System.Windows.Data.Binding("sWed");
			column.Width = new DataGridLength(28, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Common.sWeekLetter4;
			column.Binding = new System.Windows.Data.Binding("sThu");
			column.Width = new DataGridLength(28, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Common.sWeekLetter5;
			column.Binding = new System.Windows.Data.Binding("sFri");
			column.Width = new DataGridLength(28, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Common.sWeekLetter6;
			column.Binding = new System.Windows.Data.Binding("sSat");
			column.Width = new DataGridLength(28, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
			column.Header = "";
			column.Binding = new System.Windows.Data.Binding("sSun");
			column.Width = new DataGridLength(28, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			column = new System.Windows.Controls.DataGridTextColumn();
            column.Header = g.Helper.sStop;
			column.Binding = new System.Windows.Data.Binding("sdtStop");
			column.Width = new DataGridLength(150, DataGridLengthUnitType.Pixel);
			_ui_dgTrails.Columns.Add(column);

			_ui_cmTrails = new ContextMenu();
			_ui_cmTrails.Opened += new RoutedEventHandler(cCM_Opened);
			_ui_cmTrails.Closed += new RoutedEventHandler(cCM_Closed);
            _ui_cmiTrails_Delete = new MenuItem() { Header = g.Common.sDelete, Name = "_ui_cmTrailDelete", IsEnabled = false };
			_ui_cmiTrails_Delete.Click += new RoutedEventHandler(cMI_Delete_Click);
			_ui_cmTrails.Items.Add(_ui_cmiTrails_Delete);

			_ui_dgTrails.SetValue(ContextMenuService.ContextMenuProperty, _ui_cmTrails);

			_ui_spTrails.Children.Add(_ui_spAddTrail);
			_ui_spTrails.Children.Add(_ui_spFolder);
			_ui_spTrails.Children.Add(_ui_spLine1);
			_ui_spTrails.Children.Add(_ui_spLine2);
			_ui_spTrails.Children.Add(_ui_btnSaveTrails);
			#endregion

			_ui_btnSaveTrails.Visibility = System.Windows.Visibility.Collapsed;

			_cDBI.TemplateRegisteredTableGetAsync();
		}

		#region DBI
		void _cDBI_TemplateRegisteredTableGetCompleted(object sender, TemplateRegisteredTableGetCompletedEventArgs e)
		{
			if (null == e.Result)
			{
				_cMsgBox.ShowError(g.Replica.sErrorTemplates1);
				return;
			}
			_cCuesTemplate = e.Result;
			_cDBI.TempateMessagesGetAsync();
		}
		void _cDBI_TempateMessagesGetCompleted(object sender, TempateMessagesGetCompletedEventArgs e)
		{
			_nTotalMessages = e.Result.Length;
			_aMesages = e.Result;
			_cDBI.TemplateMessagesTextGetAsync(_aMesages);
		}
		void _cDBI_TemplateMessagesTextGetCompleted(object sender, TemplateMessagesTextGetCompletedEventArgs e)
		{
			System.Windows.Controls.Grid ui_gCues;
			TextBox cTB;
			Label cL;
			DictionaryElement cDE;
			string sLine;
			int ni = 0;
			foreach (Template cT in _aMesages)
			{
				ni++;				
				ui_gCues = new System.Windows.Controls.Grid();
				ui_gCues.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
				ui_gCues.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
				_ui_spMessages.Children.Remove(_ui_btnSaveMessages);
				_ui_spMessages.Children.Add(ui_gCues);
				ui_gCues.Margin = new Thickness(30, 6, 30, ni == _nTotalMessages ? 6 : 0);
				//ui_gCues.Width = 800;
				ui_gCues.Name = ni.ToString();
				ui_gCues.Background = Coloring.Notifications.cTextBoxActive;
				ui_gCues.Children.Add(new Label() { Content = "message-" + ni + ".xml", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(6, 0, 0, 0), Height = 28, VerticalAlignment = System.Windows.VerticalAlignment.Top, HorizontalAlignment = System.Windows.HorizontalAlignment.Left });

				cDE = e.Result.FirstOrDefault(o => o.nTargetID == cT.nID && o.sKey == "line#0");
				sLine = cDE == null || " " == cDE.sValue ? "" : cDE.sValue;
				cDE = null == cDE ? new DictionaryElement() { nID = -1, nRegisteredTablesID = _cCuesTemplate.nID, nTargetID = cT.nID, sKey = "line#0", sValue = " " } : cDE;

				cTB = new TextBox() { Text = sLine, Tag = cDE, FontSize = 11, Margin = new Thickness(0, 26, 0, 0), Height = 23, Width = 240, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Top, Background = new SolidColorBrush() { Color = Colors.White } };
				cTB.TextChanged += new TextChangedEventHandler(cTB_TextChanged);
				ui_gCues.Children.Add(cTB);
				System.Windows.Controls.Grid.SetColumn(cTB, 0);
                cL = new Label() { Content = g.Helper.sLine1.ToLower(), FontSize = 10, Margin = new Thickness(0, 47, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Top };
				ui_gCues.Children.Add(cL);
				System.Windows.Controls.Grid.SetColumn(cL, 0);
				if (-1 == cDE.nID)
					_aChangedTBs.Add(cTB);

				cDE = e.Result.FirstOrDefault(o => o.nTargetID == cT.nID && o.sKey == "line#1");
				sLine = cDE == null || " " == cDE.sValue ? "" : cDE.sValue;
				cDE = null == cDE ? new DictionaryElement() { nID = -1, nRegisteredTablesID = _cCuesTemplate.nID, nTargetID = cT.nID, sKey = "line#1", sValue = " " } : cDE;

				cTB = new TextBox() { Text = sLine, Tag = cDE, FontSize = 11, Margin = new Thickness(0, 26, 0, 0), Height = 23, Width = 240, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Top, Background = new SolidColorBrush() { Color = Colors.White } };
				cTB.TextChanged += new TextChangedEventHandler(cTB_TextChanged);
				ui_gCues.Children.Add(cTB);
				System.Windows.Controls.Grid.SetColumn(cTB, 1);
                cL = new Label() { Content = g.Helper.sLine2.ToLower(), FontSize = 10, Margin = new Thickness(0, 47, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Top };
				ui_gCues.Children.Add(cL);
				System.Windows.Controls.Grid.SetColumn(cL, 1);
				if (-1 == cDE.nID)
					_aChangedTBs.Add(cTB);
			}
			_ui_spMessages.Children.Add(_ui_btnSaveMessages);

			_cDBI.TemplateBindsTrailsGetAsync();
		}
		void _cDBI_TemplateBindsTrailsGetCompleted(object sender, TemplateBindsTrailsGetCompletedEventArgs e)
		{
			_aTemplateBindsTrails.AddRange(e.Result);
			_ui_cbTrailName.ItemsSource = _aTemplateBindsTrails;
			AddTrailReset();

			_cDBI.MacrosCrawlsGetAsync();
		}
		void _cDBI_MacrosCrawlsGetCompleted(object sender, MacrosCrawlsGetCompletedEventArgs e)
		{
			_nTotalCrawls = e.Result.Length;
			System.Windows.Controls.Grid ui_gCues;
			TextBox cTB;
			Label cL;
			for (int ni = 1; _nTotalCrawls >= ni; ni++)
			{
				ui_gCues = new System.Windows.Controls.Grid();
				ui_gCues.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
				_ui_spCrawls.Children.Remove(_ui_btnSaveCrawls);
				_ui_spCrawls.Children.Add(ui_gCues);
				ui_gCues.Margin = new Thickness(30, 6, 30, ni == _nTotalCrawls ? 6 : 0);
				//ui_gCues.Width = 800;
				ui_gCues.Background = Coloring.Notifications.cTextBoxActive;
				ui_gCues.Children.Add(new Label() { Content = "crawl-" + ni + ".xml", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(6, 0, 0, 0), Height = 28, VerticalAlignment = System.Windows.VerticalAlignment.Top, HorizontalAlignment = System.Windows.HorizontalAlignment.Left });

				cTB = new TextBox() { Text = e.Result[ni - 1].sValue, Tag = e.Result[ni - 1], FontSize = 11, Margin = new Thickness(0, 26, 0, 0), Height = 23, Width = 680, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Top, Background = new SolidColorBrush() { Color = Colors.White } };
				cTB.TextChanged += new TextChangedEventHandler(cTB_TextChanged);
				ui_gCues.Children.Add(cTB);
				System.Windows.Controls.Grid.SetColumn(cTB, 0);
                cL = new Label() { Content = g.Helper.sCrawl.ToLower(), FontSize = 10, Margin = new Thickness(0, 47, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Top };
				ui_gCues.Children.Add(cL);
				System.Windows.Controls.Grid.SetColumn(cL, 0);
			}
			_ui_spCrawls.Children.Add(_ui_btnSaveCrawls);

			_cDBI.TemplatesScheduleGetAsync(_aTemplateBindsTrails.ToArray(), DateTime.Today.GetMonday());
		}
		void _cDBI_TemplatesScheduleGetCompleted(object sender, TemplatesScheduleGetCompletedEventArgs e)
		{
			_aTSISL_CurrentDG = TemplatesScheduleSL.GetTemplatesScheduleSLs(e.Result).ToList();

			TrailTableRefrash();
			_dlgProgress.Close();
			_ui_dgTrails.IsEnabled = true;
		}
		void _cDBI_TemplateMessagesTextSaveCompleted(object sender, TemplateMessagesTextSaveCompletedEventArgs e)
		{
			if (!e.Result)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(g.Replica.sErrorTemplates3.Fmt(Environment.NewLine));
			}
			else
			{
				_ui_spMessages.Children.Clear();
				_ui_spMessages.Children.Add(_ui_btnSaveMessages);
				_ui_spCrawls.Children.Clear();
				_ui_spCrawls.Children.Add(_ui_btnSaveCrawls);
				_aChangedTBs.Clear();
				_aMesages = null;
				_cDBI.TempateMessagesGetAsync();
			}
		}
		void _cDBI_TemplatesScheduleDeleteCompleted(object sender, TemplatesScheduleDeleteCompletedEventArgs e)
		{
			if (e.Result)
			{
				_aTSISL_ToDelete.Clear();

				if (0 < _aTSISL_Changed.Count)
					_cDBI.TemplatesScheduleAddAsync(TemplatesScheduleSL.GetTemplatesSchedule(_aTSISL_Changed.ToArray()));
				else
					_cDBI.TemplatesScheduleGetAsync(_aTemplateBindsTrails.ToArray(), DateTime.Today.GetMonday());
			}
			else
			{
				_cMsgBox.Closed += new EventHandler(_cMsgBox_Closed);
                _cMsgBox.ShowError(g.Common.sErrorDelete + ". " + g.Replica.sErrorTemplates2);
				_ui_dgTrails.IsEnabled = true;
			}
		}
		void _cMsgBox_Closed(object sender, EventArgs e)
		{
			_dlgProgress.Show();
			_dlgProgress.Close();
			System.Threading.Thread.Sleep(500);
			_cMsgBox.Closed -= _cMsgBox_Closed;
		}
		void _cDBI_TemplatesScheduleAddCompleted(object sender, TemplatesScheduleAddCompletedEventArgs e)
		{
			if (!e.Result)
			{
				_cMsgBox.Closed += new EventHandler(_cMsgBox_Closed);
                _cMsgBox.ShowError(g.Common.sErrorSave + ". " + g.Replica.sErrorTemplates2);
				_ui_dgTrails.IsEnabled = true;
			}
			else
			{
				_aTSISL_Changed.Clear();
				_cDBI.TemplatesScheduleGetAsync(_aTemplateBindsTrails.ToArray(), DateTime.Today.GetMonday());
			}
		}
		#endregion

		#region UI
		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
		}
		void cTB_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox cTB = (TextBox)sender;

			if (cTB == _tbSemafor && "" == cTB.Text)
			{
				_tbSemafor = null;
				return;
			}
			if (" " == cTB.Text)
			{
				cTB.Text = "";
				_tbSemafor = cTB;
				return;
			}

			if (null == _aChangedTBs.FirstOrDefault(o => o == cTB))
			{
				if (cTB.Text != ((DictionaryElement)cTB.Tag).sValue)
					_aChangedTBs.Add(cTB);
			}
			else
				if (cTB.Text == ((DictionaryElement)cTB.Tag).sValue)
					_aChangedTBs.Remove(cTB);

			MarkGrid((System.Windows.Controls.Grid)cTB.Parent);
			//((System.Windows.Controls.StackPanel)((System.Windows.Controls.Grid)cTB.Parent).Parent).Name
		}
		void MarkGrid(System.Windows.Controls.Grid cG)
		{
			bool isChanged = false;
			TextBox cTB;
			foreach (UIElement cElem in cG.Children)
				if (cElem is TextBox)
				{
					cTB = (TextBox)cElem;
					if (cTB.Text != ((DictionaryElement)cTB.Tag).sValue)
						isChanged = true;
				}
			if (isChanged)
			{
				cG.Background = Coloring.Notifications.cTextBoxChanged;
			}
			else
			{
				cG.Background = Coloring.Notifications.cTextBoxActive;
			}
		}
		private void ButtonSaveClick(object sender, RoutedEventArgs e)
		{
			_dlgProgress.Show();
			List<DictionaryElement> aChangedLines = new List<DictionaryElement>();
			DictionaryElement cDE;
			foreach (TextBox cTB in _aChangedTBs)
			{
				cDE = (DictionaryElement)cTB.Tag;
				cDE.sValue = "" == cTB.Text ? " " : cTB.Text;
				aChangedLines.Add(cDE);
			}
			_cDBI.TemplateMessagesTextSaveAsync(aChangedLines.ToArray());
		}
		private void _ui_btnSaveTrails_Click(object sender, RoutedEventArgs e)
		{
			_ui_dgTrails.IsEnabled = false;
			_aTSISL_ToDelete.AddRange(_aTSISL_Changed);
			TemplatesSchedule[] aToDel = TemplatesScheduleSL.GetTemplatesScheduleSource(_aTSISL_ToDelete.ToArray());

			if (0 < aToDel.Length)
				_cDBI.TemplatesScheduleDeleteAsync(aToDel);
			else
				_cDBI.TemplatesScheduleAddAsync(TemplatesScheduleSL.GetTemplatesSchedule(_aTSISL_Changed.ToArray()));
		}
		private void _ui_btnAddTrail_Click(object sender, RoutedEventArgs e)
		{
			TemplatesScheduleSL cTSI_New = new TemplatesScheduleSL();
			cTSI_New.IsChanged = true;
			cTSI_New.cTemplatesBind = (TemplateBind)_ui_cbTrailName.SelectedItem;
            cTSI_New.sMon = null == _ui_cbMon.IsChecked || !_ui_cbMon.IsChecked.Value ? null : g.Common.sMon;
            cTSI_New.sTue = null == _ui_cbTue.IsChecked || !_ui_cbTue.IsChecked.Value ? null : g.Common.sTue;
            cTSI_New.sWed = null == _ui_cbWed.IsChecked || !_ui_cbWed.IsChecked.Value ? null : g.Common.sWed;
            cTSI_New.sThu = null == _ui_cbThu.IsChecked || !_ui_cbThu.IsChecked.Value ? null : g.Common.sThu;
            cTSI_New.sFri = null == _ui_cbFri.IsChecked || !_ui_cbFri.IsChecked.Value ? null : g.Common.sFri;
            cTSI_New.sSat = null == _ui_cbSat.IsChecked || !_ui_cbSat.IsChecked.Value ? null : g.Common.sSat;
            cTSI_New.sSun = null == _ui_cbSun.IsChecked || !_ui_cbSun.IsChecked.Value ? null : g.Common.sSun;
			DateTime dtTmp=_ui_tpTrailStart.Value.Value;
			cTSI_New.dtStart = _ui_dpTrailStart.SelectedDate.Value.AddHours(dtTmp.Hour).AddMinutes(dtTmp.Minute).AddSeconds(dtTmp.Second);
            cTSI_New.dtStart = cTSI_New.dtStart.GetMonday();
			if (_ui_tpTrailStop.Value != null && _ui_dpTrailStop.Text != "")
			{
				dtTmp = _ui_tpTrailStop.Value.Value;
				cTSI_New.dtStop = _ui_dpTrailStop.SelectedDate.Value.AddHours(dtTmp.Hour).AddMinutes(dtTmp.Minute).AddSeconds(dtTmp.Second);
			}
			else
				cTSI_New.dtStop = DateTime.MaxValue;
			cTSI_New.sPath = Preferences.cServer.sTrailersPath + _ui_tbTrailPath.Text;
			cTSI_New.sFoldername = _ui_tbTrailPath.Text;
			cTSI_New.sLine01 = _ui_tbTrailLine1.Text;
			cTSI_New.sLine02 = _ui_tbTrailLine2.Text;

			foreach (TemplatesScheduleSL cTSISL in _aTSISL_CurrentDG)
				if (cTSISL.dtStart.TimeOfDay == cTSI_New.dtStart.TimeOfDay)
				{
					foreach (TemplatesSchedule cTSI in cTSISL.aTSI_Source)
						if (cTSI_New.IsThisDayChacked(cTSI.dtStart.DayOfWeek) && cTSI.dtStop > cTSI_New.dtStart)
						{
							_cMsgBox.Closed += new EventHandler(_cMsgBox_Closed);
							_cMsgBox.ShowError(g.Replica.sErrorTemplates4);
							return;
						}
				}
			_aTSISL_CurrentDG.Add(cTSI_New);
			_aTSISL_Changed.Add(cTSI_New);
			_ui_dgTrails.SelectedItem = cTSI_New;

			TrailTableRefrash();
			_ui_btnSaveTrails_Click(null, null);

			AddTrailReset();
		}
		private void TrailTableRefrash()
		{
			_ui_dgTrails.ItemsSource = null;
			_aTSISL_CurrentDG.Sort(TSISL_Compare);
			_ui_dgTrails.ItemsSource = _aTSISL_CurrentDG;
			_ui_dgTrails.UpdateLayout();
		}
		private int TSISL_Compare(TemplatesScheduleSL a, TemplatesScheduleSL b)
		{
			int nRetVal = a.cTemplatesBind.nID.CompareTo(b.cTemplatesBind.nID);
			if (0 == nRetVal)
				nRetVal = a.dtStart.TimeOfDay.CompareTo(b.dtStart.TimeOfDay);
			return nRetVal;
		}
		private void AddTrailReset()
		{
			_ui_cbTrailName.SelectedValue = _aTemplateBindsTrails[0];
			_ui_dpTrailStart.SelectedDate = DateTime.Today.GetMonday();
			_ui_dpTrailStart_SelectedDateChanged(null, null);
			_ui_dpTrailStop.ClearValue(DatePicker.SelectedDateProperty);
			_ui_tpTrailStart.Value = null;
			_ui_tpTrailStop.Value = null;
			MakeBtnAddTrailReady();
		}
		private void _ui_cbTrailName_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			MakeBtnAddTrailReady();
		}
		private void _ui_dpTrailStart_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			_ui_cbMon.IsChecked = false;
			_ui_cbTue.IsChecked = false;
			_ui_cbWed.IsChecked = false;
			_ui_cbThu.IsChecked = false;
			_ui_cbFri.IsChecked = false;
			_ui_cbSat.IsChecked = false;
			_ui_cbSun.IsChecked = false;
			switch (_ui_dpTrailStart.SelectedDate.Value.DayOfWeek)
			{
				case DayOfWeek.Monday:
					_ui_cbMon.IsChecked = true;
					break;
				case DayOfWeek.Tuesday:
					_ui_cbTue.IsChecked = true;
					break;
				case DayOfWeek.Wednesday:
					_ui_cbWed.IsChecked = true;
					break;
				case DayOfWeek.Thursday:
					_ui_cbThu.IsChecked = true;
					break;
				case DayOfWeek.Friday:
					_ui_cbFri.IsChecked = true;
					break;
				case DayOfWeek.Saturday:
					_ui_cbSat.IsChecked = true;
					break;
				case DayOfWeek.Sunday:
					_ui_cbSun.IsChecked = true;
					break;
			} 
			MakeBtnAddTrailReady();
		}
		void _ui_tpTrailStart_ValueChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
		{
			MakeBtnAddTrailReady();
		}
		private void MakeBtnAddTrailReady()
		{
			bool bNameIsReady=false;
			bool bStartDateIsReady=false;
			bool bStartTimeIsReady=false;
			bool bPathIsReady=false;
			bool bLinesAreReady = false;
			if (null != _ui_cbTrailName.SelectedItem)
				bNameIsReady = true;
			if (null != _ui_dpTrailStart.SelectedDate)
				bStartDateIsReady = true;
			if (null != _ui_tpTrailStart.Value)
				bStartTimeIsReady = true;
			if (null != _ui_tbTrailPath.Text && "" != _ui_tbTrailPath.Text)
				bPathIsReady = true;
			if (null != _ui_tbTrailLine1.Text && 0 < _ui_tbTrailLine1.Text.Length && null != _ui_tbTrailLine2.Text && 0 < _ui_tbTrailLine2.Text.Length)
				bLinesAreReady = true;
			if (bNameIsReady && bStartDateIsReady && bStartTimeIsReady && bPathIsReady && bLinesAreReady && _ui_dgTrails.IsEnabled == true)
				_ui_btnAddTrail.IsEnabled = true;
			else
				_ui_btnAddTrail.IsEnabled = false;
		}
		void _ui_dgTrails_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			TemplatesScheduleSL cTSISL = (TemplatesScheduleSL)e.Row.DataContext;
            DateTime dtSunday = DateTime.Today.GetMonday().AddDays(6).Date;
			if (_aTSISL_Changed.Contains(cTSISL))
				e.Row.Background = Coloring.Templates.cRow_ChangedBackgr;
			else if (cTSISL.dtStart > DateTime.Now)
				e.Row.Background = Coloring.Templates.cRow_FutureBackgr;
			else if (cTSISL.dtStop.Date <= dtSunday)
				e.Row.Background = Coloring.Templates.cRow_StopOnThisWeekBackgr;
			else
				e.Row.Background = Coloring.Templates.cRow_NormalBackgr;    //null;
		}
		private void _ui_hbTrailPath_Click(object sender, RoutedEventArgs e)   // надо думать делать ли выбор папки на серваке прям и как...
		{
			FolderChooser cFC = new FolderChooser(_cDBI);  // сейчас на dbi нет инфы про файлы на серваке...
			cFC.Closed += new EventHandler(cFC_Closed);
		}
		void cFC_Closed(object sender, EventArgs e)
		{
			FolderChooser cFC = (FolderChooser)sender;
			cFC.Closed -= new EventHandler(cFC_Closed);
			if (null != cFC.DialogResult && cFC.DialogResult.Value)
				_ui_tbTrailPath.Text = cFC.sSelectedFullPath;
			MakeBtnAddTrailReady();
		}
		private void _ui_tbTrailLines_TextChanged(object sender, TextChangedEventArgs e)
		{
			MakeBtnAddTrailReady();
		}

		#region ContextMenu
		private void ui_dgTrails_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				_cTSICurrent = (TemplatesScheduleSL)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
				if (null != _ui_dgTrails.SelectedItems && 1 >= _ui_dgTrails.SelectedItems.Count)
					_ui_dgTrails.SelectedItem = _cTSICurrent;
			}
			catch
			{
				_cTSICurrent = null;
			}
		}
		void cCM_Opened(object sender, RoutedEventArgs e)
		{
			if (null != _cTSICurrent && 0 < _ui_dgTrails.SelectedItems.Count && _ui_dgTrails.IsEnabled == true)
			{
                _ui_cmiTrails_Delete.Header = g.Common.sDelete + " (" + g.Common.sQty + ":" + _ui_dgTrails.SelectedItems.Count + ")";
				_ui_cmiTrails_Delete.IsEnabled = true;
				_ui_cmiTrails_Delete.Refresh();
			}
		}
		void cCM_Closed(object sender, RoutedEventArgs e)
		{
			_ui_cmiTrails_Delete.Header = g.Common.sDelete;
			_ui_cmiTrails_Delete.IsEnabled = false;
			_ui_cmiTrails_Delete.Refresh();
		}
		void cMI_Delete_Click(object sender, RoutedEventArgs e)
		{
			TemplatesScheduleSL cTSISL;
			List<TemplatesScheduleSL> aTSISL_ToRemove = new List<TemplatesScheduleSL>();
			foreach (object oTSISL in _ui_dgTrails.SelectedItems)
			{
				cTSISL = (TemplatesScheduleSL)oTSISL;
				aTSISL_ToRemove.Add(cTSISL);
				_aTSISL_ToDelete.Add(cTSISL);
			}
			foreach (TemplatesScheduleSL cTSISL_ToRemove in aTSISL_ToRemove)
				_aTSISL_CurrentDG.Remove(cTSISL_ToRemove);

			TrailTableRefrash();
			_ui_btnSaveTrails_Click(null, null);
		}
		#endregion

		#endregion




	}
}
