using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

using controls.childs.sl;
using helpers.replica.services.dbinteract;
using helpers.extensions;
using g = globalization;

namespace replica.sl
{
    public partial class Statistics : Page
    {
        internal class WorkerInfo
        {
            public ulong nID;
            public object cUserState;

            public WorkerInfo(object cUserState)
            {
                nID = 0;
                this.cUserState = cUserState;
            }
        }
        private Progress _dlgProgress;
        private DBInteract _cDBI;
		private IdNamePair[] _aStatuses;

        public Statistics()
        {
            InitializeComponent();
			Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.Name);
            Title = g.Helper.sStatistics;

            _ui_rpArchive.IsOpen = true;
            _ui_rpArchiveFilters.IsOpen = true;
            _ui_rpRAO.Visibility = Preferences.cServer.bStatisticsRAOVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
			_ui_rpRAO.IsOpen = false;
            _ui_rpMessages.Visibility = Preferences.cServer.bStatisticsMessagesVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _ui_rpMessages.IsOpen = false;

			App.Current.Host.Content.Resized += new EventHandler(BrowserWindow_Resized);
			_ui_svMainViewer.MaxHeight = UI_Sizes.GetPossibleHeightOfPlaylistScrollViewer();

			try
			{
				_dlgProgress = new Progress();
				_cDBI = new DBInteract();

				#region pl
				_ui_dtpFilterStartFrom.SelectedDate = DateTime.Now.AddHours(-1);
				_ui_dtpFilterStartUpto.SelectedDate = DateTime.Now;

				_ui_tspFilterStartFrom.Value = _ui_dtpFilterStartFrom.SelectedDate;
				_ui_tspFilterStartUpto.Value = _ui_dtpFilterStartUpto.SelectedDate;

				_ui_tbFilterStartFrom.Text = _ui_dtpFilterStartFrom.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
				_ui_tbFilterStartUpto.Text = _ui_dtpFilterStartUpto.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");

				_cDBI.StatGetCompleted += new EventHandler<StatGetCompletedEventArgs>(_cDBI_StatGetCompleted);
				_cDBI.ClassesGetCompleted += new EventHandler<ClassesGetCompletedEventArgs>(_cDBI_ClassesGetCompleted);
				_cDBI.StatusesGetCompleted += new EventHandler<StatusesGetCompletedEventArgs>(_cDBI_StatusesGetCompleted);
				_cDBI.StatusesClearGetCompleted += _cDBI_StatusesClearGetCompleted;
				#endregion

				#region messages
				_ui_dtpFilterRegisteredFrom.SelectedDate = DateTime.Now.AddHours(-1);
				_ui_dtpFilterRegisteredUpto.SelectedDate = DateTime.Now;

				_ui_tspFilterRegisteredFrom.Value = _ui_dtpFilterRegisteredFrom.SelectedDate;
				_ui_tspFilterRegisteredUpto.Value = _ui_dtpFilterRegisteredUpto.SelectedDate;

				_ui_tbFilterRegisteredFrom.Text = _ui_dtpFilterRegisteredFrom.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
				_ui_tbFilterRegisteredUpto.Text = _ui_dtpFilterRegisteredUpto.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");

				_cDBI.MessagesGetCompleted += new EventHandler<MessagesGetCompletedEventArgs>(_cDBI_MessagesGetCompleted);
				#endregion

				#region rao
				DateTime dtNow = DateTime.Now;
				DateTime dtNow1 = DateTime.Now.AddDays(-1);

				_ui_dtpRAOFilterStartFrom.SelectedDate = new DateTime(dtNow1.Year, dtNow1.Month, dtNow1.Day, 0, 0, 0);
				_ui_dtpRAOFilterStartUpto.SelectedDate = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 23, 59, 59);

                _ui_tspRAOFilterStartFrom.Value = _ui_dtpRAOFilterStartFrom.SelectedDate;
                _ui_tspRAOFilterStartUpto.Value = _ui_dtpRAOFilterStartUpto.SelectedDate;

                _ui_tbRAOFilterStartFrom.Text = _ui_dtpRAOFilterStartFrom.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
                _ui_tbRAOFilterStartUpto.Text = _ui_dtpRAOFilterStartUpto.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");

                _cDBI.ExportCompleted += new EventHandler<ExportCompletedEventArgs>(_cDBI_ExportCompleted);
                _cDBI.WorkerProgressGetCompleted += new EventHandler<WorkerProgressGetCompletedEventArgs>(_cDBI_WorkerProgressGetCompleted);
                _cDBI.ExportResultGetCompleted += new EventHandler<ExportResultGetCompletedEventArgs>(_cDBI_ExportResultGetCompleted);
                #endregion

