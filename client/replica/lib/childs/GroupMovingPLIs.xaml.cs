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

using replica.sl;
using helpers.replica.services.dbinteract;
using g = globalization;

namespace controls.childs.replica.sl
{
	public partial class GroupMovingPLIs : ChildWindow
	{
		public class GroupPLI
		{
			public enum StartType
			{
				HARD,
				SOFT,
				NONE
			}
			public string sName
			{
				get { if (null != cPLI) return cPLI.sName; else return ""; }
				set { }
			}
			public string sdtStart
			{
				get
				{
					return _dtStart.ToString("HH:mm:ss");
				}
				set { }
			}
			public string sdtStartPrev
			{
				get
				{
					if (enStartTypePrev != StartType.NONE)
						return _dtStartPrev.ToString("HH:mm:ss");
					else
						return "";
				}
				set { }
			}
			public string sdtStartPlanned
			{
				get { return _dtStartPlanned.Date.ToString("yyyy-MM-dd") + "     " + _dtStartPlanned.ToString("HH:mm:ss"); }
				set { }
			}
			public StartType enStartType { get; set; }
			public StartType enStartTypePrev { get; set; }
			public DateTime _dtStart;
			private DateTime _dtStartPrev;
			private DateTime _dtStartPlanned;
			private PlaylistItem cPLI;
			public GroupPLI(PlaylistItem cPLI)
			{
				this.cPLI = cPLI;
			}
			public static List<GroupPLI> GetGPLIs(List<PlaylistItem> aPLIs)
			{
				List<GroupPLI> aGPLIs = new List<GroupPLI>();
				bool bFirstIteration = true;
				GroupPLI cGPLI;
				foreach (PlaylistItem cPLI in aPLIs)
				{
					cGPLI = new GroupPLI(cPLI);
					if (bFirstIteration)
					{
						cGPLI._dtStart = PlaylistItemSL.HardSoftPlannedGet(cPLI);
						cGPLI.enStartType = DateTime.MaxValue > cPLI.dtStartHard ? StartType.HARD : StartType.SOFT;
						bFirstIteration = false;
					}
					else
						cGPLI.enStartType = StartType.SOFT;

					cGPLI._dtStartPlanned = cPLI.dtStartPlanned;
					cGPLI._dtStartPrev = PlaylistItemSL.HardSoftPlannedGet(cPLI);
					cGPLI.enStartTypePrev = DateTime.MaxValue > cPLI.dtStartHard || DateTime.MaxValue > cPLI.dtStartSoft ? DateTime.MaxValue > cPLI.dtStartHard ? StartType.HARD : StartType.SOFT : StartType.NONE; 
					aGPLIs.Add(cGPLI);
				}
				return aGPLIs;
			}
			public static List<PlaylistItem> GetPLIs(List<GroupPLI> aGPLIs)
			{
				List<PlaylistItem> aPLIs = new List<PlaylistItem>();
				foreach (GroupPLI cGPLI in aGPLIs)
					aPLIs.Add(cGPLI.cPLI);
				return aPLIs;
			}
			public static List<GroupPLI> RecalcFromFirsElement(List<GroupPLI> aGPLIs)
			{
				if (null == aGPLIs)
					return null;
				List<GroupPLI> aRetVal = new List<GroupPLI>();
				DateTime dtStart = aGPLIs[0]._dtStart;
				if (DateTime.MaxValue == dtStart)
					return aGPLIs;
				for (int ni = 1; aGPLIs.Count > ni; ni++)
					aGPLIs[ni]._dtStart = dtStart.AddSeconds(ni);
				return aRetVal = aGPLIs;
			}
			public static void ApplyChanges(List<GroupPLI> aGPLIs)
			{
				if (null == aGPLIs)
					return;
				foreach (GroupPLI cGPLI in aGPLIs)
					if (cGPLI.enStartType == StartType.HARD)
					{
						cGPLI.cPLI.dtStartHard = cGPLI._dtStart;
						cGPLI.cPLI.dtStartSoft = DateTime.MaxValue;
					}
					else
					{
						cGPLI.cPLI.dtStartHard = DateTime.MaxValue;
						cGPLI.cPLI.dtStartSoft = cGPLI._dtStart;
					}
			}
		}
		public List<PlaylistItem> aPLIs;
		private List<GroupPLI> _aGPLIs;
		private bool bSelfChanging;

		public GroupMovingPLIs(List<PlaylistItem> aPLIs)
		{
			InitializeComponent();
            Title = g.Helper.sPlaylistItemsMove.ToLower();

			if (null == aPLIs || 0 == aPLIs.Count)
				this.DialogResult = false;

			_ui_tudTime.ValueChanged += new RoutedPropertyChangedEventHandler<DateTime?>(_ui_TimeDateChanged);

			this.aPLIs = aPLIs;
			_aGPLIs = GroupPLI.GetGPLIs(aPLIs);
			_aGPLIs = GroupPLI.RecalcFromFirsElement(_aGPLIs);

			bSelfChanging = true;
			if (DateTime.MaxValue > aPLIs[0].dtStartHard)
				_ui_rbHard.IsChecked = true;
			else
				_ui_rbSoft.IsChecked = true;
			_ui_dpDate.SelectedDate = _aGPLIs[0]._dtStart.Date;
			_ui_tudTime.Value = _aGPLIs[0]._dtStart;
			bSelfChanging = false;
			_ui_dpDate.DisplayDateStart = DateTime.Now.Date;

			_ui_dgItemsToMove.ItemsSource = _aGPLIs;
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			GroupPLI.ApplyChanges(_aGPLIs);
			this.DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}

		private void _ui_rbChanged(object sender, RoutedEventArgs e)
		{
			if (bSelfChanging)
				return;
			if ("_ui_rbHard" == ((RadioButton)sender).Name)
				_aGPLIs[0].enStartType = GroupPLI.StartType.HARD;
			else
				_aGPLIs[0].enStartType = GroupPLI.StartType.SOFT;
			_ui_dg_Refresh();
		}
		private void _ui_TimeDateChanged(object sender, RoutedEventArgs e)
		{
			if (bSelfChanging)
				return;
			DateTime dtUserStart = _ui_dpDate.SelectedDate ?? DateTime.MaxValue;
			_aGPLIs[0]._dtStart = new DateTime(dtUserStart.Year, dtUserStart.Month, dtUserStart.Day);
			_aGPLIs[0]._dtStart = _aGPLIs[0]._dtStart.AddMilliseconds(((DateTime)_ui_tudTime.Value).TimeOfDay.TotalMilliseconds);
			_aGPLIs = GroupPLI.RecalcFromFirsElement(_aGPLIs);
			_ui_dg_Refresh();
		}
		private void _ui_dg_Refresh()
		{
			_ui_dgItemsToMove.ItemsSource = null;
			_ui_dgItemsToMove.ItemsSource = _aGPLIs;
		}


	}
}

