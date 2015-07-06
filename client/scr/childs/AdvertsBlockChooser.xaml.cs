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

using helpers.replica.services.dbinteract;
using DBI = helpers.replica.services.dbinteract;
using IP = scr.services.ingenie.player;
using scr.services.ingenie.player;
using controls.sl;
using controls.childs.sl;
using helpers.extensions;

using g = globalization;

namespace scr.childs
{
	public partial class AdvertsBlockChooser : ChildWindow
	{
		public enum BlockType
		{
			Adverts,
			Clips,
			Unknown
		}
		private DBInteract _cDBI;
		private PlayerSoapClient _cPlayer;
		private List<LivePLItem> _aAdvPLSingle = new List<LivePLItem>();
		private List<LivePLItem> _aAdvPLWithBlocks = new List<LivePLItem>();
		public List<LivePLItem> _aAdvSelectedSingle = new List<LivePLItem>();
		public List<LivePLItem> _aAdvSelectedWithBlocks = new List<LivePLItem>();
		private List<LivePLItem> _aAdvPreviousBlocks = new List<LivePLItem>();
		private BlockType _enType = BlockType.Unknown;
		private List<long> _aAdvertsStoppedPLIsIDs;
		public List<IP.IdNamePair> aStorages { get; set; }
		public string sLog;
		public BlockType enType
		{
			get { return _enType; }
			set
			{
				_enType = value;
			}
		}
		public AdvertsBlockChooser()
		{
			_aAdvertsStoppedPLIsIDs = new List<long>();
			InitializeComponent();
            Title = g.SCR.sNoticeAdvertsBlockChooser0;
		}
		public AdvertsBlockChooser(DBInteract cDBI, PlayerSoapClient cIG)
			: this()
		{
			_cDBI = cDBI;
			_cPlayer = cIG;
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			sLog = "";
			sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser1.Fmt(Environment.NewLine);
			_cPlayer.ClipsSCRGetCompleted += new EventHandler<ClipsSCRGetCompletedEventArgs>(_cPlayer_ClipsSCRGetCompleted);
			_cPlayer.AdvertsSCRGetCompleted += new EventHandler<AdvertsSCRGetCompletedEventArgs>(_cPlayer_AdvertsSCRGetCompleted);
			_cPlayer.AdvertsStoppedGetCompleted += new EventHandler<AdvertsStoppedGetCompletedEventArgs>(_cPlayer_AdvertsStoppedGetCompleted);
			_ui_dgAdvPL.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgAdvPL_LoadingRow);
			_cDBI.ClipsGetCompleted += new EventHandler<ClipsGetCompletedEventArgs>(_cDBI_ClipsGetCompleted);
			_cDBI.PlaylistItemsAdvertsGetCompleted += new EventHandler<PlaylistItemsAdvertsGetCompletedEventArgs>(_cDBI_PlaylistItemsAdvertsGetCompleted);
			_cDBI.LogoBindingGetCompleted += new EventHandler<LogoBindingGetCompletedEventArgs>(_cDBI_LogoBindingGetCompleted);


			if (BlockType.Adverts == enType)
			{
				DateTime dtBegin = DateTime.Now.Subtract(System.TimeSpan.FromMinutes(40));
				DateTime dtEnd = dtBegin.AddHours(3);
				_ui_dpDate.SelectedDate = _ui_dpDate.DisplayDateStart = dtBegin.Date;
				_ui_dpDate.DisplayDateEnd = dtBegin.AddDays(7).Date;
				_ui_tpStartTime.Value = dtBegin;
				_ui_nudHoursQty.Value = 3;
				sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser2.Fmt(dtBegin, dtEnd);
				_cDBI.PlaylistItemsAdvertsGetAsync(dtBegin, dtEnd, dtBegin);
			}
			else if (BlockType.Clips == enType)
			{
				_cDBI.ClipsGetAsync();
			}
		}

		public void Show(string sMsg)
		{
			this.Show();
		}
		private void ProgressOn()
		{
			_ui_gAdverts.Visibility = Visibility.Collapsed;
			_ui_gClips.Visibility = Visibility.Collapsed;
		}
		private void ProgressOff()
		{
			switch (_enType)
			{
				case BlockType.Adverts:
					_ui_gAdverts.Visibility = Visibility.Visible;
					_ui_gClips.Visibility = Visibility.Collapsed;
					break;
				case BlockType.Clips:
					_ui_gClips.Visibility = Visibility.Visible;
					_ui_gAdverts.Visibility = Visibility.Collapsed;
					break;
				default:
					break;
			}
		}


