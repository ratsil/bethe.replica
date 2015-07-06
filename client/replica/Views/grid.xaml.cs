using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using controls.childs.sl;
using helpers.extensions;
using helpers.replica.services.dbinteract;
using controls.extensions.sl;
using g = globalization;

namespace replica.sl
{
	public partial class Grid : Page
	{
		public class GridItem
		{ 
			public enum StartType
			{
				soft,
				hard,
				none
			}
			public StartType eStartType { get; set; }
			public string sName { get; set; }
			public string sInfo { get; set; }
			public DateTime dtStart { get; set; }
			public int nApproximateDuration { get; set; }  // in minutes
			public Color stColor { get; set; }
			public PlaylistItem[] aPlaylistItems { get; set; }
			public GridItem()
			{ }
		}
		public class GIProgram : GridItem
		{
			public Asset cEpisode { get; set; }
			public Asset[] aParts { get; set; }
		}
		public class GIClips : GridItem
		{
			public Clip[] aClips { get; set; }
		}
		public class GIAdvertisement : GridItem
		{
			public Asset[] aAssets { get; set; }
		}

		public class GridDaySL
		{
			public static TextBox mousePOS;

			public static Style uiTextBoxStyle;
			public byte nUnitVertical { get; set; }  // minutes per 1 minimal textbox object
			public float nVerticalScale { get; set; }  // pixels per minute
			public float nRulerStep { get; set; } 
			public delegate void OnSelected();
			public OnSelected JustSelected;
			public delegate void OnSaveNeeded(GridItem cGridItem, TextBox cTextBox);
			public OnSaveNeeded SaveGridItem;
			private DateTime dtDate;
			private List<GridItem> _aGridItems;
			//private List<TextBox> _aTextBoxes;
			private Dictionary<TextBox, GridItem> _ahResources;
			private StackPanel _ui_spDay;
			private Color stColorUnused;
			private ContextMenu _ui_cmTextBox;
			private MenuItem _ui_cmiTextBox_Delete;
			private MenuItem _ui_cmiTextBox_Add;
			private TextBox _ui_tbSelected;
			private TextBox _ui_tbChanged;

			public GridDaySL(GridItem[] aGridItems, DateTime dtDate, float nVerticalScale, byte nUnitVertical, float nRulerStep)
			{
				this._aGridItems = null == aGridItems ? null : aGridItems.Where(o => null != o && o.dtStart >= dtDate && o.dtStart < dtDate.AddDays(1)).ToList();
				this.nVerticalScale = nVerticalScale;
				this.dtDate = dtDate.Date;
				this.nUnitVertical = nUnitVertical;
				this.nRulerStep = nRulerStep;
				stColorUnused = Color.FromArgb(255, 150, 150, 150);
				_ui_spDay = new StackPanel() { Orientation = Orientation.Vertical };
				_ahResources = new Dictionary<TextBox, GridItem>();
				_ui_cmTextBox = new ContextMenu();
				_ui_cmTextBox.Opened += new RoutedEventHandler(cCM_Opened);
				_ui_cmTextBox.Closed += new RoutedEventHandler(cCM_Closed);
				_ui_cmiTextBox_Delete = new MenuItem() { Header = g.Common.sDelete, IsEnabled = false };
				_ui_cmiTextBox_Delete.Click += new RoutedEventHandler(cCM_Delete);
				_ui_cmTextBox.Items.Add(_ui_cmiTextBox_Delete);
				_ui_cmiTextBox_Add = new MenuItem() { Header = g.Common.sAdd, IsEnabled = false };
				_ui_cmiTextBox_Add.Click += new RoutedEventHandler(cCM_Add);
				_ui_cmTextBox.Items.Add(_ui_cmiTextBox_Add);
				_ui_spDay.SetValue(ContextMenuService.ContextMenuProperty, _ui_cmTextBox);
				_ui_spDay.MouseMove += new MouseEventHandler(_ui_spDay_MouseMove);
			}

