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
using controls.replica.sl;
using IC=presentation.sl.services.cues;
using presentation.sl.services.cues;
using helpers.sl;
using helpers.extensions;

namespace presentation.sl
{
	public partial class MainPage : UserControl
	{
		public class Item : TemplateButton.Item
		{
			public static implicit operator Item(IC.Item cItem)
			{
				if (null == cItem)
					return null;
				Item cRetVal = Get(cItem.nID);
				cRetVal.eStatus = cItem.eStatus.To<TemplateButton.Status>();
				cRetVal.sPreset = cItem.sPreset;
				cRetVal.sInfo = cItem.sInfo;
				return cRetVal;
			}
			public static implicit operator IC.Item(Item cItem)
			{
				if (null == cItem)
					return null;
				return new IC.Template() { nID = cItem.nID, eStatus = cItem.eStatus.To<IC.Status>(), sPreset = cItem.sPreset, sInfo = cItem.sInfo };
			}

			static protected Item Get(ulong nID)
			{
				Item cRetVal;
				if (null == (cRetVal = (Item)aItems.FirstOrDefault(o => o.nID == nID)))
				{
					cRetVal = new Item();
					cRetVal.nID = nID;
					aItems.Add(cRetVal);
				}
				return cRetVal;
			}
		}

		private Progress _dlgProgress;
		private MsgBox _dlgMsgBox;
		private CuesSoapClient _cCues;
		private System.Windows.Threading.DispatcherTimer _cTimerForStatusGet;
		private Dictionary<Item, TemplateButton> _ahItemsTemplateButtons;

		public string[] aFontFamilies { get; set; }

		public MainPage()
		{
			InitializeComponent();

			_dlgMsgBox = new MsgBox();
			_dlgProgress = new Progress();
			_dlgProgress.Show();
			_ahItemsTemplateButtons = new Dictionary<Item, TemplateButton>();

			_ui_ddlRollFonts.ItemsSource = _ui_ddlCrawlFonts.ItemsSource = aFontFamilies = App.cPreferences.aFontFamilies;

			_ui_tbText.FontFamily = new FontFamily(aFontFamilies.FirstOrDefault(o => "Arial" == (string)o));

			_ui_ddlRollFonts.SelectedItem = _ui_ddlCrawlFonts.SelectedItem = _ui_tbText.FontFamily.Source;
			_ui_nudRollSize.Value = _ui_nudCrawlSize.Value = _ui_tbText.FontSize = 20;
			_ui_cpRollColor.SelectedColor = _ui_cpCrawlColor.SelectedColor = ((SolidColorBrush)_ui_tbText.Foreground).Color;

			_cTimerForStatusGet = new System.Windows.Threading.DispatcherTimer();
			_cTimerForStatusGet.Tick += delegate(object s, EventArgs args)
				{
					_cTimerForStatusGet.Stop();
					_cCues.ItemsUpdateAsync(_ahItemsTemplateButtons.Keys.Select(o => (IC.Item)o).ToArray());
				};
			_cTimerForStatusGet.Interval = new TimeSpan(0, 0, 0, 0, 100);  // период проверки статуса темплейта

			_cCues = new CuesSoapClient();
			_cCues.DeviceDownStreamKeyerDisableCompleted += _cCues_DeviceDownStreamKeyerDisableCompleted;
			_cCues.DeviceDownStreamKeyerEnableCompleted += _cCues_DeviceDownStreamKeyerEnableCompleted;
			_cCues.InitCompleted += _cCues_InitCompleted;
			_cCues.ItemCreateCompleted += _cCues_ItemCreateCompleted;
			_cCues.ItemPrepareCompleted += _cCues_ItemPrepareCompleted;
			_cCues.ItemStartCompleted += _cCues_ItemStartCompleted;
			_cCues.ItemsUpdateCompleted += _cCues_ItemsUpdateCompleted;

			_cCues.InitAsync((ulong)Application.Current.GetHashCode());
		}