		#region . UI .
		protected override void OnClosed(EventArgs e)
		{
			_cDBI.ClipsGetCompleted -= _cDBI_ClipsGetCompleted;
			_cDBI.PlaylistItemsAdvertsGetCompleted -= _cDBI_PlaylistItemsAdvertsGetCompleted;
			_cPlayer.ClipsSCRGetCompleted -= _cPlayer_ClipsSCRGetCompleted;
			_cPlayer.AdvertsStoppedGetCompleted -= _cPlayer_AdvertsStoppedGetCompleted;
			_cPlayer.AdvertsSCRGetCompleted -= _cPlayer_AdvertsSCRGetCompleted;
			_ui_dgAdvPL.LoadingRow -= _ui_dgAdvPL_LoadingRow;
			_cDBI.LogoBindingGetCompleted -= _cDBI_LogoBindingGetCompleted;
			ProgressOn();
			base.OnClosed(e);
		}
		private void _ui_btnOk_Click(object sender, RoutedEventArgs e)
		{
			if (null == _ui_lblNameOfSelected.Content)
				this.DialogResult = false;
			else
			{
				this.DialogResult = true;
			}
		}
		private void _ui_btnCancel_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}
		private void _ui_hlbtnDetales_Click(object sender, RoutedEventArgs e)
		{
			if (g.Common.sShowDetails.ToLower() == _ui_hlbtnDetales.Content.ToString())
			{
				_ui_dgAdvPL.ItemsSource = _aAdvPLSingle;
                _ui_hlbtnDetales.Content = g.Common.sHideDetails.ToLower();
			}
			else
			{
				_ui_dgAdvPL.ItemsSource = _aAdvPLWithBlocks;
                _ui_hlbtnDetales.Content = g.Common.sShowDetails.ToLower();
			}
		}
		private void _ui_btnShowBlocks_Click(object sender, RoutedEventArgs e)
		{
			if (null != _ui_tpStartTime.Value)
			{
				DateTime dtNow = _ui_dpDate.SelectedDate.Value;
				DateTime dtTmp = (DateTime)_ui_tpStartTime.Value;
				DateTime dtBegin = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtTmp.Hour, dtTmp.Minute, dtTmp.Second);
				double nH = _ui_nudHoursQty.Value;
				DateTime dtEnd = dtBegin.AddHours(nH);
				_cDBI.PlaylistItemsAdvertsGetAsync(dtBegin, dtEnd, dtBegin);
			}
		}
		void _ui_dgAdvPL_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			LivePLItem cLPLI = (LivePLItem)e.Row.DataContext;
			if (!cLPLI.bFileExist)
				e.Row.Background = Coloring.Notifications.cTextBoxError;
			else if (PLIType.AdvBlockItem == cLPLI.eType)
			{
				if (_aAdvertsStoppedPLIsIDs.Contains(cLPLI._cAdvertSCR.nPlaylistID))
					e.Row.Background = Coloring.SCR.cPLRow_AdvBlockItemStoppedBackgr;
				else
					e.Row.Background = Coloring.SCR.cPLRow_AdvBlockItemBackgr;
			}
			else if (PLIType.AdvBlock == cLPLI.eType || PLIType.JustString == cLPLI.eType)
			{
				long nPLIID;
				if (PLIType.AdvBlock == cLPLI.eType)
					nPLIID = cLPLI.aItemsInThisBlock[1]._cAdvertSCR.nPlaylistID;
				else
					nPLIID = cLPLI.cBlock.aItemsInThisBlock[1]._cAdvertSCR.nPlaylistID;

				if (_aAdvertsStoppedPLIsIDs.Contains(nPLIID))
					e.Row.Background = Coloring.SCR.cPLRow_AdvBlockStoppedBackgr;
				else
					e.Row.Background = Coloring.SCR.cPLRow_AdvBlockBackgr;
			}
		}
		private void _ui_dgAdvPL_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_ui_lblStatus.Content = "";
			ClearSelected();
			if (null == _ui_dgAdvPL.SelectedItem)
				return;
			LivePLItem cItem = (LivePLItem)_ui_dgAdvPL.SelectedItem;
			if (PLIType.AdvBlock == cItem.eType)
				AddSelected(cItem);
			else
				AddSelected(cItem.cBlock);
		}
		void AddSelected(LivePLItem cBlock)
		{
			_ui_lblNameOfSelected.Content = cBlock.sName;
			_aAdvSelectedWithBlocks.Add(cBlock);
			_aAdvSelectedSingle.AddRange(cBlock.aItemsInThisBlock);
		}
		void ClearSelected()
		{
			_ui_lblNameOfSelected.Content = g.Common.sNoSelection.ToUpper();
			_aAdvSelectedWithBlocks.Clear();
			_aAdvSelectedSingle.Clear();
		}
		private void _ui_dgClipsSCR_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ClearSelected();
			if (null == _ui_dgClipsSCR.SelectedItem)
				return;
			_ui_lblNameOfSelected.Content = ((IP.Clip)_ui_dgClipsSCR.SelectedItem).sName;
			LivePLItem cTMP = new LivePLItem((IP.Clip)_ui_dgClipsSCR.SelectedItem);
			_aAdvSelectedWithBlocks.Add(cTMP);
			_aAdvSelectedSingle.Add(cTMP);
		}
		#endregion


		#region . DB interact .
		void _cPlayer_ClipsSCRGetCompleted(object sender, ClipsSCRGetCompletedEventArgs e)
		{
			_ui_dgClipsSCR.ItemsSource = e.Result;
			ProgressOff();
		}
		void _cDBI_PlaylistItemsAdvertsGetCompleted(object sender, PlaylistItemsAdvertsGetCompletedEventArgs e)
		{
			if (null != e.Result)
			{
				_cDBI.LogoBindingGetAsync(e.Result, new object[2] { e.Result, e.UserState });
				sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser3.Fmt(e.Result.Length);
			}
			else
			{
				sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser3.Fmt("NULL !!!!!!!!\n");
				ProgressOff();
			}
		}
		void _cDBI_LogoBindingGetCompleted(object sender, LogoBindingGetCompletedEventArgs e)
		{
			sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser4.Fmt(e.Result.Length);
			DBI.PlaylistItem[] aPLIs = null;
			List<IP.Advertisement> aRetVal = new List<IP.Advertisement>();
			int nI;
			List<bool> aLogoBinds;
			try
			{
				if (null != e.UserState && e.UserState is object[] && null != ((object[])e.UserState)[0] && ((object[])e.UserState)[0] is DBI.PlaylistItem[])
				{
					aPLIs = (DBI.PlaylistItem[])(((object[])e.UserState)[0]);
					nI = 0;
					aLogoBinds = new List<bool>();
					if (null != e.Result && aPLIs.Length == e.Result.Count(o => true))
						aLogoBinds.AddRange(e.Result);
					else
						(new MsgBox()).ShowError(g.SCR.sErrorAdvertsBlockChooser1);
					foreach (DBI.PlaylistItem cPLI in aPLIs)
					{
						IP.Advertisement cAdvertisement = new IP.Advertisement()
						{
							nFramesQty = cPLI.nFramesQty,
							nAssetID = cPLI.cAsset.nID,
							sFilename = cPLI.cFile.sFilename,
							nPlaylistID = cPLI.nID,
							sName = cPLI.sName,
							sStorageName = cPLI.cFile.cStorage.sName,
							sStoragePath = 0 < aStorages.Count(o => o.nID == cPLI.cFile.cStorage.nID) ? aStorages.FirstOrDefault(o => o.nID == cPLI.cFile.cStorage.nID).sName : cPLI.cFile.cStorage.sPath,
							sDuration = cPLI.nFramesQty.ToFramesString(),
							dtStartSoft = cPLI.dtStartHard == DateTime.MaxValue ? cPLI.dtStartSoft : cPLI.dtStartHard,
							dtStartReal = cPLI.dtStartReal,
							dtStartPlanned = cPLI.dtStartPlanned,
							sStartPlanned = cPLI.dtStartPlanned.ToString("HH:mm:ss"),
							sClassName = cPLI.cClass.sName,
							bLogoBinding = aLogoBinds[nI]
						};
						aRetVal.Add(cAdvertisement);
						nI++;
					}
					_cPlayer.AdvertsSCRGetAsync(aRetVal.ToArray(), ((object[])e.UserState)[1]);
					return;
				}
				else
                    (new MsgBox()).ShowError(g.SCR.sErrorAdvertsBlockChooser2);
			}
			catch(Exception ex)
			{
				(new MsgBox()).ShowError(g.SCR.sErrorAdvertsBlockChooser3.Fmt(Environment.NewLine) + ex.Message);
			}
			ProgressOff();
			sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser5.Fmt(Environment.NewLine); 
		}
		void _cPlayer_AdvertsSCRGetCompleted(object sender, AdvertsSCRGetCompletedEventArgs e)
		{
			if (null == e.Result || null != e.Error)
			{
				(new MsgBox()).ShowError(g.SCR.sErrorAdvertsBlockChooser4.Fmt(Environment.NewLine) + e.Error.Message);
				ProgressOff();
				return;
			}
            sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser6.Fmt(e.Result.Length);
			List<LivePLItem> aLPL_block = null;
			List<LivePLItem> aLPL_single = null;
			IP.Advertisement cPLI_prevous = null;
			LivePLItem cLPLItem;
			_aAdvPreviousBlocks = _aAdvPLWithBlocks;
			_aAdvPLWithBlocks = new List<LivePLItem>();
			_aAdvPLSingle = new List<LivePLItem>();

			foreach (IP.Advertisement cPLI in e.Result)
			{
				sLog += " | a " + cPLI.nPlaylistID;
				if (null != cPLI_prevous) // значит уже не первый раз
				{
					sLog += " b " + cPLI.dtStartSoft + " " + cPLI_prevous.dtStartSoft + " " + cPLI.dtStartSoft.Subtract(cPLI_prevous.dtStartSoft).TotalSeconds;
					if (1 == (int)cPLI.dtStartSoft.Subtract(cPLI_prevous.dtStartSoft).TotalSeconds)
					{
						sLog += " c";
						if (null != aLPL_block) // первый потенциально неполный блок уже пропущен
						{
							sLog += " i";
							aLPL_block.Add(new LivePLItem(cPLI_prevous));
						}
					}
					else
					{
						sLog += " d";
						if (null == aLPL_block) //следующий блок будет первым
						{
							sLog += " e";
							aLPL_block = new List<LivePLItem>();
						}
						else if (0 < aLPL_block.Count) // текущий блок закончен
						{
							sLog += " f";
							aLPL_block.Add(new LivePLItem(cPLI_prevous));
							cLPLItem = new LivePLItem(aLPL_block);
							_aAdvPLWithBlocks.Add(cLPLItem);
							_aAdvPLSingle.AddRange(cLPLItem.aItemsInThisBlock);
							aLPL_block = new List<LivePLItem>();
						}
					}
				}
				else   // первый раз
				{
					sLog += " g";
					if (DateTime.MaxValue == cPLI.dtStartReal)
					{
						sLog += " h";
						aLPL_block = new List<LivePLItem>();
					}
				}
				cPLI_prevous = cPLI;
			}
			sLog += g.SCR.sNoticeAdvertsBlockChooser7.Fmt(_aAdvPLWithBlocks.Count, _aAdvPLSingle.Count, _aAdvPreviousBlocks.Count);
			aLPL_block = new List<LivePLItem>();
			aLPL_single = new List<LivePLItem>();
			DateTime dtMin = DateTime.MinValue;
			if (null != e.UserState && e.UserState is DateTime)
				dtMin = (DateTime)e.UserState;

			foreach (LivePLItem cLPLI in _aAdvPreviousBlocks)
			{
				if (1 < cLPLI.aItemsInThisBlock.Count && cLPLI.aItemsInThisBlock[1]._cAdvertSCR.dtStartPlanned > dtMin && (0 == _aAdvPLWithBlocks.Count || cLPLI.aItemsInThisBlock[1]._cAdvertSCR.dtStartSoft.AddMinutes(5) < _aAdvPLWithBlocks[0].aItemsInThisBlock[1]._cAdvertSCR.dtStartSoft))
				{
					aLPL_block.Add(cLPLI);
					aLPL_single.AddRange(cLPLI.aItemsInThisBlock);
				}
			}
			aLPL_block.AddRange(_aAdvPLWithBlocks);
			aLPL_single.AddRange(_aAdvPLSingle);
			_aAdvPLWithBlocks = aLPL_block;
			_aAdvPLSingle = aLPL_single;
			_cPlayer.AdvertsStoppedGetAsync();
			sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser8.Fmt(e.Result.Length, _aAdvPLWithBlocks.Count, _aAdvPLSingle.Count);
		}
		void _cPlayer_AdvertsStoppedGetCompleted(object sender, AdvertsStoppedGetCompletedEventArgs e)
		{
			sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser9.Fmt(_aAdvPLWithBlocks.Count);
			ProgressOff();
			if (null != e.Result)
				_aAdvertsStoppedPLIsIDs = e.Result.ToList();
			_ui_dgAdvPL.ItemsSource = _aAdvPLWithBlocks;
		}
		void _cDBI_ClipsGetCompleted(object sender, ClipsGetCompletedEventArgs e)
		{
			List<IP.Clip> aSCRClips = new List<IP.Clip>();
			if (null != e.Result)
				foreach (DBI.Clip cClip in e.Result)
					aSCRClips.Add(new IP.Clip() 
					{ 
						nFramesQty = cClip.nFramesQty, 
						nID = cClip.nID, 
						sFilename = cClip.cFile.sFilename, 
						sName = cClip.sName, 
						sStorageName = cClip.cFile.cStorage.sName,
						sArtist = cClip.stCues.sArtist, 
						sSong = cClip.stCues.sSong, 
						sRotation = cClip.cRotation.sName, 
						sClassName = cClip.cClass.sName 
					});
			_cPlayer.ClipsSCRGetAsync(aSCRClips.ToArray());
		}
		#endregion

	}
}

