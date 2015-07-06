using System.IO.IsolatedStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Controls;
using swc = System.Windows.Controls;
using sio = System.IO;

using controls.sl;
using controls.childs.sl;
using helpers.replica.services.dbinteract;
using helpers.extensions;

using g = globalization;
using System.Runtime.InteropServices.Automation;

namespace replica.sl
{
    public partial class ingest: Page
    {
        public class Tab
        {
			public Preferences.Ingest.Tab cData;
			public bool bInited;
			public Ingest cIngestInfo;
        }
		class FileTask
		{
			public class UI
			{
                private object _cPauseSymbol, _cPlaySymbol, _cStopSymbol;
				TextBlock _ui_txtSource;
				ProgressBar _ui_pb;
				Button _ui_btnPlayPause;
				Button _ui_btnStop;
				TextBlock _ui_txtTarget;


				FileTask _cFileTask;

				public UI(FileTask cFT)
				{
					_cFileTask = cFT;
					_cFileTask.cUI = this;

					Image cImg = new Image();
					System.Windows.Media.Imaging.BitmapImage cBI = new System.Windows.Media.Imaging.BitmapImage();
					cBI.UriSource = new Uri("/replica;component/Images/task_play.png", UriKind.Relative);
					cImg.Source = cBI;
					_cPlaySymbol = cImg;

					cImg = new Image();
					cBI = new System.Windows.Media.Imaging.BitmapImage();
					cBI.UriSource = new Uri("/replica;component/Images/task_pause.png", UriKind.Relative);
					cImg.Source = cBI;
					_cPauseSymbol = cImg;

					cImg = new Image();
					cBI = new System.Windows.Media.Imaging.BitmapImage();
					cBI.UriSource = new Uri("/replica;component/Images/task_stop.png", UriKind.Relative);
					cImg.Source = cBI;
					_cStopSymbol = cImg;

					_ui_txtSource = new TextBlock();
					_ui_txtSource.Margin = new Thickness(5);

					_ui_pb = new ProgressBar();
					_ui_pb.Minimum = 0;
					_ui_pb.Value = 0;
					_ui_pb.Maximum = 1000;
					_ui_pb.Margin = _ui_txtSource.Margin;

					_ui_btnPlayPause = new Button();
					_ui_btnPlayPause.Content = _cPauseSymbol;
					_ui_btnPlayPause.Margin = new Thickness(0, 5, 0, 5);
					_ui_btnPlayPause.Click += new RoutedEventHandler(_ui_btnPlayPause_Click);
					
					_ui_btnStop = new Button();
					_ui_btnStop.Content = _cStopSymbol;
					_ui_btnStop.Margin = _ui_btnPlayPause.Margin;
					_ui_btnStop.Click += new RoutedEventHandler(_ui_btnStop_Click);

					_ui_txtTarget = new TextBlock();
					_ui_txtTarget.Margin = _ui_txtSource.Margin;
				}

				public void Create(swc.Grid ui_g, string sSource, string sTarget)
				{
					_ui_txtSource.Text = sSource;
					_ui_txtTarget.Text = sTarget;

					RowDefinition ui_grd = new RowDefinition();
					ui_grd.Height = GridLength.Auto;
					int nRowsQty = ui_g.RowDefinitions.Count;
					ui_g.RowDefinitions.Add(ui_grd);

					_ui_txtSource.SetValue(swc.Grid.RowProperty, nRowsQty);
					_ui_txtSource.SetValue(swc.Grid.ColumnProperty, 0);
					ui_g.Children.Add(_ui_txtSource);

					_ui_pb.SetValue(swc.Grid.RowProperty, nRowsQty);
					_ui_pb.SetValue(swc.Grid.ColumnProperty, 1);
					ui_g.Children.Add(_ui_pb);

					_ui_btnPlayPause.SetValue(swc.Grid.RowProperty, nRowsQty);
					_ui_btnPlayPause.SetValue(swc.Grid.ColumnProperty, 2);
					ui_g.Children.Add(_ui_btnPlayPause);

					_ui_btnStop.SetValue(swc.Grid.RowProperty, nRowsQty);
					_ui_btnStop.SetValue(swc.Grid.ColumnProperty, 3);
					ui_g.Children.Add(_ui_btnStop);

					_ui_txtTarget.SetValue(swc.Grid.RowProperty, nRowsQty);
					_ui_txtTarget.SetValue(swc.Grid.ColumnProperty, 4);
					ui_g.Children.Add(_ui_txtTarget);
				}

				void _ui_btnPlayPause_Click(object sender, RoutedEventArgs e)
				{
					if (_cFileTask.bPaused)
					{
						_cFileTask.bPaused = false;
						_ui_btnPlayPause.Content = _cPauseSymbol;
					}
					else
					{
						_cFileTask.bPaused = true;
						_ui_btnPlayPause.Content = _cPlaySymbol;
					}
				}
				void _ui_btnStop_Click(object sender, RoutedEventArgs e)
				{
					if (_cFileTask.bCompleted)
					{
						_ui_txtSource.Visibility = Visibility.Collapsed;
						_ui_pb.Visibility = Visibility.Collapsed;
						_ui_btnPlayPause.Visibility = Visibility.Collapsed;
						_ui_btnStop.Visibility = Visibility.Collapsed;
						_ui_txtTarget.Visibility = Visibility.Collapsed;
					}
					else
						_cFileTask.bCanceled = true;
				}
				public void Update(float nProgress)
				{
					try
					{
						_ui_txtSource.Dispatcher.BeginInvoke(() =>
							{
								_ui_pb.Value = (int)(nProgress * _ui_pb.Maximum);
							}
						);
					}
					catch { }
				}
				public void Complete()
				{
					try
					{
						_ui_txtSource.Dispatcher.BeginInvoke(() =>
							{
								_cFileTask.Close();
								if (_cFileTask.bCanceled)
								{
									_ui_pb.Value = _ui_pb.Minimum;
                                    _ui_txtTarget.Text += " (" + g.Common.sCanceled.ToLower() + ")";
									_ui_txtTarget.Foreground = new SolidColorBrush(Colors.Red);
									_ui_btnPlayPause.Visibility = Visibility.Collapsed;
								}
								else
								{
									_ui_pb.Value = _ui_pb.Maximum;
                                    _ui_txtTarget.Text += " (" + g.Helper.sEnded.ToLower() + ")";
									_ui_txtTarget.Foreground = new SolidColorBrush(Colors.Green);
									_ui_btnPlayPause.Visibility = Visibility.Collapsed;
								}
							}
						);
					}
					catch { }
				}
                public void ShowError()
                {
                    _ui_txtTarget.Foreground = new SolidColorBrush(Colors.Red);
                    _ui_txtTarget.Text += " --> " + g.Common.sError.ToUpper() + "!";
                }
			}

