using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Data;

using System.Windows.Media.Imaging;
using g = globalization;

namespace controls.replica.sl
{
    public partial class Clock : UserControl
    {
		public class Level
		{
			public class Item //: INotifyPropertyChanged
			{
				private Level _cLevel;
				private Color _cColor;
				private Brush _cBackground;
				private TimeSpan _tsStart;
				private TimeSpan _tsDuration;
				private bool _bVisible;
				internal PieDataPoint cPieDataPoint { get; set; }
				internal Item cReplacement;

				public Level cLevel
				{
					set
					{
						if (_cLevel != value)
						{
							if (null != _cLevel)
							{
								_cLevel._aItems.Remove(this);
								_cLevel.Update();
							}
							_cLevel = value;
							if (null != _cLevel)
							{
								_cLevel._aItems.Add(this);
								Update();
							}
						}
					}
					get
					{
						return _cLevel;
					}
				}
				public string sCaption { get; internal set; }
				public Brush cBackground {
					get { return _cBackground; }
					private set
					{
						if (value != _cBackground)
						{
							_cBackground = value;
							Redraw();
						}
					}
				}
				public Color cColor
				{
					get
					{
						return _cColor;
					}
					internal set
					{
						if (Colors.Transparent != value)
						{
							byte nDarkness = 50;
							Color cBorderColor;
							if (0 == value.R && 0 == value.G && 0 == value.B) //UNDONE
								value = Color.FromArgb(value.A, (byte)(value.R + nDarkness), (byte)(value.G + nDarkness), (byte)(value.B + nDarkness));
							cBorderColor = Color.FromArgb(value.A, (byte)((nDarkness > value.R ? 0 : value.R - nDarkness)), (byte)((nDarkness > value.G ? 0 : value.G - nDarkness)), (byte)((nDarkness > value.B ? 0 : value.B - nDarkness)));

							RadialGradientBrush cRadialGradientBrush = null;
							if (null == cBackground || !(cBackground is RadialGradientBrush))
							{
								cRadialGradientBrush = new RadialGradientBrush();
								cRadialGradientBrush.MappingMode = BrushMappingMode.Absolute;
								cRadialGradientBrush.GradientStops = new GradientStopCollection();
							}
							else
							{
								cRadialGradientBrush = (RadialGradientBrush)cBackground;
								cRadialGradientBrush.GradientStops.Clear();
							}
							GradientStop cGS = new GradientStop();
							cGS.Color = value;
							cGS.Offset = 0.9;
							cRadialGradientBrush.GradientStops.Add(cGS);
							cGS = new GradientStop();
							cGS.Color = cBorderColor;
							cGS.Offset = 1;
							cRadialGradientBrush.GradientStops.Add(cGS);
							cBackground = cRadialGradientBrush;
							bVisible = true;
						}
						else
							bVisible = false;
						_cColor = value;
					}
				}
				public short nStart
				{
					get { return (short)_tsStart.TotalSeconds; }
					internal set
					{
						_tsStart = TimeSpan.FromSeconds(value);
					}
				}
				public string sStart
				{
					get { return _tsStart.ToString("mm\\:ss"); }
					internal set
					{
						if (6 > value.Length)
							value = "00:" + value;
						_tsStart = TimeSpan.Parse(value);
					}
				}
				public short nDuration
				{
					get { return (short)_tsDuration.TotalSeconds; }
					internal set
					{
						_tsDuration = TimeSpan.FromSeconds(value);
					}
				}
				public string sDuration
				{
					get { return _tsDuration.ToString("mm\\:ss"); }
					internal set
					{
						if (6 > value.Length)
							value = "00:" + value;
						_tsDuration = TimeSpan.Parse(value);
					}
				}
				public object cTag;
				public bool bVisible
				{
					get { return _bVisible; }
					set
					{
						if (value != _bVisible)
						{
							_bVisible = value;
							if (null != _cLevel && null != _cLevel.cPieSeries)
							{
								if (this == _cLevel.cPieSeries.SelectedItem)
									_cLevel.cPieSeries.SelectedItem = null;
								Redraw();
							}
						}
					}
				}