		#region cues
		void _cCues_DeviceDownStreamKeyerEnableCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				if (null != e.Error)
					throw e.Error;
			}
			catch (Exception ex)
			{
				_dlgMsgBox.Show(ex.Message, "ошибка веб-сервиса (cues:item:create)", MsgBox.MsgBoxButton.OK);
			}
		}
		void _cCues_DeviceDownStreamKeyerDisableCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				if (null != e.Error)
					throw e.Error;
			}
			catch (Exception ex)
			{
				_dlgMsgBox.Show(ex.Message, "ошибка веб-сервиса (cues:item:create)", MsgBox.MsgBoxButton.OK);
			}
		}
		void _cCues_InitCompleted(object sender, InitCompletedEventArgs e)
		{
			try
			{
				if (null != e.Error)
					throw e.Error;
				_cTimerForStatusGet.Start();
			}
			catch (Exception ex)
			{
				_dlgMsgBox.Show(ex.Message, "ошибка веб-сервиса (cues:item:create)", MsgBox.MsgBoxButton.OK);
			}
			_dlgProgress.Close();
		}
		void _cCues_ItemCreateCompleted(object sender, ItemCreateCompletedEventArgs e)
		{
			try
			{
				if (null != e.Error)
					throw e.Error;
				Item cItem= (Item)e.Result;
				TemplateButton ui_tplb = (TemplateButton)e.UserState;
				ui_tplb.cItem = cItem;
				lock (_ahItemsTemplateButtons)
					_ahItemsTemplateButtons.Add(cItem, ui_tplb);
				_cCues.ItemPrepareAsync(e.Result, ui_tplb);
			}
			catch (Exception ex)
			{
				_dlgMsgBox.Show(ex.Message, "ошибка веб-сервиса (cues:item:create)", MsgBox.MsgBoxButton.OK);
			}
		}
		void _cCues_ItemPrepareCompleted(object sender, ItemPrepareCompletedEventArgs e)
		{
			try
			{
				if (!e.Result)
					throw new Exception("не удалось подготовить указанный элемент");
				ItemPrepareCompleted((TemplateButton)e.UserState);
			}
			catch (Exception ex)
			{
				_dlgMsgBox.Show(ex.Message, "ошибка веб-сервиса (cues:item:prepare)", MsgBox.MsgBoxButton.OK);
			}
		}
		void _cCues_ItemStartCompleted(object sender, ItemStartCompletedEventArgs e)
		{
			try
			{
				if (!e.Cancelled)
				{
					if (null != e.Error)
						_dlgMsgBox.ShowError(e.Error);
					if(_ui_tplbVideo == e.UserState)
						_cCues.DeviceDownStreamKeyerDisableAsync();
				}
				else
					_dlgMsgBox.ShowError("текущий запрос был отменен");
			}
			catch { }
			_dlgProgress.Close();
		}
		void _cCues_ItemDeleteCompleted(object sender, ItemDeleteCompletedEventArgs e)
		{
			try
			{
				if (null != e.UserState && e.UserState is TemplateButton)
					((TemplateButton)e.UserState).eStatus = (e.Result ? TemplateButton.Status.Idle : TemplateButton.Status.Error);
			}
			catch (Exception ex)
			{
				_dlgMsgBox.Show(ex.Message, "ошибка веб-сервиса (cues:item:delete)", MsgBox.MsgBoxButton.OK);
			}
		}

		void _cCues_ItemsUpdateCompleted(object sender, ItemsUpdateCompletedEventArgs e)
		{
			try
			{
				ItemsUpdate(e.Result.Translate());
				_cTimerForStatusGet.Start();
			}
			catch (Exception ex)
			{
				_dlgMsgBox.Show(ex.Message, "ошибка веб-сервиса (cues:items:update)", MsgBox.MsgBoxButton.OK);
			}
		}
		void _cCues_ItemsRunningGetCompleted(object sender, ItemsRunningGetCompletedEventArgs e)
		{
			try
			{
				if (null != e.Result)
				{
					lock (_ahItemsTemplateButtons)
					{
						foreach (Item cItem in e.Result)
						{
							if (_ahItemsTemplateButtons.ContainsKey(cItem))
							{
								cItem.eStatusPrevious = TemplateButton.Status.Idle;
								lock (_ahItemsTemplateButtons[cItem])
									_ahItemsTemplateButtons[cItem].cItem = cItem;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				_dlgMsgBox.Show(ex.Message, "ошибка веб-сервиса (cues:items:running)", MsgBox.MsgBoxButton.OK);
			}
		}
		#endregion
		
		#region ui
		private void TemplatePrepare(object sender, EventArgs e)
		{
			TemplateButton ui_tplb = (TemplateButton)sender;
			UserReplacement[] aUserReplacements = null;
			if (ui_tplb == _ui_tplbRoll || ui_tplb == _ui_tplbCrawl)
			{
				aUserReplacements = new UserReplacement[] {
						new UserReplacement() { sKey = "{%RUNTIME::USER::TEXT%}" }
						, new UserReplacement() { sKey = "{%RUNTIME::USER::SPEED%}" }
						, new UserReplacement() { sKey = "{%RUNTIME::USER::FONTNAME%}" }
						, new UserReplacement() { sKey = "{%RUNTIME::USER::FONTSIZE%}" }
						, new UserReplacement() { sKey = "{%RUNTIME::USER::FONTCOLOR:RED%}" }
						, new UserReplacement() { sKey = "{%RUNTIME::USER::FONTCOLOR:GREEN%}" }
						, new UserReplacement() { sKey = "{%RUNTIME::USER::FONTCOLOR:BLUE%}" }
					};
				Color cColor;
				string sText = _ui_tbText.Text;
				if (ui_tplb == _ui_tplbCrawl)
				{
					sText = _ui_tbText.Text.Replace("\r", "\n").Replace("\n\n", " ");
					cColor = _ui_cpCrawlColor.SelectedColor;
					aUserReplacements[1].sValue = ((int)_ui_nudCrawlSpeed.Value).ToString();
					aUserReplacements[2].sValue = (string)_ui_ddlRollFonts.SelectedItem;
					aUserReplacements[3].sValue = ((int)_ui_nudCrawlSize.Value).ToString();
				}
				else
				{
					cColor = _ui_cpRollColor.SelectedColor;
					aUserReplacements[1].sValue = ((int)_ui_nudRollSpeed.Value).ToString();
					aUserReplacements[2].sValue = (string)_ui_ddlRollFonts.SelectedItem;
					aUserReplacements[3].sValue = ((int)_ui_nudRollSize.Value).ToString();
				}
				aUserReplacements[0].sValue = sText;
				aUserReplacements[4].sValue = cColor.R.ToString();
				aUserReplacements[5].sValue = cColor.G.ToString();
				aUserReplacements[6].sValue = cColor.B.ToString();
			}
			_cCues.ItemCreateAsync("presentation", ui_tplb.sFile, aUserReplacements, sender);
		}
		private void TemplateStart(object sender, EventArgs e)
		{
			_cCues.ItemStartAsync((Item)((TemplateButton)sender).cItem, sender);
		}
		private void TemplateStop(object sender, EventArgs e)
		{
			if (_ui_tplbVideo == sender)
				_cCues.DeviceDownStreamKeyerEnableAsync(255, true);
			_cCues.ItemStopAsync((Item)((TemplateButton)sender).cItem);
		}

		private void _ui_btnAnimationAdd_Click(object sender, RoutedEventArgs e)
		{

		}

		private void _ui_sldrSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{

		}
		private void _ui_sldrSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{

		}
		#endregion

		private void ItemsUpdate(Item[] aItems)
		{
			TemplateButton.Status eStatus;
			foreach (Item cItem in aItems)
			{
				eStatus = cItem.eStatus.To<TemplateButton.Status>();
				if (cItem.eStatusPrevious != eStatus && TemplateButton.Status.Error != cItem.eStatusPrevious)
				{
					cItem.eStatusPrevious = eStatus;

					switch (eStatus)
					{
						case TemplateButton.Status.Started:
						case TemplateButton.Status.Prepared:
							lock(_ahItemsTemplateButtons)
								_ahItemsTemplateButtons[cItem].eStatus = eStatus;
							break;
						case TemplateButton.Status.Stopped:
						case TemplateButton.Status.Error:
							_ahItemsTemplateButtons[cItem].eStatus = eStatus;
							_ahItemsTemplateButtons[cItem].cItem = null;
							//if (_ui_tplbVideo == _ahItemsTemplateButtons[cItem])
							//    _cCues.DeviceDownStreamKeyerEnableAsync(255, true);
							break;
						case TemplateButton.Status.Idle:
						default:
							break;
					}
				}
			}
		}
		private void ItemPrepareCompleted(TemplateButton ui_tplb)
		{
			if (null == ui_tplb)   // если вызывали не с кнопки.
				return;
			if (null == ui_tplb.cItem)
				ui_tplb.eStatus = TemplateButton.Status.Error;
			else
				ui_tplb.cItem.eStatusPrevious = TemplateButton.Status.Unknown;
		}
	}
	static public class x
	{
		static public MainPage.Item[] Translate(this IC.Item[] aItems)
		{
			return aItems.Select(o => (MainPage.Item)o).ToArray();
		}
	}
}