			public class Source
			{
				public long nBytesTotal;
				public sio.Stream cStream;

				public Source(sio.FileInfo cFI)
				{
					this.cStream = cFI.OpenRead();
					nBytesTotal = cFI.Length;
				}
				public void Close()
				{
					try
					{
						if (null != cStream)
							cStream.Close();
					}
					catch { }
				}
			}
			public class Target
			{
				public long nBytesTotal;
				public sio.Stream cStream;

				public Target(string sFilename, sio.Stream cStream)
				{
					this.cStream = cStream;
					nBytesTotal = 0;
				}
				public void Close()
				{
					try
					{
						if (null != cStream)
							cStream.Close();
					}
					catch { }
				}
			}

            public event EventHandler Completed;
            
            private Source _cSource;
			private Target _cTarget;

            public UI cUI;
            public Ingest cInfo;
            public bool bPaused;
			public bool bCanceled;
			public bool bCompleted;

			public int Create(Source cSource, Target cTarget)
			{
				_cSource = cSource;
				_cTarget = cTarget;

				bCompleted = false;

				return GetHashCode();
			}
			public void Start()
			{
				System.Threading.ThreadPool.QueueUserWorkItem(ProcessWorker, this);
			}

			static private void ProcessWorker(object cState)
			{
                FileTask cFileTask = (FileTask)cState;
				try
				{
					int nBytesReaded = 1024 * 512;
					byte[] aBytes = new byte[nBytesReaded];
					while(0 < nBytesReaded)
					{
                        if (cFileTask.bCanceled)
							break;
                        if (cFileTask.bPaused)
						{
							System.Threading.Thread.Sleep(300);
							continue;
						}
                        nBytesReaded = cFileTask._cSource.cStream.Read(aBytes, 0, nBytesReaded);
                        cFileTask._cTarget.cStream.Write(aBytes, 0, nBytesReaded);
                        cFileTask._cTarget.nBytesTotal += nBytesReaded;
                        if (null != cFileTask.cUI)
                            cFileTask.cUI.Update((float)cFileTask._cTarget.nBytesTotal / cFileTask._cSource.nBytesTotal);
					}
				}
				catch { }

                cFileTask.bCompleted = true;
                if (null != cFileTask.cUI)
                    cFileTask.cUI.Complete();
			}
			public void Close()
			{
				_cSource.Close();
				_cTarget.Close();
                Completed(this, bCanceled ? null :  new EventArgs());
            }
		}
		private Progress _dlgProgress = new Progress();
        private Person _cSelectedPerson = null;
		private DBInteract _cDBI;
        private DateTime dtNextMouseClickForDouble;
        private MsgBox _cMsgBox;
		private Dictionary<string, string> _ahTransliteration;
        private int _nFullRights = -1;
		private Dictionary<Preferences.Ingest.Tab.Type, swc.Grid> _ahGrids;

        public ingest()
        {
            InitializeComponent();

			_ahGrids = new Dictionary<Preferences.Ingest.Tab.Type, swc.Grid>();
			_ahGrids.Add(Preferences.Ingest.Tab.Type.clip, _ui_gClip);
			_ahGrids.Add(Preferences.Ingest.Tab.Type.advertisement, _ui_gAdvertisement);
			_ahGrids.Add(Preferences.Ingest.Tab.Type.program, _ui_gProgram);
			_ahGrids.Add(Preferences.Ingest.Tab.Type.design, _ui_gDesign);

            Title = g.Helper.sCheckPoint;
            _cMsgBox = new MsgBox();
			_ahTransliteration = new Dictionary<string, string>();
            dtNextMouseClickForDouble = DateTime.Now;

            _cDBI = new DBInteract();
            //_cDBI.GetRightsForPageCompleted += new EventHandler<GetRightsForPageCompletedEventArgs>(_cDBI_GetRightsForPageCompleted);
			_cDBI.TransliterationGetCompleted += _cDBI_TransliterationGetCompleted;
			_cDBI.ArtistsGetCompleted += _cDBI_ArtistsGetCompleted;
            _cDBI.PersonSaveCompleted += _cDBI_PersonSaveCompleted;
            _cDBI.PersonsRemoveCompleted += _cDBI_PersonsRemoveCompleted;
            _cDBI.ProgramsGetCompleted += _cDBI_ProgramsGetCompleted;
            _cDBI.IngestCompleted += _cDBI_IngestCompleted;
            _cDBI.StoragesGetCompleted += _cDBI_StoragesGetCompleted;

            //_cDBI.GetRightsForPageAsync("ingest");
			_nFullRights = 1;

			foreach (swc.Grid ui_g in _ahGrids.Values)
			{
				((swc.Grid)ui_g.Parent).Children.Remove(ui_g);
				ui_g.Visibility = Visibility.Visible;
			}

			_ui_ddlNewTypes.ItemsSource = Enum.GetValues(typeof(Preferences.Ingest.Tab.Type)).Cast<Preferences.Ingest.Tab.Type>().Select(o => new IdNamePair() { nID = (long)o, sName = o.ToStr() }).ToArray();
			foreach (Preferences.Ingest.Tab cTab in Preferences.Ingest.aTabs)
				TabAdd(new Tab() { cData = cTab });             
            _ui_tcPresets.SelectedItem = null;
			if (!_ui_rpFiles.IsOpen)
				_ui_rpFiles.IsOpen = true;
			_dlgProgress.Show();
			_cDBI.TransliterationGetAsync();
        }