				private Item(string sCaption)
				{
					this.sCaption = sCaption;
					_cColor = Colors.Transparent;
					_bVisible = false;
					cReplacement = null;
				}
				private Item(string sCaption, string sStart, string sDuration)
					: this(sCaption)
				{
					this.sStart = sStart;
					this.sDuration = sDuration;
				}
				internal Item(string sCaption, string sStart, string sDuration, Color cColor)
					: this(sCaption, sStart, sDuration)
				{
					this.cColor = cColor;
				}
				internal Item(string sCaption, string sStart, string sDuration, ImageBrush cImageBrush)
					: this(sCaption, sStart, sDuration)
				{
					cBackground = cImageBrush;
					bVisible = true;
				}

				internal Item CopyDeep()
				{
					Item cRetVal = new Item(sCaption);
					cRetVal._cLevel = _cLevel;
					cRetVal._cColor = _cColor;
					cRetVal._cBackground = _cBackground;
					cRetVal._bVisible = _bVisible;
					cRetVal._tsStart = _tsStart;
					cRetVal._tsDuration = _tsDuration;
					cRetVal.cTag = cTag;
					cRetVal.cPieDataPoint = cPieDataPoint;
					return cRetVal;
				}
				internal void Redraw()
				{
					if (null == cLevel || null == cLevel._ui_pnlPlotArea)
						return;
					if (null == cPieDataPoint && null == (cPieDataPoint = cLevel._ui_pnlPlotArea.Children.OfType<PieDataPoint>().Where(pdp => pdp.DataContext == this).FirstOrDefault()))
						return;
					if (bVisible)
					{
						if (cBackground is RadialGradientBrush)
						{
							RadialGradientBrush cRadialGradientBrush = null;
							if (!(cPieDataPoint.Background is RadialGradientBrush))
							{
								cRadialGradientBrush = (RadialGradientBrush)cBackground;
								cPieDataPoint.Background = cRadialGradientBrush;
							}
							else
								cRadialGradientBrush = (RadialGradientBrush)cPieDataPoint.Background;
							if (null != cRadialGradientBrush)
							{
								Point stCenter = new Point(
									cLevel.stBounds.Left + ((cLevel.stBounds.Right - cLevel.stBounds.Left) / 2),
									cLevel.stBounds.Top + ((cLevel.stBounds.Bottom - cLevel.stBounds.Top) / 2));
								cRadialGradientBrush.Center = stCenter;
								cRadialGradientBrush.GradientOrigin = stCenter;
								double nRadius = (cLevel.stBounds.Right - cLevel.stBounds.Left) / 2;
								cRadialGradientBrush.RadiusX = nRadius;
								cRadialGradientBrush.RadiusY = nRadius;
							}
						}
						else if (!(cPieDataPoint.Background is ImageBrush))
							cPieDataPoint.Background = cBackground;
					}
					else
						cPieDataPoint.Visibility = Visibility.Collapsed;
				}
				public void Update()
				{
					if(null != _cLevel)
						_cLevel.Update();
				}
			}

			private Clock _cClock;
			private Item[] _aSeconds;
			private List<Item> _aItems;
			private Panel _ui_pnlPlotArea;
			private Rect stBounds
			{
				get
				{
					double nDiameter = Math.Min(_ui_pnlPlotArea.ActualWidth, _ui_pnlPlotArea.ActualHeight) * 0.95;
					Point stLeftTop = new Point((_ui_pnlPlotArea.ActualWidth - nDiameter) / 2, (_ui_pnlPlotArea.ActualHeight - nDiameter) / 2);
					return new Rect(stLeftTop, new Point(stLeftTop.X + nDiameter, stLeftTop.Y + nDiameter));
				}
			}

			public event EventHandler Changed;