			void _ui_spDay_MouseMove(object sender, MouseEventArgs e)
			{
				double x = e.GetPosition(null).X;
				double y = e.GetPosition(null).Y;
				mousePOS.Text = String.Format("X: {0} Y: {1}", x, y);
			}
			//public GridItem[] GridItemsGet();
			//public GridItem[] GridItemsChangedGet();
			public StackPanel DayGet()
			{
				List<TextBox> aTB = new List<TextBox>();
				DateTime dtPrev = dtDate;
				int nRoundedDur = 0;
				foreach (GridItem cGI in _aGridItems)
				{
					if (cGI.dtStart > dtPrev.AddMinutes((float)nUnitVertical / 2)) 
						aTB.Add(TextBoxCreate(RoundMinutes(cGI.dtStart.Subtract(dtPrev).TotalMinutes), stColorUnused, null));
					aTB.Add(TextBoxCreate(nRoundedDur = RoundMinutes(cGI.nApproximateDuration), cGI.stColor, cGI));
					dtPrev = cGI.dtStart.AddMinutes(nRoundedDur);
				}
				if (dtDate.AddDays(1) > dtPrev.AddMinutes((float)nUnitVertical / 2))
					aTB.Add(TextBoxCreate(RoundMinutes(dtDate.AddDays(1).Subtract(dtPrev).TotalMinutes), stColorUnused, null));

				_ui_spDay.Children.AddRange(aTB);
				return _ui_spDay;
			}
			public StackPanel LeftRulerGet()
			{
				StackPanel ui_spLeft = new StackPanel();
				List<TextBox> aTB = new List<TextBox>();
				DateTime dtTime = dtDate.Date;
				for (int ni = 0; ni < 24 / nRulerStep; ni++)
				{
					aTB.Add(TextBoxLeftCreate(dtDate.AddHours(ni * nRulerStep).ToString("HH:mm"), ni % 2 == 0));
				}
				ui_spLeft.Children.AddRange(aTB);
				return ui_spLeft;
			}
			private TextBox TextBoxCreate(int nMinutes, Color cColor, GridItem cGI)
			{
				double nHeight;
				TextBox cRetVal = new TextBox()
				{
					Height = nHeight = nMinutes * nVerticalScale,
					Background = new SolidColorBrush(cColor),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					MinWidth = 20,
					Text = cGI == null ? "" : cGI.sName,
					Tag = cGI,
					AcceptsReturn = true,
					TextWrapping = TextWrapping.Wrap,
					VerticalContentAlignment = VerticalAlignment.Center,
					TextAlignment = TextAlignment.Center,
					BorderThickness = new Thickness(0, 0, 0, 1),
					BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50)),
					IsReadOnly = cGI == null ? true : false,
					Padding = nHeight < 20 ? new Thickness(0, -2, 0, 0) : new Thickness(0, 0, 0, 0),
					FontFamily = new FontFamily("Trebuchet MS"),
					FontSize = nHeight < 20 ? 9 : 14,
					Style = uiTextBoxStyle,
				};
				cRetVal.MouseRightButtonDown += new MouseButtonEventHandler(uiTextBox_MouseRightButtonDown);
				cRetVal.TextChanged += new TextChangedEventHandler(uiTextBox_TextChanged);
				cRetVal.GotFocus += new RoutedEventHandler(uiTextBox_GotFocus);
				cRetVal.LostFocus += new RoutedEventHandler(uiTextBox_LostFocus);
				_ahResources.Add(cRetVal, cGI);
				return cRetVal;
			}