        #region event handlers
		#region UI
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }
		private void _ui_ddl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TargetNameMake();
		}
        private void _ui_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TargetNameMake();
        }
        private void _ui_nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TargetNameMake();
        }

        private void _ui_rpFiles_Loaded(object sender, RoutedEventArgs e)
		{
            _ui_scClipSearch.ItemAdd = _ui_scClipSearch_Add;
            _ui_scClipSearch.sCaption = "";
            _ui_scClipSearch.nGap2nd = 0;
            _ui_scClipSearch.AddButtonWidth = _ui_btnBrowse.ActualWidth;
            _ui_scClipSearch.sDisplayMemberPath = "sName";
			_ui_scClipSearch.DataContext = _ui_lbClipArtists;
            _ui_lbClipArtistsSelected.Background = Coloring.Notifications.cTextBoxActive;

            _ui_ddlAir.ItemsSource = new IdNamePair[] {
                        new IdNamePair() { nID = 0, sName = g.Common.sNo.ToLower() },
                        new IdNamePair() { nID = 1, sName = g.Common.sYes.ToLower() }
                    };

            _ui_ddlFormat.ItemsSource = new IdNamePair[] {
                        new IdNamePair() { nID = -240, sName = "240p" },
                        new IdNamePair() { nID = -360, sName = "360p" },
                        new IdNamePair() { nID = 480, sName = "480i" },
                        new IdNamePair() { nID = -480, sName = "480p" },
                        new IdNamePair() { nID = 576, sName = "576i (pal)" },
                        new IdNamePair() { nID = -576, sName = "576p" },
                        new IdNamePair() { nID = -720, sName = "720p (hd)"},
                        new IdNamePair() { nID = 1080, sName = "1080i (fhd)" },
                        new IdNamePair() { nID = -1080, sName = "1080p" },
                        new IdNamePair() { nID = -1440, sName = "1440p (qhd)" },
                        new IdNamePair() { nID = -2160, sName = "2160p (4k)" },
                        new IdNamePair() { nID = -4320, sName = "4320p (5k)" }
                    };

            _ui_ddlFPS.ItemsSource = new IdNamePair[] {
                        new IdNamePair() { nID = 24, sName = "24" },
                        new IdNamePair() { nID = 25, sName = "25" },
                        new IdNamePair() { nID = 30, sName = "30" },
                        new IdNamePair() { nID = 48, sName = "48" },
                        new IdNamePair() { nID = 50, sName = "50" },
                        new IdNamePair() { nID = 60, sName = "60" }
                    };
            _ui_txtFilename.Text = "";
            _ui_txtFilename.Tag = null;

            _ui_ddlAge.SelectedIndex = 1;
            _ui_ddlAction.SelectedIndex = 0;
            _ui_ddlVersion.SelectedIndex = 0;
            _ui_ddlAir.SelectedIndex = 1;
            _ui_ddlFormat.SelectedIndex = 7;
            _ui_ddlFPS.SelectedIndex = 1;
        }
        private void _ui_tcPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null == _ui_tcPresets || null == _ui_tcPresets.SelectedItem)
                return;
			if (null == _ui_tcPresets.TabGet())
			{
				if (null == _ui_ddlNewStorages.ItemsSource)
				{
					_dlgProgress.Show();
					_cDBI.StoragesGetAsync();
					List<string> aRoots = new List<string>();
					try
					{
						foreach (var oDrive in AutomationFactory.CreateObject("Scripting.FileSystemObject").Drives)
						{
							try
							{
								aRoots.Add(oDrive.DriveLetter + ":\\");
							}
							catch { }
						}
					}
					catch
					{
						aRoots.AddRange(sio.Directory.EnumerateDirectories("/"));
					}
					TreeViewItem ui_tvi;
					foreach (string sRoot in aRoots.OrderBy(o => o))
					{
						ui_tvi = new TreeViewItem() { Header = sRoot, Tag = sRoot };
						ui_tvi.Items.AddRange(sio.Directory.EnumerateDirectories(sRoot).OrderBy(o => o).Select(o => new TreeViewItem() { Header = sio.Path.GetFileName(o), Tag = o }).ToList());
						ui_tvi.Expanded += new RoutedEventHandler(ui_tvi_Expanded);
						_ui_tvNewFolder.Items.Add(ui_tvi);
					}
				}
				return;
			}
            PresetDefaults();
        }
		private void ui_tvi_Expanded(object sender, RoutedEventArgs e)
		{
			_dlgProgress.Show();
			foreach (TreeViewItem ui_tvi in ((TreeViewItem)sender).Items)
			{
				try
				{
					ui_tvi.Items.AddRange(sio.Directory.EnumerateDirectories((string)ui_tvi.Tag).OrderBy(o => o).Select(o => new TreeViewItem() { Header = sio.Path.GetFileName(o), Tag = o }).ToList());
					ui_tvi.Expanded += new RoutedEventHandler(ui_tvi_Expanded);
				}
				catch { }
			}
			_dlgProgress.Close();
		}

        private void _ui_btnBrowse_Click(object sender, RoutedEventArgs e)
        {
			try
			{
				OpenFileDialog cOFD = new OpenFileDialog();
                cOFD.Filter = "(*.*)|*.*|QuickTime Movies (*.mov)|*.mov|MPEG Files (*.mpg)|*.mpg|Material eXchange Format (*.mxf)|*.mxf|Audio Video Interleaved (*.avi)|*.avi|Windows Media Video (*.wmv)|*.wmv";
				if ((bool)cOFD.ShowDialog())
				{
					_ui_txtFilename.Text = cOFD.File.Name;
					_ui_txtFilename.Tag = cOFD.File;
					TargetNameMake();
				}
			}
			catch (Exception ex)
			{
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
			}
			TargetNameMake();
		}
		private void _ui_btnSave_Click(object sender, RoutedEventArgs e)
		{
			try
			{
                Tab cTab = _ui_tcPresets.TabGet();
				sio.FileInfo cFileInfo = new sio.FileInfo(sio.Path.Combine(cTab.cData.sPath, _ui_txtTargetName.Text));
                if (cFileInfo.Exists)
                {
                    if (MessageBoxResult.OK == MessageBox.Show(g.Common.sDoYouWantToReplaceExistedFile, g.Common.sAttention, MessageBoxButton.OKCancel))
						cFileInfo.Delete();
                    else
                        return;
                }

                FileTask cFileTask = new FileTask() { cInfo = cTab.cIngestInfo };
                cFileTask.Create(
                    new FileTask.Source((sio.FileInfo)_ui_txtFilename.Tag),
                    new FileTask.Target(_ui_txtTargetName.Text, cFileInfo.Create())
                );
                cFileTask.Completed += cFileTaskUI_Completed;
                FileTask.UI cFileTaskUI = new FileTask.UI(cFileTask);
                cFileTaskUI.Create(_ui_gTasksLayout, _ui_txtFilename.Text, _ui_txtTargetName.Text);
                cFileTask.Start();
				if (Preferences.Ingest.Tab.Type.program == cTab.cData.eType)
                    _ui_nudProgramPart.Value++; //we don't reset to defaults for program, just advance part number
                else
                    PresetDefaults();
				if (!_ui_rpTasks.IsOpen)
					_ui_rpTasks.IsOpen = true;
			}
			catch (Exception ex)
			{
                ShowError(ex.Message + Environment.NewLine + ex.StackTrace, true, null);
			}
		}

        private void _ui_lbClipArtists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dtNextMouseClickForDouble < DateTime.Now)
                dtNextMouseClickForDouble = DateTime.Now.AddMilliseconds(400);
            else
            {
                Person cSelectedPerson = (Person)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
                dtNextMouseClickForDouble = DateTime.MinValue;
                _ui_lbClipArtistSelectedAdd(cSelectedPerson);
            }
        }
        private void _ui_lbClipArtistSelectedAdd(Person cPers)
        {
            if (null != cPers)
            {
                List<Person> aPersons;
                aPersons = ((IEnumerable<Person>)_ui_lbClipArtistsSelected.ItemsSource).ToList<Person>();
                if (!aPersons.Contains(cPers))
                {
                    aPersons.Add(cPers);
                    _ui_lbClipArtistsSelected.ItemsSource = aPersons;
					_ui_lbClipArtistsSelected.Background = Coloring.Notifications.cTextBoxChanged;
                }
                _ui_lbClipArtistsSelected.UpdateLayout();
            }
            TargetNameMake();
        }
		private void _ui_lbClipArtistsSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            Person cSelectedPerson = (Person)_ui_lbClipArtistsSelected.SelectedItem;
            _ui_lbClipArtistSelectedRemove(cSelectedPerson);
		}
        private void _ui_lbClipArtistSelectedRemove(Person cPers)
        {
            if (null != cPers)
            {
                List<Person> aPersons;
                aPersons = ((IEnumerable<Person>)_ui_lbClipArtistsSelected.ItemsSource).ToList<Person>();
                if (aPersons.Contains(cPers))
                {
                    aPersons.Remove(cPers);
                    _ui_lbClipArtistsSelected.ItemsSource = aPersons;
                }
                _ui_lbClipArtistsSelected.UpdateLayout();
            }
            if (0 == ((IEnumerable<Person>)_ui_lbClipArtistsSelected.ItemsSource).ToList<Person>().Count)
                _ui_lbClipArtistsSelected.Background = Coloring.Notifications.cTextBoxActive;
            TargetNameMake();
        }
        private void _ui_lbClipArtists_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _cSelectedPerson = (Person)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
        }
        private void _ui_cmClipArtists_Opened(object sender, RoutedEventArgs e)
        {
            if (null != _cSelectedPerson)
            {
                _ui_cmClipArtistsDelete.Header = g.Common.sDelete + ": " + _cSelectedPerson.sName;
                _ui_cmClipArtistsRename.Header = g.Common.sRename + ": " + _cSelectedPerson.sName;
            }
            switch (_nFullRights)
            {
                case -1:
                    _ui_cmClipArtistsDelete.IsEnabled = false;
                    _ui_cmClipArtistsRename.IsEnabled = false;
                    break;
                case 0:
                    _ui_cmClipArtistsDelete.IsEnabled = false;
                    _ui_cmClipArtistsRename.IsEnabled = false;
                    break;
                case 1:
                    _ui_cmClipArtistsDelete.IsEnabled = true;
                    _ui_cmClipArtistsRename.IsEnabled = true;
                    break;
                default:
                    break;
            }
        }
        private void _ui_cmClipArtists_Refresh(object sender, RoutedEventArgs e)
        {
            _dlgProgress.Show();
            _cDBI.ArtistsGetAsync();
        }
        private void _ui_cmClipArtists_Delete(object sender, RoutedEventArgs e)
        {
            if (null == _cSelectedPerson)
                _cMsgBox.Show(g.Common.sNoItemsSelected);
            else
            {
                _dlgProgress.Show();
                _ui_lbClipArtistSelectedRemove(_cSelectedPerson);
                _cDBI.PersonsRemoveAsync(new Person[] {_cSelectedPerson});
            }
        }
        private void _ui_cmClipArtists_Rename(object sender, RoutedEventArgs e)
        {
            _cMsgBox.Show(g.Helper.sEditArtistName + ":", g.Common.sRenaming, MsgBox.MsgBoxButton.OKCancel, _cSelectedPerson.sName);
            _cMsgBox.Closed += new EventHandler(_cMsgBox_Closed);
        }
        private void _ui_scClipSearch_Add(string sText)
        {
            _dlgProgress.Show();
			sText = sText.ToLower().Trim();
            _cDBI.PersonSaveAsync(new Person() { sName = sText, cType = new IdNamePair() { nID = 1, sName = "artist" }, nID = -1 });
			_ui_scClipSearch.Tag = sText;
			_ui_scClipSearch.Clear();
        }

		private void TabAdd_Click(object sender, RoutedEventArgs e)
		{
			if (null == _ui_tvNewFolder.SelectedItem || _ui_tbNewCaption.Text.IsNullOrEmpty() || null == _ui_ddlNewTypes.SelectedItem || null == _ui_ddlNewStorages.SelectedItem)
			{
				MsgBox.Error(g.Replica.sErrorIngest2);
				return;
			}
			Tab cTab = new Tab()
			{
				cData = new Preferences.Ingest.Tab()
				{
					sPath = (string)((TreeViewItem)_ui_tvNewFolder.SelectedItem).Tag,
					cStorage = (Storage)_ui_ddlNewStorages.SelectedItem,
					eType = (Preferences.Ingest.Tab.Type)((IdNamePair)_ui_ddlNewTypes.SelectedItem).nID,
					sCaption = _ui_tbNewCaption.Text
				}
			};
			Preferences.Ingest.TabAdd(cTab.cData);
			_ui_tcPresets.SelectedItem = TabAdd(cTab);
			((TreeViewItem)_ui_tvNewFolder.SelectedItem).IsSelected = false;
		}

        void _cMsgBox_Closed(object sender, EventArgs e)
        {
            if (MsgBox.MsgBoxButton.OK == _cMsgBox.enMsgResult)
            {
                _dlgProgress.Show();
                _cSelectedPerson.sName = _cMsgBox.sText;
                _cDBI.PersonSaveAsync(_cSelectedPerson, _cSelectedPerson.nID); //второй параметр - для отмотки lbox на него
            }
        }
		void cTimer_Tick(object sender, EventArgs e)
		{
			((System.Windows.Threading.DispatcherTimer)_ui_txtError.Tag).Stop();
			_ui_txtError.Visibility = Visibility.Collapsed;
		}
        void cFileTaskUI_Completed(object sender, EventArgs e)
        {
            if (null != e)
            {
                FileTask cFileTask = (FileTask)sender;
                _cDBI.IngestAsync(cFileTask.cInfo, cFileTask);
            }
        }
		#endregion
		#region DBI
		void _cDBI_TransliterationGetCompleted(object sender, TransliterationGetCompletedEventArgs e)
        {
            if (null != e.Result && 0 < e.Result.Count())
            {
                foreach (TransliterationPair cTLP in e.Result)
                    _ahTransliteration.Add(cTLP.sSource, cTLP.sTarget);
            }
            else
            {
                _cMsgBox.ShowError(g.Replica.sErrorIngest1 + "...");
                this.NavigationService.Navigate(new Uri("/auth", UriKind.Relative));
            }
            _dlgProgress.Close();
        }
        void _cDBI_ArtistsGetCompleted(object sender, ArtistsGetCompletedEventArgs e)
        {
            if (null != e.Result)
            {
                Dictionary<string, Person> ahPersons = e.Result.ToDictionary(prsn => prsn.sName);
                if (null == _ui_lbClipArtistsSelected.ItemsSource)
                {
                    _ui_lbClipArtistsSelected.ItemsSource = new List<Person>();
                    _ui_lbClipArtistsSelected.Background = Coloring.Notifications.cTextBoxActive;
                    _ui_tcPresets.TabGet().bInited = true;
                    PresetInit();
                    PresetDefaults();
                }

                //_ui_lbArtists.ItemsSource = null;
                _ui_lbClipArtists.Tag = ahPersons.Values.ToList();
                _ui_lbClipArtists.ItemsSource = ahPersons.Values;
				_ui_scClipSearch.DataContextUpdateInitial();
                //_ui_scClipSearch.Search();
                _ui_lbClipArtists.UpdateLayout();
                if (null != e.UserState && e.UserState is long)
                {
                    Person[] aPers = e.Result.Where(pers => pers.nID.Equals(e.UserState)).ToArray();
                    ScrollToPerson(aPers);
                }
                if (null != _ui_scClipSearch.Tag) // && 0 == _ui_scClipSearch._ui_TextBox.Text.Length
                {
                    Person[] aPers = e.Result.Where(pers => pers.sName.Equals(_ui_scClipSearch.Tag.ToString())).ToArray();
                    ScrollToPerson(aPers);
                    _ui_scClipSearch.Tag = null;
                }
                //_ui_tbArtist.Text = "";
            }
            _dlgProgress.Close();
        }
        void _cDBI_PersonSaveCompleted(object sender, PersonSaveCompletedEventArgs e)
        {
            if (1 > e.Result)
                _cMsgBox.ShowError();
            _cDBI.ArtistsGetAsync(e.UserState);
        }
        void _cDBI_IngestCompleted(object sender, IngestCompletedEventArgs e)
        {
            if (null == e.Result)
            {
                ListBox cLB = new ListBox();
                cLB.ItemsSource = new string[] { ((FileTask)e.UserState).cInfo.sFilename };
                ShowError(" " + g.Common.sErrorDataSave.ToLower() + ": ", true, cLB);
                ((FileTask)e.UserState).cUI.ShowError();
            }
        }
        void _cDBI_PersonsRemoveCompleted(object sender, PersonsRemoveCompletedEventArgs e)
        {
            _cDBI.ArtistsGetAsync();
        }
        void _cDBI_ProgramsGetCompleted(object sender, ProgramsGetCompletedEventArgs e)
        {
            List<Program> aSeries;
            if (null != e.Result && 0 < (aSeries = e.Result.Where(o => null != o.cType && AssetType.series == o.cType.eType && 0 < e.Result.Count(o1 => o.nID == o1.nIDParent)).OrderBy(o => o.sName).ToList()).Count)
            {//aSeries has only series with episodes
                _ui_ddlProgramSeries.ItemsSource = aSeries;
                _ui_ddlProgramEpisodes.ItemsSource = null;
                _ui_ddlProgramEpisodes.Tag = e.Result.Where(o => 0 < aSeries.Count(o1 => o.nIDParent == o1.nID)); //only episodes
            }
            else
            {
                _ui_ddlProgramSeries.ItemsSource = _ui_ddlProgramEpisodes.ItemsSource = new Program[] { new Program() { nID = -1, sName = g.Common.sMissing1.ToLower() } };
                _ui_ddlProgramEpisodes.SelectedIndex = 0;
                _ui_ddlProgramEpisodes.Tag = null;
            }
            _ui_ddlProgramSeries.SelectedIndex = 0;
            _ui_tcPresets.TabGet().bInited = true;
            PresetDefaults();

            _dlgProgress.Close();
        }
        void _cDBI_StoragesGetCompleted(object sender, StoragesGetCompletedEventArgs e)
        {
            try
            {
                if (null != e.Error)
                    throw e.Error;
				_ui_ddlNewStorages.ItemsSource = e.Result;
            }
            catch (Exception ex)
            {
                (new MsgBox()).ShowError(ex);
            }
            _dlgProgress.Close();
        }
		#endregion
        #endregion

        private void PresetInit()
        {
            Tab cTab = _ui_tcPresets.TabGet();
			switch (cTab.cData.eType)
            {
				case Preferences.Ingest.Tab.Type.clip:
                    if (cTab.bInited)
                    {
                        _ui_ddlClipQuality.ItemsSource = new IdNamePair[] {
                            new IdNamePair() { nID = 0, sName = g.Replica.sNoticeIngest11 },
                            new IdNamePair() { nID = 1, sName = g.Replica.sNoticeIngest12 },
                            new IdNamePair() { nID = 2, sName = g.Replica.sNoticeIngest13 }
                        };

                        _ui_ddlClipShow.ItemsSource =
                            _ui_ddlClipRemix.ItemsSource =
                            _ui_ddlClipPromo.ItemsSource =
                            _ui_ddlClipCut.ItemsSource =
                            _ui_ddlClipForeign.ItemsSource =
                                new IdNamePair[] {
                                    new IdNamePair() { nID = 0, sName = g.Common.sNo.ToLower() },
                                    new IdNamePair() { nID = 1, sName = g.Common.sYes.ToLower() }
                                };
                    }
                    else
                    {
                        _dlgProgress.Show();
                        _cDBI.ArtistsGetAsync();
                    }
                    return;
				case Preferences.Ingest.Tab.Type.advertisement:
                    break;
				case Preferences.Ingest.Tab.Type.program:
                    _dlgProgress.Show();
                    _cDBI.ProgramsGetAsync();
                    return;
				case Preferences.Ingest.Tab.Type.design:
                    _ui_ddlDesignDTMF.ItemsSource = new IdNamePair[] {
                        new IdNamePair() { nID = 0, sName = g.Common.sNo.ToLower() },
                        new IdNamePair() { nID = 1, sName = g.Common.sYes.ToLower() }
                    };
                    break;
            }
            cTab.bInited = true;
            PresetDefaults();
        }
        private void PresetDefaults()
		{
            Tab cTab = _ui_tcPresets.TabGet();
			swc.Grid ui_g = _ahGrids[_ui_tcPresets.TabGet().cData.eType];
			if (null != ui_g.Parent)
				((TabItem)ui_g.Parent).Content = null;
			((TabItem)_ui_tcPresets.SelectedItem).Content = ui_g;
			if (!cTab.bInited)
            {
                PresetInit();
                if (!cTab.bInited)
                    return;
            }
            switch (cTab.cData.eType)
            {
				case Preferences.Ingest.Tab.Type.clip:
                    _ui_ddlAge.SelectedIndex = 0;
                    _ui_nudAge.Value = 12;
                    _ui_ddlAction.SelectedIndex = 1;

                    _ui_tbClipSong.Text = "";
                    _ui_lbClipArtistsSelected.ItemsSource = new List<Person>();
                    _ui_lbClipArtistsSelected.Background = Coloring.Notifications.cTextBoxActive;
                    _ui_lbClipArtists.ItemsSource = (List<Person>)_ui_lbClipArtists.Tag;
                    _ui_scClipSearch.DataContextUpdateInitial();
                    _ui_ddlClipQuality.SelectedIndex = 1;
                    _ui_ddlClipShow.SelectedIndex =
                    _ui_ddlClipRemix.SelectedIndex =
                    _ui_ddlClipPromo.SelectedIndex =
                    _ui_ddlClipCut.SelectedIndex =
                    _ui_ddlClipForeign.SelectedIndex = 0;
                    break;
				case Preferences.Ingest.Tab.Type.advertisement:
                    _ui_ddlAge.SelectedIndex = 1;
                    _ui_nudAge.Value = 12;
                    _ui_ddlAction.SelectedIndex = 1;

					_ui_tbAdvertisementID.Text = "";
                    break;
				case Preferences.Ingest.Tab.Type.program:
                    _ui_ddlAge.SelectedIndex = 1;
                    _ui_nudAge.Value = 3;
                    _ui_ddlAction.SelectedIndex = 1;
                    break;
				case Preferences.Ingest.Tab.Type.design:
                    _ui_ddlAge.SelectedIndex = 0;
                    _ui_nudAge.Value = 12;
                    _ui_ddlAction.SelectedIndex = 1;

					_ui_ddlDesignType.SelectedIndex =
                        _ui_ddlDesignDTMF.SelectedIndex = 0;
                    _ui_tbDesignName.Text = "";
                    break;
            }
            TargetNameMake();
        }
        private void ScrollToPerson(Person[] aPers)
        {
            if (0 < aPers.Length)
            {
                //_ui_lbArtistSelectedAdd(cPers[0]);
                _ui_lbClipArtists.ScrollIntoView(aPers[0]);
                _ui_lbClipArtists.SelectedItem = aPers[0];
            }
        }
		private string Transliterate(string sSource)
		{
			string sRetVal = "";
			string sValue, sPrevious = null;
			foreach (char sChar in sSource)
			{
				sValue = sChar.ToString();
				if (_ahTransliteration.ContainsKey(sValue))
					sValue = _ahTransliteration[sValue];
                else if (!char.IsLetterOrDigit(sChar) || sChar > 'z')
					continue;

                if (0 < sValue.Length && ("_" != sValue || sValue != sPrevious) && "NO" != sValue)
				{
					sRetVal += sValue;
					sPrevious = sValue;
				}
			}
			return sRetVal;
		}

		private void TargetNameMake()
		{
            if (null == _ui_txtTargetName || null == _ui_tcPresets.SelectedItem)
                return;
            Tab cTab = _ui_tcPresets.TabGet();
            string sDelimeter = "__", sPath = "???", sExtention = "", sPartCommon, sPartItem = "", sPartVersion = "", sValue;
            _ui_txtTargetName.Tag = null;
            try
            {

                switch (cTab.cData.eType)
                {
					case Preferences.Ingest.Tab.Type.clip:
                        if (0 < _ui_lbClipArtistsSelected.Items.Count)
                        {
                            foreach (Person cPerson in _ui_lbClipArtistsSelected.ItemsSource)
                                sPartItem += Transliterate(cPerson.sName.ToLower()) + sDelimeter;
                        }
                        else
                        {
                            sPartItem = "???" + sDelimeter;
                            _ui_txtTargetName.Tag = false;
                        }


                        sValue = _ui_tbClipSong.Text.ToLower().Trim();
                        if (sValue.IsNullOrEmpty())
                        {
                            sValue = "???";
                            _ui_txtTargetName.Tag = false;
                        }
                        else
                            sValue = Transliterate(sValue);
                        sPartItem += sValue + sDelimeter;

                        sPartItem += "q" + ((IdNamePair)_ui_ddlClipQuality.SelectedItem).nID;
                        sPartItem += "s" + ((IdNamePair)_ui_ddlClipShow.SelectedItem).nID;
                        sPartItem += "r" + ((IdNamePair)_ui_ddlClipRemix.SelectedItem).nID;
                        sPartItem += "p" + ((IdNamePair)_ui_ddlClipPromo.SelectedItem).nID;
                        sPartItem += "c" + ((IdNamePair)_ui_ddlClipCut.SelectedItem).nID;
                        sPartItem += "f" + ((IdNamePair)_ui_ddlClipForeign.SelectedItem).nID;

                        cTab.cIngestInfo = new IngestClip()
                        {
                            aArtists = ((IEnumerable<Person>)_ui_lbClipArtistsSelected.ItemsSource).ToArray(),
                            sSongName = _ui_tbClipSong.Text,
                            nQuality = (byte)((IdNamePair)_ui_ddlClipQuality.SelectedItem).nID,
                            bLocation = (0 < _ui_ddlClipShow.SelectedIndex),
                            bRemix = (0 < _ui_ddlClipRemix.SelectedIndex),
                            bPromo = (0 < _ui_ddlClipPromo.SelectedIndex),
                            bCutted = (0 < _ui_ddlClipCut.SelectedIndex),
                            bForeign = (0 < _ui_ddlClipForeign.SelectedIndex)
                        };
                        break;
					case Preferences.Ingest.Tab.Type.advertisement:
                        sValue = _ui_tbAdvertisementCompany.Text.ToLower().Trim();
                        if (sValue.IsNullOrEmpty())
                        {
                            sValue = "???";
                            _ui_txtTargetName.Tag = false;
                        }
                        else
                            sValue = Transliterate(sValue);
                        sPartItem = sValue;

						IngestAdvertisement cIngestAdvertisement = new IngestAdvertisement(){ sCompany = _ui_tbAdvertisementCompany.Text };

                        sValue = _ui_tbAdvertisementCampaign.Text.ToLower().Trim();
                        if (!sValue.IsNullOrEmpty())
						{
							sPartItem += sDelimeter + Transliterate(sValue);
							cIngestAdvertisement.sCampaign = _ui_tbAdvertisementCampaign.Text;
						}

						sValue = _ui_tbAdvertisementID.Text.ToLower().Trim();
						if (!sValue.IsNullOrEmpty())
						{
							sPartItem += sDelimeter + Transliterate(sValue);
							cIngestAdvertisement.sID = _ui_tbAdvertisementID.Text;
						}
                        cTab.cIngestInfo = cIngestAdvertisement;
                    break;
					case Preferences.Ingest.Tab.Type.program:
                        _ui_txtTargetName.Tag = false;
                        sPartItem = "???" + sDelimeter + "???";
                        if (null != _ui_ddlProgramEpisodes.Tag)
                        {
                            Program cSeries = (Program)_ui_ddlProgramSeries.SelectedItem;
                            if (-1 < cSeries.nID)
                            {
                                string sSeriesName = cSeries.sName.Trim().ToLower();
                                sPartItem = Transliterate(sSeriesName) + sDelimeter;
                                Program cEpisode = (Program)_ui_ddlProgramEpisodes.SelectedItem;
                                if (null == cEpisode || cSeries.nID != cEpisode.nIDParent)
                                {
                                    _ui_ddlProgramEpisodes.ItemsSource = ((IEnumerable<Program>)_ui_ddlProgramEpisodes.Tag).Where(o => cSeries.nID == o.nIDParent);
                                    _ui_ddlProgramEpisodes.SelectedIndex = 0;
                                    cEpisode = (Program)_ui_ddlProgramEpisodes.SelectedItem;
                                }
                                sValue = cEpisode.sName.Trim().ToLower();
                                if (sValue.StartsWith(sSeriesName))
                                    sValue = sValue.Remove(sSeriesName).Trim(' ', ':'); //episodes names contain a series name by default. if so we will skip it 
                                sPartItem += Transliterate(sValue);
                                _ui_txtTargetName.Tag = null;
                            }
                            else
                            {
                                _ui_ddlProgramEpisodes.ItemsSource = (Program[])_ui_ddlProgramSeries.Tag;
                                _ui_ddlProgramEpisodes.SelectedIndex = 0;
                            }
                        }
                        else
                            _ui_ddlProgramSeries.SelectedIndex = _ui_ddlProgramEpisodes.SelectedIndex = 0;

                        if (0 < _ui_nudProgramPart.Value)
                            sPartItem += sDelimeter + _ui_nudProgramPart.Value;

                        cTab.cIngestInfo = new IngestProgram()
                        {
                            cSeries = (Asset)_ui_ddlProgramSeries.SelectedItem,
                            cEpisode = (Asset)_ui_ddlProgramEpisodes.SelectedItem,
                            nPart = (byte)_ui_nudProgramPart.Value
                        };
                        break;
					case Preferences.Ingest.Tab.Type.design:
						if (0 < _ui_ddlDesignSeason.SelectedIndex)
							sPartItem += (string)((ComboBoxItem)_ui_ddlDesignSeason.SelectedItem).Content + sDelimeter;

						if (1 > _ui_ddlDesignType.SelectedIndex)
                        {
                            sValue = "???";
                            _ui_txtTargetName.Tag = false;
                        }
                        else
							sValue = Transliterate((string)((ComboBoxItem)_ui_ddlDesignType.SelectedItem).Content);
						sPartItem += sValue + sDelimeter;

                        sValue = _ui_tbDesignName.Text.ToLower().Trim();
                        if (sValue.IsNullOrEmpty())
                        {
                            sValue = "???";
                            _ui_txtTargetName.Tag = false;
                        }
                        else
                            sValue = Transliterate(sValue);

                        sPartItem += sValue + sDelimeter + "d" + _ui_ddlDesignDTMF.SelectedIndex;

                        cTab.cIngestInfo = new IngestDesign()
                        {
							sSeason = (null == _ui_ddlDesignSeason.SelectedItem ? null : (string)((ComboBoxItem)_ui_ddlDesignSeason.SelectedItem).Content),
							sType = (null == _ui_ddlDesignType.SelectedItem?null : (string)((ComboBoxItem)_ui_ddlDesignType.SelectedItem).Content),
							bDTMF = (0 < _ui_ddlDesignDTMF.SelectedIndex),
                            sName = _ui_tbDesignName.Text,
                        };
                        break;
                }

                if (null != cTab.cData.sPath)
                {
					sPath = cTab.cData.sPath;
                    cTab.cIngestInfo.cStorage = cTab.cData.cStorage;
                }

                if (0 < _ui_ddlVersion.SelectedIndex)
                {
                    sPartVersion = "(" + _ui_nudVersion.Value.ToString() + ")";
                    cTab.cIngestInfo.nVersion = (byte?)_ui_nudVersion.Value;
                }
                else
                    cTab.cIngestInfo.nVersion = null;

                IdNamePair cINP = (IdNamePair)_ui_ddlFormat.SelectedItem;
                sPartCommon = Math.Abs(cINP.nID) + (0 > cINP.nID ? "p" : "i");
                sPartCommon += ((IdNamePair)_ui_ddlFPS.SelectedItem).nID;
                sPartCommon += "b" + _ui_ddlAir.SelectedIndex;

                cTab.cIngestInfo.nFormat = (int)((IdNamePair)_ui_ddlFormat.SelectedItem).nID;
                cTab.cIngestInfo.nFPS = (byte)((IdNamePair)_ui_ddlFPS.SelectedItem).nID;
                cTab.cIngestInfo.bBroadcast = (0 < _ui_ddlAir.SelectedIndex);

                sPartCommon += "a";
                if (0 < _ui_ddlAge.SelectedIndex)
                {
                    _ui_txtAction.Visibility = _ui_ddlAction.Visibility = Visibility.Visible;
                    sPartCommon += _ui_nudAge.Value;
                    cTab.cIngestInfo.nAge = (sbyte)_ui_nudAge.Value;
                    if (0 < _ui_ddlAction.SelectedIndex)
                    {
                        sPartCommon += "d";
                        cTab.cIngestInfo.nAge *= -1;
                    }
                    else
                        sPartCommon += "m";
               }
                else
                {
                    sPartCommon += "0";
                    _ui_txtAction.Visibility = _ui_ddlAction.Visibility = Visibility.Collapsed;
                    cTab.cIngestInfo.nAge = 0;
                }


                if (0 < _ui_txtFilename.Text.Length && null != _ui_txtFilename.Tag)
                {
                    sExtention += sio.Path.GetExtension(_ui_txtFilename.Text).ToLower();
                    cTab.cIngestInfo.sOriginalFile = ((sio.FileInfo)_ui_txtFilename.Tag).FullName;
                }
                else
                    _ui_txtTargetName.Tag = false;

				//(zhasmin__razgadai_lyubov__q1s0r0p0c0f0)(576i25b1a0).mov
				_ui_txtTargetName.Text = cTab.cIngestInfo.sFilename = sio.Path.Combine(sPath, "(" + sPartItem + ")(" + sPartCommon + ")" + sPartVersion + sExtention);
			}
			catch (Exception ex)
			{
				_ui_txtTargetName.Tag = false;
				MessageBox.Show(ex.Message + " " + ex.StackTrace);
			}
			_ui_btnSave.IsEnabled = (null == _ui_txtTargetName.Tag);
		}

		private void ShowMessage(string sText, bool bMessageBox, Color cColor, ListBox cLB)
		{
            if (!bMessageBox)
            {
                if (null == _ui_txtError.Tag)
                {
                    System.Windows.Threading.DispatcherTimer cTimer = new System.Windows.Threading.DispatcherTimer();
					cTimer.Interval = new System.TimeSpan(0, 0, 5);
                    cTimer.Tick += new EventHandler(cTimer_Tick);
                    _ui_txtError.Tag = cTimer;
                }
                _ui_txtError.Foreground = new SolidColorBrush(cColor);
                _ui_txtError.Text = sText;
                _ui_txtError.Visibility = Visibility.Visible;
                ((System.Windows.Threading.DispatcherTimer)_ui_txtError.Tag).Start();
            }
            else
                _cMsgBox.ShowAttention(sText, cLB);
		}
		private void ShowError(string sText, bool bMessageBox, ListBox cLB)
		{
            ShowMessage(sText, bMessageBox, Colors.Red, cLB);
		}
        private void ShowNotice(string sText, bool bMessageBox, ListBox cLB)
		{
			ShowMessage(sText, bMessageBox, Colors.Green, cLB);
		}

		private TabItem TabAdd(Tab cTab)
		{
			TabItem cRetVal = new TabItem() { Header = cTab.cData.sCaption, Tag = cTab };
			ContextMenu ui_cm = new ContextMenu();
			MenuItem ui_cmi = new MenuItem();
			ui_cm.Items.Add(ui_cmi);
			ui_cmi.Header = g.Common.sDelete.ToLower();
			ui_cmi.Click += delegate { Preferences.Ingest.TabRemove(cRetVal.ToTab().cData); _ui_tcPresets.Items.Remove(cRetVal); };
			ContextMenuService.SetContextMenu(cRetVal, ui_cm);
			_ui_tcPresets.Items.Insert(_ui_tcPresets.Items.Count - 1, cRetVal);
			return cRetVal;
		}

		private void _ui_tvNewFolder_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{

		}
	}
    static public class x
    {
        static public ingest.Tab TabGet(this TabControl ui)
        {
			if (null == ui.SelectedItem)
				return null;
            return ui.SelectedItem.ToTab();
        }
        static public ingest.Tab ToTab(this object o)
        {
			return (ingest.Tab)(((TabItem)o).Tag ?? null);
        }
    }
}