			public Clock cClock
			{
				set
				{
					_cClock = value;
				}
				get
				{
					return _cClock;
				}
			}
			public Level cLevelPrevious;
			public Level cLevelNext;
			public PieSeries cPieSeries;
			public IEnumerable<Item> aItems
			{
				get { return _aItems; }
			}
			public double nMargin;
			public double nMarginAbsolute
			{
				get
				{
					double nRetVal = nMargin;
					if (null != cLevelPrevious)
						nRetVal += cLevelPrevious.nMarginAbsolute;
					return nRetVal;
				}
			}
			public double nPadding;
			public double nPaddingAbsolute
			{
				get
				{
					double nRetVal = nPadding;
					if (null != cLevelPrevious)
						nRetVal += cLevelPrevious.nPaddingAbsolute;
					return nRetVal;
				}
			}

			internal Level(Clock cClock, bool bSelectable)
			{
				_cClock = cClock;
				_aItems = new List<Item>();
				_aSeconds = new Item[3600];

				cPieSeries = new PieSeries();
				if (bSelectable)
				{
					cPieSeries.IsSelectionEnabled = true;
					cPieSeries.SelectionChanged += new SelectionChangedEventHandler(_cClock.PieSeries_SelectionChanged);
				}
				if (_cClock.Resources.Contains("DataPointStyle"))
					cPieSeries.DataPointStyle = (Style)_cClock.Resources["DataPointStyle"];
				cPieSeries.DependentValueBinding = new Binding("nDuration");
				cPieSeries.IndependentValueBinding = new Binding("sCaption");
				cPieSeries.Palette = new Collection<ResourceDictionary>();
				cPieSeries.ItemsSource = aItems;
				nMargin = 0.05;
				nPadding = 0;

				ItemAdd(new Level.Item("", "00:00", "01:00:00", Colors.Transparent));
			}
			private void Rebuild()
			{
				_aItems.Clear();
				Item cItemSource = null, cItemTarget = null;
				for (short nSecond = 0; 3600 > nSecond; nSecond++)
				{
					if(cItemSource != _aSeconds[nSecond])
					{
						if(null != cItemTarget)
							cItemTarget.nDuration = (short)(nSecond - cItemTarget.nStart);
						cItemSource = _aSeconds[nSecond];
						cItemTarget = cItemSource.CopyDeep();
						cItemSource.cReplacement = cItemTarget;
						cItemTarget.nStart = nSecond;
						cItemTarget.cPieDataPoint = null;
						_aItems.Add(cItemTarget);
					}
					_aSeconds[nSecond] = cItemTarget;
				}
				if (null != cItemTarget)
					cItemTarget.nDuration = (short)(3600 - cItemTarget.nStart);
				cPieSeries.ItemsSource = null;
				cPieSeries.ItemsSource = aItems;
			}
			public void ItemAdd(Item cItem)
			{
				cItem.cLevel = this;
			}
			private bool PlotAreaAttach()
			{
				_ui_pnlPlotArea = Traverse<FrameworkElement>(cPieSeries, e => VisualTreeChildren(e).OfType<FrameworkElement>(), element => null == element as Chart).OfType<Panel>().Where(e => "PlotArea" == e.Name).FirstOrDefault();
				if (null == _ui_pnlPlotArea)
					return false;
				_ui_pnlPlotArea.SizeChanged += new SizeChangedEventHandler(PlotArea_SizeChanged);
				return true;
			}
			internal void Redraw()
			{
				if (null == cPieSeries)
					return;
				cPieSeries.ApplyTemplate();
				cPieSeries.Margin = new Thickness((_cClock.ActualHeight > _cClock.ActualWidth ? _cClock.ActualWidth : _cClock.ActualHeight) * (nMarginAbsolute + (null == cLevelPrevious ? 0 : cLevelPrevious.nPaddingAbsolute)));
				if (null != _ui_pnlPlotArea || PlotAreaAttach())
				{
					foreach (Item cItem in _aItems)
						cItem.Redraw();
					if (null != cPieSeries && null != cPieSeries.SelectedItem)
						cClock.PieSeries_SelectionChanged(null, new SelectionChangedEventArgs(new object[0], new object[] { cPieSeries.SelectedItem }));
				}
			}
			public void Update()
			{
				int nDuration = 0;
				foreach (Item cItem in _aItems)
				{
					nDuration = cItem.nStart + cItem.nDuration;
					cItem.cLevel = this;
					for (short nSecond = cItem.nStart; nDuration > nSecond; nSecond++)
						_aSeconds[nSecond] = cItem;
				}
				Rebuild();
				Redraw();
				if (null != Changed)
					Changed(this, new EventArgs());
			}
			private void PlotArea_SizeChanged(object sender, SizeChangedEventArgs e)
			{
				Redraw();
			}
		}