			public TextBox TextBoxHeaderCreate()
			{
				// System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");
				TextBox ui_tbHeader = new TextBox()
				{
					Background = new SolidColorBrush(Color.FromArgb(255,212,212,212)),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					MinWidth = 20,
					AcceptsReturn = true,
					Text = dtDate.ToString("dddd") + "\r" + dtDate.ToString("dd.MM.yyyy"),
					Tag = null,
					TextWrapping = TextWrapping.Wrap,
					VerticalContentAlignment = VerticalAlignment.Center,
					TextAlignment = TextAlignment.Center,
					BorderThickness = new Thickness(1, 0, 0, 0),
					BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50)),
					IsReadOnly = true,
					Padding = new Thickness(0, 0, 0, 0),
					FontFamily = new FontFamily("Trebuchet MS"),
					FontSize = 14,
					Style = uiTextBoxStyle,
				};

				return ui_tbHeader;
			}
			public TextBox TextBoxLeftCreate(string sText, bool bHighLighted)
			{
				// System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");
				TextBox ui_tbHeader = new TextBox()
				{
					Height = 60 * nRulerStep * nVerticalScale,
					Background = new SolidColorBrush(Color.FromArgb(255, 212, 212, 212)),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					MinWidth = 20,
					AcceptsReturn = true,
					Text = sText,
					Tag = null,
					TextWrapping = TextWrapping.Wrap,
					VerticalContentAlignment = VerticalAlignment.Top,
					TextAlignment = TextAlignment.Center,
					BorderThickness = new Thickness(0, 0, 0, 1),
					BorderBrush = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150)),
					IsReadOnly = true,
					Padding = new Thickness(0, 0, 0, 0),
					FontFamily = new FontFamily("Trebuchet MS"),
					FontSize = bHighLighted ? 11 : 9,
					FontStyle= FontStyles.Normal,
					FontWeight = bHighLighted ? FontWeights.Bold : FontWeights.Normal,
					Style = uiTextBoxStyle,
				};

				return ui_tbHeader;
			}
			private int RoundMinutes(double nMinutes)
			{
				return ((int)(nMinutes / nUnitVertical + 0.5)) * nUnitVertical;
			}
			void uiTextBox_TextChanged(object sender, TextChangedEventArgs e)
			{
				_ui_tbChanged = (TextBox)sender;
			}
			void uiTextBox_LostFocus(object sender, RoutedEventArgs e)
			{
				if (null != _ui_tbChanged && _ui_tbChanged == _ui_tbSelected)
				{
					if (null != _ahResources[_ui_tbChanged])
					{
						_ahResources[_ui_tbChanged].sName = _ui_tbChanged.Text;
						if (null != SaveGridItem)
							SaveGridItem(_ahResources[_ui_tbChanged], _ui_tbChanged);
					}
					_ui_tbSelected = null;
				}
			}
			void uiTextBox_GotFocus(object sender, RoutedEventArgs e)
			{
				_ui_tbSelected = (TextBox)sender;
				if (JustSelected != null)
					JustSelected();
			}
			void uiTextBox_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
			{
				((TextBox)sender).Focus();
			}
			void cCM_Opened(object sender, RoutedEventArgs e)
			{
				if (null != _ui_tbSelected && _ui_tbSelected.IsEnabled == true)
				{
					if (null != _ahResources[_ui_tbSelected])
					{
						_ui_cmiTextBox_Delete.Header = g.Common.sDelete + "  '" + _ui_tbSelected.Text.Substring(0, 20 > _ui_tbSelected.Text.Length ? _ui_tbSelected.Text.Length : 20) + "..'";
						_ui_cmiTextBox_Delete.IsEnabled = true;
						_ui_cmiTextBox_Delete.Refresh();
					}
					else
					{
						_ui_cmiTextBox_Add.Header = g.Common.sAdd;
						_ui_cmiTextBox_Add.IsEnabled = true;
						_ui_cmiTextBox_Add.Refresh();
					}
				}
			}
			void cCM_Closed(object sender, RoutedEventArgs e)
			{
                _ui_cmiTextBox_Delete.Header = g.Common.sDelete;
				_ui_cmiTextBox_Delete.IsEnabled = false;
				_ui_cmiTextBox_Delete.Refresh();

				_ui_cmiTextBox_Add.IsEnabled = false;
				_ui_cmiTextBox_Add.Refresh();
			}
			void cCM_Delete(object sender, RoutedEventArgs e)
			{ 
			
			}
			void cCM_Add(object sender, RoutedEventArgs e)
			{

			}
		}
		
		public class Record
		{
			public string sTime { get; set; }
			public string sMonday { get; set; }
			public string sTuesday { get; set; }
			public string sWednesday { get; set; }
			public string sThursday { get; set; }
			public string sFriday { get; set; }
			public string sSaturday { get; set; }
			public string sSunday { get; set; }
		}
		public class DictionaryRecord
		{
			public string sRu { get; set; }
			public string sEn { get; set; }
		}
		private DBInteract _cDBI;
		private Progress _dlgProgress;
		private MsgBox _dlgMsgBox;
		private GridItem[] _aGI;

		public Grid()
		{
			InitializeComponent();

            Title = g.Helper.sGrid;
            _dlgProgress = new Progress();
			_dlgMsgBox = new MsgBox();
			_cDBI = new DBInteract();
			_cDBI.GridGetCompleted += _cDBI_GridGetCompleted;
			_cDBI.GridSaveCompleted += _cDBI_GridSaveCompleted;

			GridDaySL.uiTextBoxStyle = _ui_HeaderMon.Style;      // (Style)Resources["BestStileForTB"];
			_ui_HeaderMon.Visibility = System.Windows.Visibility.Collapsed;

			//_dlgProgress.Show();
			//_cDBI.GridGetAsync();
			_cDBI_GridGetCompleted(null, null);
		}

		#region dbi
		void _cDBI_GridGetCompleted(object sender, GridGetCompletedEventArgs e)
		{
			try
			{
				_aGI = new GridItem[5]; //EMERGENCY:l как я понимаю, 5 следующих строчек - это некая заглушка? пока не буду прикручивать локализацию
				_aGI[0] = new GIProgram() { dtStart = new DateTime(2014, 8, 18, 12, 0, 0), eStartType = GridItem.StartType.hard, nApproximateDuration = 30, sName = "10-ка худших", sInfo = "все худшие клипы за год", stColor = Color.FromArgb(255, 255, 170, 170) };
				_aGI[1] = new GIProgram() { dtStart = new DateTime(2014, 8, 18, 12, 30, 0), eStartType = GridItem.StartType.hard, nApproximateDuration = 30, sName = "10-ка худших2", sInfo = "все худшие клипы за год", stColor = Color.FromArgb(255, 170, 255, 170) };
				_aGI[2] = new GIProgram() { dtStart = new DateTime(2014, 8, 18, 14, 0, 0), eStartType = GridItem.StartType.hard, nApproximateDuration = 60, sName = "10-ка худших3", sInfo = "все худшие клипы за год", stColor = Color.FromArgb(255, 255, 100, 255) };
				_aGI[3] = new GIProgram() { dtStart = new DateTime(2014, 8, 18, 16, 0, 0), eStartType = GridItem.StartType.hard, nApproximateDuration = 10, sName = "10-ка худших4", sInfo = "все худшие клипы за год", stColor = Color.FromArgb(255, 255, 170, 170) };
				_aGI[4] = new GIProgram() { dtStart = new DateTime(2014, 8, 19, 12, 30, 0), eStartType = GridItem.StartType.hard, nApproximateDuration = 30, sName = "10-ка худших6", sInfo = "все худшие клипы за год", stColor = Color.FromArgb(255, 255, 170, 170) };

				_ui_gWeek.MouseWheel += new MouseWheelEventHandler(_ui_scrlWeek_MouseWheel);
				
				GridShow();
			}
			catch (Exception ex)
			{
                _dlgMsgBox.ShowError(ex);
			}
			_dlgProgress.Close();
		}

		void _ui_scrlWeek_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			_ui_tbMouseWEEL.Text = ((int)_ui_scrlWeek.VerticalOffset).ToString();
		}
		void _cDBI_GridSaveCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
			}
			catch (Exception ex)
			{
                _dlgMsgBox.ShowError(ex);
			}
			_dlgProgress.Close();
		}
		#endregion







		#region ui
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{ }

		private void GridShow()
		{
			DateTime dtStartDay = new DateTime(2014, 8, 18);
			float nScale = 1;
			byte nUnit = 5;
			float nRulerStep = 0.5F;
			//ui_gTrails.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
			//ui_gTrails.Children.Add(_ui_dgTrails);
			//System.Windows.Controls.Grid.SetRow(_ui_dgTrails, 1);

			if (_ui_gWeek.Children.Count > 0)
				_ui_gWeek.Children.Clear();

			if (_ui_gHeader.Children.Count > 0)
				_ui_gHeader.Children.Clear();

			GridDaySL cGDSL = new GridDaySL(_aGI, new DateTime(2014, 8, 18), nScale, nUnit, nRulerStep);
			GridDaySL.mousePOS = _ui_tbMousePOS;
			StackPanel ui_spTMP;
			TextBox ui_tbHeader;
			for (int ni = 0; ni < 7; ni++)
			{
				_ui_gWeek.Children.Add(ui_spTMP = (cGDSL = new GridDaySL(_aGI, new DateTime(2014, 8, 18 + ni), nScale, nUnit, nRulerStep)).DayGet());
				System.Windows.Controls.Grid.SetColumn(ui_spTMP, ni + 1);

				_ui_gHeader.Children.Add(ui_tbHeader = cGDSL.TextBoxHeaderCreate());
				System.Windows.Controls.Grid.SetColumn(ui_tbHeader, ni + 1);
			}

			ui_spTMP = cGDSL.LeftRulerGet();
			_ui_gWeek.Children.Add(ui_spTMP);
			System.Windows.Controls.Grid.SetColumn(ui_spTMP, 0);

		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_dlgProgress.Show();
			}
			catch (Exception ex)
			{
				_dlgProgress.Close();
                _dlgMsgBox.ShowError(ex);
			}
		}

        #endregion

	}
}