                //_dlgProgress.Show();
			}
			catch { }
        }

		#region event handlers
		#region UI

		protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }
		void BrowserWindow_Resized(object sender, EventArgs e)
		{
			_ui_svMainViewer.MaxHeight = UI_Sizes.GetPossibleHeightOfPlaylistScrollViewer();
		}

		private void _ui_dgStat_Loaded(object sender, RoutedEventArgs e)
        {
			_dlgProgress.Show();
			KeyValuePair<string, DBFiltersOperators>[] aKVP = new KeyValuePair<string, DBFiltersOperators>[2];
            aKVP[0] = new KeyValuePair<string, DBFiltersOperators>(g.Helper.sEqual, DBFiltersOperators.equal);
            aKVP[1] = new KeyValuePair<string, DBFiltersOperators>(g.Replica.sNoticeStat1 + g.Helper.sEqual, DBFiltersOperators.notequal);

			_ui_ddlFilterStatusOperator.ItemsSource = aKVP;
			_ui_ddlFilterStatusOperator.SelectedIndex = 0;

            aKVP[0] = new KeyValuePair<string, DBFiltersOperators>(g.Common.sContains, DBFiltersOperators.tinparraycontainsid);
            aKVP[1] = new KeyValuePair<string, DBFiltersOperators>(g.Replica.sNoticeStat1 + g.Common.sContains, DBFiltersOperators.tinparraynotcontainsid);

            _ui_ddlFilterClassOperator.ItemsSource = aKVP;
			_ui_ddlFilterClassOperator.SelectedIndex = 0;

			aKVP = new KeyValuePair<string, DBFiltersOperators>[4];
            aKVP[0] = new KeyValuePair<string, DBFiltersOperators>(g.Common.sContains, DBFiltersOperators.contains);
            aKVP[1] = new KeyValuePair<string, DBFiltersOperators>(g.Replica.sNoticeStat1 + g.Common.sContains, DBFiltersOperators.notcontains);
            aKVP[2] = new KeyValuePair<string, DBFiltersOperators>(g.Helper.sEqual, DBFiltersOperators.equal);
            aKVP[3] = new KeyValuePair<string, DBFiltersOperators>(g.Replica.sNoticeStat1 + g.Helper.sEqual, DBFiltersOperators.notequal);

			_ui_ddlFilterNameOperator.ItemsSource = aKVP;
			_ui_ddlFilterNameOperator.SelectedIndex = 0;

			_ui_ddlFilterFileOperator.ItemsSource = aKVP;
			_ui_ddlFilterFileOperator.SelectedIndex = 0;

			aKVP = new KeyValuePair<string, DBFiltersOperators>[5];
            aKVP[0] = new KeyValuePair<string, DBFiltersOperators>(g.Helper.sEqual, DBFiltersOperators.equal);
            aKVP[1] = new KeyValuePair<string, DBFiltersOperators>(g.Replica.sNoticeStat1 + g.Helper.sEqual, DBFiltersOperators.notequal);
            aKVP[2] = new KeyValuePair<string, DBFiltersOperators>(g.Helper.sMore, DBFiltersOperators.more);
            aKVP[3] = new KeyValuePair<string, DBFiltersOperators>(g.Helper.sLess, DBFiltersOperators.less);
            aKVP[4] = new KeyValuePair<string, DBFiltersOperators>(g.Common.sBetween, DBFiltersOperators.contains);

			_ui_ddlFilterFramesQtyOperator.ItemsSource = aKVP;
			_ui_ddlFilterFramesQtyOperator.SelectedIndex = 0;

			_ui_ddlFilterStartFromOperator.ItemsSource = aKVP;
			_ui_ddlFilterStartFromOperator.SelectedIndex = 4;

			_ui_ddlFilterStopFromOperator.ItemsSource = aKVP;
			_ui_ddlFilterStopFromOperator.SelectedIndex = 0;

			_cDBI.ClassesGetAsync();
			_cDBI.StatusesGetAsync();

			//_ui_btnFiltersApply_Click(null, null);
		}
		private void _ui_dtpFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
            try
            {
                DatePicker _ui_dp = (DatePicker)sender;
                if (null == _ui_dp.Tag)
                    return;
                TextBox _ui_tb = (TextBox)_ui_dp.Tag;
                DateTime dt = DateTime.Parse(_ui_tb.Text);
				DateTime dt2;
                _ui_dp.SelectedDate = _ui_dp.SelectedDate.Value.Subtract(_ui_dp.SelectedDate.Value.TimeOfDay).Add(dt.TimeOfDay);
                _ui_tb.Text = _ui_dp.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
				if (_ui_dp == _ui_dtpRAOFilterStartFrom && null != _ui_dp.SelectedDate)
				{
					dt2 = _ui_dp.SelectedDate.Value.AddMonths(1).AddDays(-1);
					_ui_dtpRAOFilterStartUpto.SelectedDate = new DateTime(dt2.Year, dt2.Month, dt2.Day, 23, 59, 59);
					_ui_tbRAOFilterStartUpto.Text = _ui_dtpRAOFilterStartUpto.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
				}
            }
            catch {}
        }
        private void _ui_tspFilter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            TimePicker _ui_tp = (TimePicker)sender;
            if (null == _ui_tp.Tag)
                return;
            TextBox _ui_tb = (TextBox)_ui_tp.Tag;
            DateTime dt = DateTime.Parse(_ui_tb.Text);
            _ui_tp.Value = dt.Subtract(dt.TimeOfDay).Add(_ui_tp.Value.Value.TimeOfDay);
            _ui_tb.Text = _ui_tp.Value.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }
		private void _ui_ddlFilterFramesQtyOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DBFiltersOperators.contains == ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterFramesQtyOperator.SelectedItem).Value)
			{
				_ui_txtFilterFramesQtyUpto.Visibility = Visibility.Visible;
				_ui_tbFilterFramesQtyUpto.Visibility = Visibility.Visible;
			}
			else
			{
				_ui_txtFilterFramesQtyUpto.Visibility = Visibility.Collapsed;
				_ui_tbFilterFramesQtyUpto.Visibility = Visibility.Collapsed;
			}
		}
		private void _ui_ddlFilterStartFromOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DBFiltersOperators.contains == ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterStartFromOperator.SelectedItem).Value)
				_ui_spStartUpto.Visibility = Visibility.Visible;
			else
				_ui_spStartUpto.Visibility = Visibility.Collapsed;
		}
		private void _ui_ddlFilterStopFromOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DBFiltersOperators.contains == ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterStopFromOperator.SelectedItem).Value)
				_ui_spStopUpto.Visibility = Visibility.Visible;
			else
				_ui_spStopUpto.Visibility = Visibility.Collapsed;
		}
		private void _ui_btnFiltersApply_Click(object sender, RoutedEventArgs e)
		{
			_dlgProgress.Show();
			bool bFilters = false;
			DBFilters cFilters = new DBFilters();
			cFilters.cGroup = new DBFiltersGroup();
			DBFilter cCurrentFilter = new DBFilter(), cParentFilter = null;
			cFilters.cGroup.cValue = cCurrentFilter;

			if ((bool)_ui_cbFilterName.IsChecked)
			{
				bFilters = true;
				cCurrentFilter.sName = "`sName`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterNameOperator.SelectedItem).Value;
				cCurrentFilter.cValue = _ui_tbFilterName.Text;
				cParentFilter = cCurrentFilter;
			}
			if ((bool)_ui_cbFilterFile.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}

				cCurrentFilter.sName = "`sFilename`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterFileOperator.SelectedItem).Value;
				cCurrentFilter.cValue = _ui_tbFilterFile.Text;
				cParentFilter = cCurrentFilter;
			}
			if ((bool)_ui_cbFilterStatus.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}

				cCurrentFilter.sName = "`idStatuses`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterStatusOperator.SelectedItem).Value;
				cCurrentFilter.cValue = ((IdNamePair)_ui_ddlFilterStatus.SelectedItem).nID;
				cParentFilter = cCurrentFilter;
			}
			if ((bool)_ui_cbFilterClass.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}

				cCurrentFilter.sName = "`aClasses`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterClassOperator.SelectedItem).Value;
				cCurrentFilter.cValue = ((Class)_ui_ddlFilterClass.SelectedItem).nID;
				cParentFilter = cCurrentFilter;
			}
			if ((bool)_ui_cbFilterFramesQty.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}

				cCurrentFilter.sName = "`nFramesQty`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterFramesQtyOperator.SelectedItem).Value;
				try
				{
					cCurrentFilter.cValue = _ui_tbFilterFramesQtyFrom.Text.ToLong();
					cParentFilter = cCurrentFilter;
					if (DBFiltersOperators.contains == cCurrentFilter.eOP)
					{
						cParentFilter.eOP = DBFiltersOperators.more;

						cCurrentFilter = new DBFilter();
						cCurrentFilter.eBind = Binds.and;
						cParentFilter.cNext = cCurrentFilter;

						cCurrentFilter.sName = cParentFilter.sName;
						cCurrentFilter.eOP = DBFiltersOperators.less;
						cCurrentFilter.cValue = _ui_tbFilterFramesQtyUpto.Text;
						cParentFilter = cCurrentFilter;
					}
				}
				catch
				{
					cParentFilter = null;
				}
			}
			if ((bool)_ui_cbFilterStartFrom.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}

				cCurrentFilter.sName = "`dtStartPlanned`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterStartFromOperator.SelectedItem).Value;
				cCurrentFilter.cValue = _ui_tbFilterStartFrom.Text;
				cParentFilter = cCurrentFilter;
				if (DBFiltersOperators.contains == cCurrentFilter.eOP)
				{
					cParentFilter.eOP = DBFiltersOperators.more;

					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;

					cCurrentFilter.sName = cParentFilter.sName;
					cCurrentFilter.eOP = DBFiltersOperators.less;
					cCurrentFilter.cValue = _ui_tbFilterStartUpto.Text;
					cParentFilter = cCurrentFilter;
				}
			}
			if ((bool)_ui_cbFilterStopFrom.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}

				cCurrentFilter.sName = "`dtStopPlanned`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterStopFromOperator.SelectedItem).Value;
				cCurrentFilter.cValue = _ui_tbFilterStopFrom.Text;
				cParentFilter = cCurrentFilter;
				if (DBFiltersOperators.contains == cCurrentFilter.eOP)
				{
					cParentFilter.eOP = DBFiltersOperators.more;

					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;

					cCurrentFilter.sName = cParentFilter.sName;
					cCurrentFilter.eOP = DBFiltersOperators.less;
					cCurrentFilter.cValue = _ui_tbFilterStopUpto.Text;
					cParentFilter = cCurrentFilter;
				}
			}

			_cDBI.StatGetAsync(bFilters ? cFilters : null);
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
				sExcelStyle += "   <Font ss:FontName=\"Arial\" x:CharSet=\"204\" x:Family=\"Swiss\" ss:Size=\"11\" ss:Bold=\"0\"/>\r\n";
				sExcelStyle += "   <Interior ss:Color=\"#D8D8D8\" ss:Pattern=\"Solid\"/>\r\n";
				sExcelStyle += "   <NumberFormat ss:Format=\"yyyy\\-mm\\-dd\\ hh:mm:ss\"/>\r\n";
				sExcelStyle += "   <Protection/>\r\n";
				sExcelStyle += " </Style>\r\n";

				sExcelStyle += " <Style ss:ID=\"dtb\">\r\n";
				sExcelStyle += "   <Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Bottom\"/>\r\n";
				sExcelStyle += "    <Borders>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "   </Borders>\r\n";
				sExcelStyle += "   <Font ss:FontName=\"Arial\" x:CharSet=\"204\" x:Family=\"Swiss\" ss:Size=\"11\" ss:Bold=\"1\"/>\r\n";
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
				sExcelStyle += "   <Font ss:FontName=\"Arial\" x:CharSet=\"204\" x:Family=\"Swiss\" ss:Size=\"11\" ss:Bold=\"0\"/>\r\n";
				sExcelStyle += "   <Interior ss:Color=\"#DBE5F1\" ss:Pattern=\"Solid\"/>\r\n";
				sExcelStyle += "   <NumberFormat/>\r\n";
				sExcelStyle += "   <Protection/>\r\n";
				sExcelStyle += " </Style>\r\n";

				sExcelStyle += " <Style ss:ID=\"plib\">\r\n";
				sExcelStyle += "   <Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Bottom\"/>\r\n";
				sExcelStyle += "   <Borders>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>\r\n";
				sExcelStyle += "   </Borders>\r\n";
				sExcelStyle += "   <Font ss:FontName=\"Arial\" x:CharSet=\"204\" x:Family=\"Swiss\" ss:Size=\"11\" ss:Bold=\"1\"/>\r\n";
				sExcelStyle += "   <Interior ss:Color=\"#DBE5F1\" ss:Pattern=\"Solid\"/>\r\n";
				sExcelStyle += "   <NumberFormat/>\r\n";
				sExcelStyle += "   <Protection/>\r\n";
				sExcelStyle += " </Style>\r\n";

				sExcelStyle += " </Styles>\r\n";
				#endregion

				String sExcelBody = "<Worksheet ss:Name=\"replica_stat_" + DateTime.Now.ToString("yyyy-MM-dd") + "\">\r\n";
				sExcelBody += "<Table  ss:DefaultColumnWidth=\"170\" ss:DefaultRowHeight=\"15\" >\r\n";

				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"280\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"280\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"85\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"155\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"87\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"125\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"125\"/>";

				sExcelBody += "<Row  ss:Height=\"24\">\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Common.sName + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Common.sFile + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Common.sStatus + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Helper.sClass + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Helper.sTimings + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Helper.sStart + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Helper.sStop + "</Data></Cell>\r\n";
				sExcelBody += "</Row>\r\n";

				foreach (PlaylistItem cPLI in _ui_dgStat.ItemsSource)
				{
					sExcelBody += "<Row>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"plib\"><Data ss:Type=\"String\">" + cPLI.sName.Replace("&", "&amp;") + "</Data></Cell>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cPLI.cFile.sFilename.Replace("&", "&amp;") + "</Data></Cell>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cPLI.cStatus.sName.Replace("&", "&amp;") + "</Data></Cell>\r\n";
                    sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cPLI.aClasses.Select(o => o.sName).ToEnumerationString(true).Replace("&", "&amp;") + "</Data></Cell>\r\n";
                    sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cPLI.nFramesQty.ToFramesString(false,false,true,false,true) + "</Data></Cell>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"dtb\"><Data ss:Type=\"DateTime\">" + cPLI.dtStartPlanned.ToString("yyyy-MM-ddTHH:mm:ss.000") + "</Data></Cell>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"dt\"><Data ss:Type=\"DateTime\">" + (cPLI.dtStartPlanned.AddMilliseconds((cPLI.nFrameStop - cPLI.nFrameStart + 1) *40)).ToString("yyyy-MM-ddTHH:mm:ss.000") + "</Data></Cell>\r\n"; //TODO FPS
					sExcelBody += "</Row>\r\n";
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

		private void _ui_dgStatMessages_Loaded(object sender, RoutedEventArgs e)
		{
			_dlgProgress.Show();
			KeyValuePair<string, DBFiltersOperators>[] aKVP = new KeyValuePair<string, DBFiltersOperators>[4];
            aKVP[0] = new KeyValuePair<string, DBFiltersOperators>(g.Common.sContains, DBFiltersOperators.contains);
            aKVP[1] = new KeyValuePair<string, DBFiltersOperators>(g.Replica.sNoticeStat1 + g.Common.sContains, DBFiltersOperators.notcontains);
            aKVP[2] = new KeyValuePair<string, DBFiltersOperators>(g.Helper.sEqual, DBFiltersOperators.equal);
            aKVP[3] = new KeyValuePair<string, DBFiltersOperators>(g.Replica.sNoticeStat1 + g.Helper.sEqual, DBFiltersOperators.notequal);

			_ui_ddlFilterSourceOperator.ItemsSource = aKVP;
			_ui_ddlFilterSourceOperator.SelectedIndex = 0;

			_ui_ddlFilterTargetOperator.ItemsSource = aKVP;
			_ui_ddlFilterTargetOperator.SelectedIndex = 0;

			_ui_ddlFilterTextOperator.ItemsSource = aKVP;
			_ui_ddlFilterTextOperator.SelectedIndex = 0;

			aKVP = new KeyValuePair<string, DBFiltersOperators>[5];
            aKVP[0] = new KeyValuePair<string, DBFiltersOperators>(g.Helper.sEqual, DBFiltersOperators.equal);
            aKVP[1] = new KeyValuePair<string, DBFiltersOperators>(g.Replica.sNoticeStat1 + g.Helper.sEqual, DBFiltersOperators.notequal);
            aKVP[2] = new KeyValuePair<string, DBFiltersOperators>(g.Helper.sMore, DBFiltersOperators.more);
            aKVP[3] = new KeyValuePair<string, DBFiltersOperators>(g.Helper.sLess, DBFiltersOperators.less);
            aKVP[4] = new KeyValuePair<string, DBFiltersOperators>(g.Common.sBetween, DBFiltersOperators.contains);

			_ui_ddlFilterRegisteredFromOperator.ItemsSource = aKVP;
			_ui_ddlFilterRegisteredFromOperator.SelectedIndex = 4;

			_ui_ddlFilterDisplayedFromOperator.ItemsSource = aKVP;
			_ui_ddlFilterDisplayedFromOperator.SelectedIndex = 0;

			_ui_btnFiltersMessagesApply_Click(null, null);
		}
		private void _ui_ddlFilterRegisteredFromOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DBFiltersOperators.contains == ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterRegisteredFromOperator.SelectedItem).Value)
				_ui_spRegisteredUpto.Visibility = Visibility.Visible;
			else
				_ui_spRegisteredUpto.Visibility = Visibility.Collapsed;
		}
		private void _ui_ddlFilterDisplayedFromOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DBFiltersOperators.contains == ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterDisplayedFromOperator.SelectedItem).Value)
				_ui_spDisplayedUpto.Visibility = Visibility.Visible;
			else
				_ui_spDisplayedUpto.Visibility = Visibility.Collapsed;
		}
		private void _ui_btnFiltersMessagesApply_Click(object sender, RoutedEventArgs e)
		{
			_dlgProgress.Show();
			bool bFilters = false;
			DBFilters cFilters = new DBFilters();
			cFilters.cGroup = new DBFiltersGroup();
			DBFilter cCurrentFilter = new DBFilter(), cParentFilter = null;
			cFilters.cGroup.cValue = cCurrentFilter;

			if ((bool)_ui_cbFilterRegisteredFrom.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}

				cCurrentFilter.sName = "`dtRegister`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterRegisteredFromOperator.SelectedItem).Value;
				cCurrentFilter.cValue = _ui_tbFilterRegisteredFrom.Text;
				cParentFilter = cCurrentFilter;
				if (DBFiltersOperators.contains == cCurrentFilter.eOP)
				{
					cParentFilter.eOP = DBFiltersOperators.more;

					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;

					cCurrentFilter.sName = cParentFilter.sName;
					cCurrentFilter.eOP = DBFiltersOperators.less;
					cCurrentFilter.cValue = _ui_tbFilterRegisteredUpto.Text;
					cParentFilter = cCurrentFilter;
				}
			}
			if ((bool)_ui_cbFilterDisplayedFrom.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}

				cCurrentFilter.sName = "`dtDisplay`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterDisplayedFromOperator.SelectedItem).Value;
				cCurrentFilter.cValue = _ui_tbFilterDisplayedFrom.Text;
				cParentFilter = cCurrentFilter;
				if (DBFiltersOperators.contains == cCurrentFilter.eOP)
				{
					cParentFilter.eOP = DBFiltersOperators.more;

					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;

					cCurrentFilter.sName = cParentFilter.sName;
					cCurrentFilter.eOP = DBFiltersOperators.less;
					cCurrentFilter.cValue = _ui_tbFilterDisplayedUpto.Text;
					cParentFilter = cCurrentFilter;
				}
			}
			if ((bool)_ui_cbFilterSource.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}
				cCurrentFilter.sName = "CAST(`nSource` AS TEXT)";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterSourceOperator.SelectedItem).Value;
				cCurrentFilter.cValue = _ui_tbFilterSource.Text;
				cParentFilter = cCurrentFilter;
			}
			if ((bool)_ui_cbFilterTarget.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}
				cCurrentFilter.sName = "CAST(`nTarget` AS TEXT)";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterTargetOperator.SelectedItem).Value;
				cCurrentFilter.cValue = _ui_tbFilterTarget.Text;
				cParentFilter = cCurrentFilter;
			}
			if ((bool)_ui_cbFilterText.IsChecked)
			{
				bFilters = true;
				if (null != cParentFilter)
				{
					cCurrentFilter = new DBFilter();
					cCurrentFilter.eBind = Binds.and;
					cParentFilter.cNext = cCurrentFilter;
				}
				cCurrentFilter.sName = "`sText`";
				cCurrentFilter.eOP = ((KeyValuePair<string, DBFiltersOperators>)_ui_ddlFilterTextOperator.SelectedItem).Value;
				cCurrentFilter.cValue = _ui_tbFilterText.Text;
				cParentFilter = cCurrentFilter;
			}
			_cDBI.MessagesGetAsync(bFilters ? cFilters : null);
		}
		private void _ui_btnMessagesExport_Click(object sender, RoutedEventArgs e)
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
				sExcelStyle += "   <Font ss:FontName=\"Arial\" x:CharSet=\"204\" x:Family=\"Swiss\" ss:Size=\"12\" ss:Bold=\"0\"/>\r\n";
				sExcelStyle += "   <Interior ss:Color=\"#DBE5F1\" ss:Pattern=\"Solid\"/>\r\n";
				sExcelStyle += "   <NumberFormat/>\r\n";
				sExcelStyle += "   <Protection/>\r\n";
				sExcelStyle += " </Style>\r\n";

				sExcelStyle += " </Styles>\r\n";
				#endregion

				String sExcelBody = "<Worksheet ss:Name=\"replica_sms_stat_" + DateTime.Now.ToString("yyyy-MM-dd") + "\">\r\n";
				sExcelBody += "<Table  ss:DefaultColumnWidth=\"130\" ss:DefaultRowHeight=\"15\" >\r\n";

				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"61\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"130\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"130\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"753\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"130\"/>";
				sExcelBody += "<Column ss:AutoFitWidth=\"0\" ss:Width=\"130\"/>";

				sExcelBody += "<Row  ss:Height=\"24\">\r\n";
				sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + "ID" + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Common.sRegistrationDate + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Common.sDisplayDate + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Common.sText + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Common.sSourceNumber + "</Data></Cell>\r\n";
                sExcelBody += "<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">" + g.Common.sTargetNumber + "</Data></Cell>\r\n";
				sExcelBody += "</Row>\r\n";

				foreach (Message cMess in _ui_dgStatMessages.ItemsSource)
				{
					sExcelBody += "<Row>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cMess.nID.ToString() + "</Data></Cell>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cMess.sRegisterDT.Replace("&", "&amp;") + "</Data></Cell>\r\n"; 
					sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cMess.sDisplayDT.Replace("&", "&amp;") + "</Data></Cell>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cMess.sText.Replace("&", "&amp;") + "</Data></Cell>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cMess.nSourceNumber.ToString() + "</Data></Cell>\r\n";
					sExcelBody += "<Cell ss:StyleID=\"pli\"><Data ss:Type=\"String\">" + cMess.nTargetNumber.ToString() + "</Data></Cell>\r\n";
					sExcelBody += "</Row>\r\n";
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

		#endregion
		#region DBI
        void _cDBI_DBCredentialsSetCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
			_dlgProgress.Close();
		}
        void _cDBI_ClassesGetCompleted(object sender, ClassesGetCompletedEventArgs e)
        {
			try
			{
				_ui_ddlFilterClass.ItemsSource = e.Result;
				_ui_ddlFilterClass.SelectedIndex = 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		void _cDBI_StatusesClearGetCompleted(object sender, StatusesClearGetCompletedEventArgs e)
		{
			if (null == _aStatuses)
				_aStatuses = e.Result;
			if (null != e.UserState)
				switch ((string)e.UserState)
				{
					case "RAO":
						RAOFiltersApply();
						return;
				}
		}
		void _cDBI_StatusesGetCompleted(object sender, StatusesGetCompletedEventArgs e)
		{
			try
			{
				_ui_ddlFilterStatus.ItemsSource = e.Result;
				_ui_ddlFilterStatus.SelectedIndex = 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			_dlgProgress.Close();
		}
		void _cDBI_StatGetCompleted(object sender, StatGetCompletedEventArgs e)
        {
			try
			{
				ListProviders.PLIs.Set(e.Result);
				_ui_dgStat.ItemsSource = ListProviders.PLIs.Queue;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			_dlgProgress.Close();
        }
		void _cDBI_MessagesGetCompleted(object sender, MessagesGetCompletedEventArgs e)
		{
			_ui_dgStatMessages.ItemsSource = null;
			if (null != e.Result)
			{
				List<Message> aMsgs = new List<Message>();
				foreach (Message cMsg in e.Result)
				{
					if (2078 < DateTime.Parse(cMsg.sRegisterDT).Year) //жуть
						cMsg.sRegisterDT = "";
					if (2078 < DateTime.Parse(cMsg.sDisplayDT).Year) //жуть
						cMsg.sDisplayDT = "";
					aMsgs.Add(cMsg);
				}
				_ui_dgStatMessages.ItemsSource = aMsgs;
			}
			_dlgProgress.Close();
		}
        void _cDBI_ExportCompleted(object sender, ExportCompletedEventArgs e)
        {
            WorkerInfo cWI = (WorkerInfo)e.UserState;
            cWI.nID = e.Result;
            _cDBI.WorkerProgressGetAsync(cWI.nID, cWI);
        }
        void _cDBI_ExportResultGetCompleted(object sender, ExportResultGetCompletedEventArgs e)
        {
			try
			{
				WorkerInfo cWI = (WorkerInfo)e.UserState;
				System.IO.StreamWriter cStreamWriter = (System.IO.StreamWriter)cWI.cUserState;
				cStreamWriter.Write(e.Result);
				cStreamWriter.Flush();
				cStreamWriter.Close();
			}
			catch(Exception ex)
			{ 
				(new MsgBox()).ShowError(ex);
			}
            _dlgProgress.Close();
        }
		void _cDBI_WorkerProgressGetCompleted(object sender, WorkerProgressGetCompletedEventArgs e)
		{
			_dlgProgress.Set(e.Result);
			WorkerInfo cWI = (WorkerInfo)e.UserState;
			if (100 > e.Result)
				_cDBI.WorkerProgressGetAsync(cWI.nID, cWI);
			else
				_cDBI.ExportResultGetAsync(cWI.nID, cWI);
		}
		#endregion
		SaveFileDialog _cSFD;
		private void RAOFiltersApply()
		{
			IdNamePair cSatusPlayed = _aStatuses.FirstOrDefault(o => o.sName == "played");
			if (null == cSatusPlayed)
			{
				(new MsgBox()).ShowError(g.Replica.sErrorStat1);
				return;
			}
			_dlgProgress.Show();

			DBFilters cFilters = new DBFilters();
			cFilters.cGroup = new DBFiltersGroup();
			DBFilter cCurrentFilter = new DBFilter(), cParentFilter = null;
			DBFiltersGroup cParentFiltersG = null;
			cFilters.cGroup.cValue = cCurrentFilter;

			cCurrentFilter.sName = "`idStatuses`";
			cCurrentFilter.eOP = DBFiltersOperators.equal;

			cCurrentFilter.cValue = cSatusPlayed.nID; //UNDONE надо заменить константу
			cParentFilter = cCurrentFilter;

			DBFiltersGroup cCurrentFiltersG = new DBFiltersGroup();
			cParentFilter.cNext = cCurrentFiltersG;
			cCurrentFiltersG.eBind = Binds.and;
			cCurrentFilter = new DBFilter();
			cCurrentFiltersG.cValue = cCurrentFilter;
			{
				cCurrentFilter.sName = "`sVideoTypeName`";
				cCurrentFilter.eOP = DBFiltersOperators.equal;
				cCurrentFilter.cValue = "clip";
				cParentFilter = cCurrentFilter;

				cCurrentFilter = new DBFilter();
				cParentFilter.cNext = cCurrentFilter;
				cCurrentFilter.eBind = Binds.or;
				cCurrentFilter.sName = "`sVideoTypeName`";
				cCurrentFilter.eOP = DBFiltersOperators.equal;
				cCurrentFilter.cValue = "program";
			}
			cParentFiltersG = cCurrentFiltersG;

			cCurrentFilter = new DBFilter();
			cParentFiltersG.cNext = cCurrentFilter;
			cCurrentFilter.eBind = Binds.and;
			cCurrentFilter.sName = "`dtStartReal`";
			cCurrentFilter.eOP = DBFiltersOperators.more;
			cCurrentFilter.cValue = _ui_tbRAOFilterStartFrom.Text;
			cParentFilter = cCurrentFilter;


			cCurrentFilter = new DBFilter();
			cParentFilter.cNext = cCurrentFilter;
			cCurrentFilter.eBind = Binds.and;
			cCurrentFilter.sName = cParentFilter.sName;
			cCurrentFilter.eOP = DBFiltersOperators.less;
			cCurrentFilter.cValue = _ui_tbRAOFilterStartUpto.Text;
			cParentFilter = cCurrentFilter;
			try
			{
				_cDBI.ExportAsync("export.RAO", cFilters, new WorkerInfo(new System.IO.StreamWriter(_cSFD.OpenFile())));
			}
			catch (Exception ex)
			{
				(new MsgBox()).ShowError(ex);
				_dlgProgress.Close();
			}
		}
		private void _ui_btnRAOFiltersApply_Click(object sender, RoutedEventArgs e)
		{
			_cSFD = new SaveFileDialog();
			_cSFD.Filter = "xls files | *.xls";
			_cSFD.DefaultExt = ".xls";
			if ((bool)_cSFD.ShowDialog())
			{
				if (null == _aStatuses)
					_cDBI.StatusesClearGetAsync("RAO");
				else
					RAOFiltersApply();
			}
		}
		#endregion


    }
}