		static public readonly DependencyProperty ClockfaceProperty = DependencyProperty.Register("Clockface", typeof(bool), typeof(Clock), new PropertyMetadata(new PropertyChangedCallback(OnClockfaceChanged)));
		static public readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(Level.Item), typeof(Clock), new PropertyMetadata(new PropertyChangedCallback(OnSelectedItemChanged)));
		public bool Clockface
		{
			get
			{
				return (bool)GetValue(ClockfaceProperty);
			}
			set
			{
				SetValue(ClockfaceProperty, value);
			}
		}
		public event EventHandler SelectionChanged;
		public Level.Item SelectedItem
		{
			get
			{
				return (Level.Item)GetValue(SelectedItemProperty);
			}
			private set
			{
				SetValue(SelectedItemProperty, value);
			}
		}
		public Level cLevelCurrent;

		private List<Level> _aLevels;
		private Level _cFaceclockLevel;

		private static IEnumerable<T> Traverse<T>(T initialNode, Func<T, IEnumerable<T>> getChildNodes, Func<T, bool> traversePredicate)
		{
			Stack<T> stack = new Stack<T>();
			stack.Push(initialNode);
			while (stack.Count > 0)
			{
				T node = stack.Pop();
				if (traversePredicate(node))
				{
					yield return node;
					IEnumerable<T> childNodes = getChildNodes(node);
					foreach (T childNode in childNodes)
					{
						stack.Push(childNode);
					}
				}
			}
		}
		private static IEnumerable<DependencyObject> VisualTreeChildren(DependencyObject reference)
		{
			var childrenCount = VisualTreeHelper.GetChildrenCount(reference);
			for (var i = 0; i < childrenCount; i++)
			{
				yield return VisualTreeHelper.GetChild(reference, i);
			}
		}

		public Clock()
        {
            InitializeComponent();

			_aLevels = new List<Level>();
		}

		internal void Redraw()
		{
			_ui_chrtPie.ApplyTemplate();
			for(int nIndx = 0; _aLevels.Count > nIndx; nIndx++)
				_aLevels[nIndx].Redraw();
		}
		public Level LevelAdd()
		{
			Level cRetVal = new Level(this, true);
			_ui_chrtPie.Series.Add(cRetVal.cPieSeries);
			if(0 < _aLevels.Count)
			{
				cRetVal.cLevelPrevious = _aLevels[_aLevels.Count - 1];
				cRetVal.cLevelPrevious.cLevelNext = cRetVal;
			}
			_aLevels.Add(cRetVal);
			SelectedItem = null;
			cLevelCurrent = cRetVal;
			return cRetVal;
		}

		private void _ui_chrtPie_Loaded(object sender, RoutedEventArgs e)
		{
			Redraw();
		}
		private void PieSeries_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ScaleTransform cST = null;
			List<Level.Item> aRemovedItems = new List<Level.Item>();
			foreach (Level.Item cItem in e.RemovedItems)
				aRemovedItems.Add(cItem);
			if (0 < e.AddedItems.Count)
			{
				foreach (PieSeries cPS in _ui_chrtPie.Series)
				{
					if (null != cPS.SelectedItem && 0 > e.AddedItems.IndexOf(cPS.SelectedItem) && 0 > e.RemovedItems.IndexOf(cPS.SelectedItem))
					{
						aRemovedItems.Add((Level.Item)cPS.SelectedItem);
						cPS.SelectedItem = null;
					}
				}
				foreach (Level.Item cItem in e.AddedItems)
				{
					if (cItem.cBackground is RadialGradientBrush)
					{
						RadialGradientBrush cRadialGradientBrush = (RadialGradientBrush)cItem.cBackground;
						cST = new ScaleTransform();
						cST.CenterX = cRadialGradientBrush.Center.X;
						cST.CenterY = cRadialGradientBrush.Center.Y;
						cST.ScaleY = 1.04;
						cST.ScaleX = 1.04;
						cItem.cPieDataPoint.RenderTransform = cST;
					}
				}
				SelectedItem = (Level.Item)e.AddedItems[0];
			}
			if (0 < aRemovedItems.Count)
			{
				foreach (Level.Item cItem in aRemovedItems)
				{
					if (cItem == SelectedItem)
						SelectedItem = null;
					if (cItem.cBackground is RadialGradientBrush)
					{
						RadialGradientBrush cRadialGradientBrush = (RadialGradientBrush)cItem.cBackground;
						cST = new ScaleTransform();
						cST.CenterX = cRadialGradientBrush.Center.X;
						cST.CenterY = cRadialGradientBrush.Center.Y;
						cST.ScaleY = 1;
						cST.ScaleX = 1;
						cItem.cPieDataPoint.RenderTransform = cST;
					}
				}
			}
		}
		//private void PlotArea_SizeChanged(object sender, SizeChangedEventArgs e)
		//{
		//    for (int nIndx = 0; _aLevels.Count > nIndx; nIndx++)
		//    {
		//        if (sender == _aLevels[nIndx]._ui_pnlPlotArea)
		//        {
		//            PieSeriesUpdate(_aLevels[nIndx]);
		//            if (0 < nIndx)
		//            {
		//                double nMin = (_ui_chrtPie.ActualHeight > _ui_chrtPie.ActualWidth ? _ui_chrtPie.ActualWidth : _ui_chrtPie.ActualHeight);
		//                _aLevels[nIndx].cPieSeries.Margin = new Thickness((nMin * 0.12) + (nMin * (nIndx - 1) * 0.05));
		//            }
		//            return;
		//        }
		//    }
		//}
		//private void PieSeriesUpdate(Level cLevel)
		//{
		//    if (null == cLevel.cPieSeries)
		//        return;
		//    cLevel.cPieSeries.ApplyTemplate();
		//    if (null == cLevel._ui_pnlPlotArea)
		//    {
		//        IEnumerable<FrameworkElement> aChildren = Traverse<FrameworkElement>(
		//            cLevel.cPieSeries,
		//            e => VisualTreeChildren(e).OfType<FrameworkElement>(),
		//            element => null == element as Chart);
		//        cLevel._ui_pnlPlotArea = aChildren.OfType<Panel>().Where(e => "PlotArea" == e.Name).FirstOrDefault();
		//    }
		//    if (null != cLevel._ui_pnlPlotArea)
		//    {
		//        cLevel._ui_pnlPlotArea.SizeChanged -= new SizeChangedEventHandler(PlotArea_SizeChanged);
		//        cLevel._ui_pnlPlotArea.SizeChanged += new SizeChangedEventHandler(PlotArea_SizeChanged);
		//        // Calculate the diameter of the pie (0.95 multiplier is from PieSeries implementation)
		//        double nDiameter = Math.Min(cLevel._ui_pnlPlotArea.ActualWidth, cLevel._ui_pnlPlotArea.ActualHeight) * 0.95;
		//        // Calculate the bounding rectangle of the pie
		//        Point stLeftTop = new Point((cLevel._ui_pnlPlotArea.ActualWidth - nDiameter) / 2, (cLevel._ui_pnlPlotArea.ActualHeight - nDiameter) / 2);
		//        Point stRightBottom = new Point(stLeftTop.X + nDiameter, stLeftTop.Y + nDiameter);
		//        Rect stPieBounds = new Rect(stLeftTop, stRightBottom);
		//        // Call the provided updater action for each PieDataPoint
		//        Level.Item cBindedObject = null;
		//        RadialGradientBrush cRadialGradientBrush = null;
		//        Point stCenter;
		//        double nRadius;
		//        foreach (PieDataPoint cPieDataPoint in cLevel._ui_pnlPlotArea.Children.OfType<PieDataPoint>())
		//        {
		//            cBindedObject = (Level.Item)cPieDataPoint.DataContext;
		//            if (null == cBindedObject.cPieDataPoint)
		//                cBindedObject.cPieDataPoint = cPieDataPoint;
		//            if (cBindedObject.bVisible)
		//            {
		//                if (cBindedObject.Background is RadialGradientBrush)
		//                {
		//                    if (!(cPieDataPoint.Background is RadialGradientBrush))
		//                    {
		//                        cRadialGradientBrush = (RadialGradientBrush)cBindedObject.Background;
		//                        cPieDataPoint.Background = cRadialGradientBrush;
		//                    }
		//                    else
		//                        cRadialGradientBrush = (RadialGradientBrush)cPieDataPoint.Background;
		//                    if (null != cRadialGradientBrush)
		//                    {
		//                        stCenter = new Point(
		//                            stPieBounds.Left + ((stPieBounds.Right - stPieBounds.Left) / 2),
		//                            stPieBounds.Top + ((stPieBounds.Bottom - stPieBounds.Top) / 2));
		//                        cRadialGradientBrush.Center = stCenter;
		//                        cRadialGradientBrush.GradientOrigin = stCenter;
		//                        nRadius = (stPieBounds.Right - stPieBounds.Left) / 2;
		//                        cRadialGradientBrush.RadiusX = nRadius;
		//                        cRadialGradientBrush.RadiusY = nRadius;
		//                    }
		//                }
		//                else if (!(cPieDataPoint.Background is ImageBrush))
		//                    cPieDataPoint.Background = cBindedObject.Background;
		//            }
		//            else
		//                cPieDataPoint.Visibility = Visibility.Collapsed;
		//        }
		//        if (null != cLevel.cPieSeries && null != cLevel.cPieSeries.SelectedItem)
		//            PieSeries_SelectionChanged(null, new SelectionChangedEventArgs(new object[0], new object[] { cLevel.cPieSeries.SelectedItem }));
		//    }
		//}

		private void ProcessClockface()
		{
			if (Clockface)
			{
				if (null == _aLevels)
					_aLevels = new List<Level>();
				if (null != _cFaceclockLevel && -1 < _aLevels.IndexOf(_cFaceclockLevel))
				{
					_aLevels.Remove(_cFaceclockLevel);
					_ui_chrtPie.Series.Remove(_cFaceclockLevel.cPieSeries);
				}
				_cFaceclockLevel = new Level(this, false);
				_cFaceclockLevel.nMargin = 0;
				_cFaceclockLevel.nPadding = 0.05;
				_ui_chrtPie.Series.Insert(0, _cFaceclockLevel.cPieSeries);
				if (0 < _aLevels.Count)
				{
					_cFaceclockLevel.cLevelNext = _aLevels[0];
					_aLevels[0].cLevelPrevious = _cFaceclockLevel;
				}
				_aLevels.Insert(0, _cFaceclockLevel);
				ImageBrush cIB = new ImageBrush();
				cIB.Stretch = Stretch.Uniform;
				cIB.ImageSource = new BitmapImage(new Uri("/replica;component/Images/clockface.png", UriKind.Relative));
                _cFaceclockLevel.ItemAdd(new Level.Item(g.Helper.sClockFace, "00:00", "01:00:00", cIB));
			}
			else if (null != _cFaceclockLevel)
			{
				if (null != _cFaceclockLevel.cPieSeries)
				{
					_cFaceclockLevel.cPieSeries.ItemsSource = null;
					_ui_chrtPie.Series.Remove(_cFaceclockLevel.cPieSeries);
				}
				if(_aLevels.Contains(_cFaceclockLevel))
					_aLevels.Remove(_cFaceclockLevel);
				_cFaceclockLevel = null;
			}
			else
				return;
			Redraw();
		}
		private void ProcessSelectedItem()
		{
			if (null != SelectionChanged)
				SelectionChanged(this, new EventArgs());
		}

		private static void OnClockfaceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Clock)d).ProcessClockface();
		}
		private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Clock)d).ProcessSelectedItem();
		}
	}
}
