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
								if (_cFileTask.bCanceled)
								{
									_ui_pb.Value = _ui_pb.Minimum;
                                    _ui_txtTarget.Text += " (" + g.Common.sCanceled.ToLower() + ")";
									_ui_txtTarget.Foreground = new SolidColorBrush(Colors.Red);
									_ui_btnPlayPause.Visibility = Visibility.Collapsed;
								}
                                if (!_cFileTask.bCompleted)
                                {
                                    _cFileTask.bCanceled = true;
                                    _ui_pb.Value = _ui_pb.Minimum;
                                    _ui_txtTarget.Text += " (" + g.Common.sError.ToUpper() + ": " + (_cFileTask.sError == null ? "unknown" : _cFileTask.sError) + ")";
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
                                _cFileTask.Close();
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
				public string sFile;
				public sio.Stream cStream;
				private sio.FileInfo cFI;

				public Source(sio.FileInfo cFI)
				{
					this.cFI = cFI;
					this.sFile = cFI.FullName;
					nBytesTotal = cFI.Length;
				}
				public void OpenStream()
				{
					this.cStream = cFI.OpenRead();
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
				public string sFileTMP;
				public string sFileNormal;
				public long nBytesTotal;
				private sio.FileInfo cFI;
                public sio.Stream cStream;

                public Target(sio.FileInfo cFI)
				{
					sFileNormal = cFI.FullName;
					sFileTMP = sio.Path.Combine(cFI.DirectoryName, cFI.Name + "!");
					if (sio.File.Exists(sFileTMP))
						sio.File.Delete(sFileTMP);
					this.cFI = new sio.FileInfo(sFileTMP);
					nBytesTotal = 0;
				}
				public void OpenStream()
				{
					cStream = cFI.Create();
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
				public void RestoreFilename()
				{
                    sio.File.Move(sFileTMP, sFileNormal);
				}
			}

            public event EventHandler Completed;
            
            private Source _cSource;
			private Target _cTarget;
            public File cSelectedFile;

            public UI cUI;
            public Ingest cInfo;
            public bool bPaused;
			public bool bCanceled;
			public bool bCompleted;
			public bool bCopy;
            public string sError;
			public int nIndx;
			public FileTask()
			{
				nIndx = -1;
			}
			public void RestoreFilename()
			{
				_cTarget.RestoreFilename();
			}
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
                    sio.FileInfo cFInfo = new sio.FileInfo(cFileTask._cSource.sFile);
                    DateTime dtModification = cFInfo.LastWriteTime;
                    if (cFileTask.bCopy || (sio.Path.GetPathRoot(cFileTask._cSource.sFile) != sio.Path.GetPathRoot(cFileTask._cTarget.sFileTMP)))
					{
						cFileTask._cSource.OpenStream();
						cFileTask._cTarget.OpenStream();
						int nBytesRead = 1024 * 512;
						byte[] aBytes = new byte[nBytesRead];
						while (0 < nBytesRead)
						{
							if (cFileTask.bCanceled)
								break;
							if (cFileTask.bPaused)
							{
								System.Threading.Thread.Sleep(300);
								continue;
							}
							nBytesRead = cFileTask._cSource.cStream.Read(aBytes, 0, nBytesRead);
							cFileTask._cTarget.cStream.Write(aBytes, 0, nBytesRead);
							cFileTask._cTarget.nBytesTotal += nBytesRead;
                            if (null != cFileTask.cUI)
								cFileTask.cUI.Update((float)cFileTask._cTarget.nBytesTotal / cFileTask._cSource.nBytesTotal);
						}
					}
					else
					{
						sio.File.Move(cFileTask._cSource.sFile, cFileTask._cTarget.sFileTMP);
						System.Threading.Thread.Sleep(300); // сталкивался с тем что иногда ошибка доступа к файлу идёт, если не ждать 
						if (null != cFileTask.cUI)
							cFileTask.cUI.Update((float)1);
					}
                    cFileTask.cInfo.dtSourceModification = dtModification;
                    cFileTask.bCompleted = true;
                }
				catch(Exception ex)
                {
                    cFileTask.sError = ex.ToString();
                }
                
                if (null != cFileTask.cUI)
                    cFileTask.cUI.Complete();
			}
			public void Close()
			{
				_cSource.Close();
                _cTarget.cStream?.Flush();
				_cTarget.Close();
				//_cTarget.RestoreFilename();   // после внесения в БД это надо...
				Completed(this, bCanceled ? null :  new EventArgs());
            }
		}
		class BatchItem
		{
			public TSRItem cTSR;
			public string sSourceFilename;
			public string sTargetFilename;
			public Ingest cIngest;
			public string sError;
		}
		private Progress _dlgProgress = new Progress();
        private Person _cSelectedPerson = null;
		private File _cSelectedFile = null;
        private bool __bNameMakeBlock = false;
        private int __nNameMakeBlockCount = 0;
        private bool _bNameMakeBlock
        {
            get
            {
                return __bNameMakeBlock;
            }
            set
            {
                if (value)
                    __nNameMakeBlockCount++;
                else
                    __nNameMakeBlockCount--;

                if ((!value && __nNameMakeBlockCount > 0) || value)
                    __bNameMakeBlock = true;
                else
                    __bNameMakeBlock = false;
            }
        }
        private DBInteract _cDBI;
        private DateTime dtNextMouseClickForDouble;
        private MsgBox _cMsgBox;
		private Dictionary<string, string> _ahTransliteration;
        private int _nFullRightsAsset = -1;
		private int _nFullRightsFile = -1;
		private Dictionary<Preferences.Ingest.Tab.Type, swc.Grid> _ahGrids;
		private List<BatchItem> _aBatch;
		private List<BatchItem> _aBatchErrors;
		private List<File> _aFiles;
		private int _nBatchIndex;
		private bool _bMove;
		private RegisteredTable[] _aRegisteredTables;
		private RegisteredTable _cRTStrings;
		private RegisteredTable _cRTAssets;
        private RegisteredTable _cRTDates;
        private bool _bAdditionalInfoGetting;

        private static int _nDefaultAgeClip = 0;
        private static int _nDefaultAgeProgram = -6;
        private static int _nDefaultAgeProgramNews = -1;
        private static int _nDefaultAgeAdverts = -12;
        private static int _nDefaultAgeDesign = 0;

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
			_aBatch = new List<BatchItem>();
			_aBatchErrors = new List<BatchItem>();
			dtNextMouseClickForDouble = DateTime.Now;

			_cDBI = new DBInteract();
            //_cDBI.GetRightsForPageCompleted += new EventHandler<GetRightsForPageCompletedEventArgs>(_cDBI_GetRightsForPageCompleted);
			_cDBI.TransliterationGetCompleted += _cDBI_TransliterationGetCompleted;
			_cDBI.ArtistsGetCompleted += _cDBI_ArtistsGetCompleted;
            _cDBI.PersonSaveCompleted += _cDBI_PersonSaveCompleted;
            _cDBI.PersonsRemoveCompleted += _cDBI_PersonsRemoveCompleted;
            _cDBI.ProgramsGetCompleted += _cDBI_ProgramsGetCompleted;
            _cDBI.IngestCompleted += _cDBI_IngestCompleted;
            _cDBI.IngestForReplacedFileCompleted += _cDBI_IngestForReplacedFileCompleted;
            _cDBI.StoragesGetCompleted += _cDBI_StoragesGetCompleted;
			_cDBI.IsThereSameCustomValueCompleted += _cDBI_IsThereSameCustomValueCompleted;
			_cDBI.IsThereSameFileCompleted += _cDBI_IsThereSameFileCompleted;

			_cDBI.ErrorLastGetCompleted += _cDBI_ErrorLastGetCompleted;
			_cDBI.TSRItemsGetCompleted += _cDBI_TSRItemsGetCompleted;
			_cDBI.AreThereSameFilesCompleted += _cDBI_AreThereSameFilesCompleted;
			_cDBI.IsThereSameCustomValuesCompleted += _cDBI_IsThereSameCustomValuesCompleted;
            _cDBI.FilesWithSourcesGetCompleted += _cDBI_FilesWithSourcesGetCompleted;
			_cDBI.RegisteredTablesGetCompleted += _cDBI_RegisteredTablesGetCompleted;
			_cDBI.FileAdditionalInfoGetCompleted += _cDBI_FileAdditionalInfoGetCompleted;
			_cDBI.FileCheckIsInPlaylistCompleted += _cDBI_FileCheckIsInPLCompleted;

            _cDBI.FilesAgeGetCompleted += _cDBI_FilesAgeGetCompleted;

           //_cDBI.GetRightsForPageAsync("ingest");
           _nFullRightsAsset = 1;
			_nFullRightsFile = 1;

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

            _ui_tbDesignTags.IsEnabled = false;


            _dlgProgress.Show();
			_cDBI.TransliterationGetAsync();

			cbAdvancedChecked(null, null);
        }

        #region event handlers
        #region UI
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }
		private void _ui_ddl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            if (sender is ComboBox && ((ComboBox)sender).Name == "_ui_ddlProgramSeries")
            {
                Asset cA = (Asset)((ComboBox)sender).SelectedItem;
                _cDBI.FilesAgeGetAsync(cA);
                return;
            }

            if (sender is ComboBox && ((ComboBox)sender).Name == "_ui_ddlProgramEpisodes")
                _ui_tbProgramPart.Tag = null;

            TargetNameMake();
        }
        private void _ui_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).Name == "_ui_tbDesignName")
            {
                _bNameMakeBlock = true;
                _ui_tbDesignTags.Text = ((TextBox)sender).Text.ToLower().Trim();
                _bNameMakeBlock = false;
            }

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
            _ui_tbFilename.Text = "";
            _ui_tbFilename.Tag = null;

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
						_cDBI.Logger_NoticeAsync("ingest", "before drives getting");
						
						foreach (var oDrive in AutomationFactory.CreateObject("Scripting.FileSystemObject").Drives)
						{
							_cDBI.Logger_NoticeAsync("ingest", "[oDrive=" + oDrive.DriveLetter + "]");
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
						try
						{
							_cDBI.Logger_NoticeAsync("ingest", "[enumerate dir = " + sRoot + "]");
							ui_tvi = new TreeViewItem() { Header = sRoot, Tag = sRoot };
							ui_tvi.Items.AddRange(sio.Directory.EnumerateDirectories(sRoot).OrderBy(o => o).Select(o => new TreeViewItem() { Header = sio.Path.GetFileName(o), Tag = o }).ToList());
							ui_tvi.Expanded += new RoutedEventHandler(ui_tvi_Expanded);
							_ui_tvNewFolder.Items.Add(ui_tvi);
						}
						catch (Exception ex) { _cDBI.Logger_ErrorAsync("ingest", "[drive is bad]<br>" + ex + "]"); }
					}
					_cDBI.Logger_NoticeAsync("ingest", "after enumerating");
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
                cOFD.Filter = Preferences.cServer.sFilesDialogFilter;
				if ((bool)cOFD.ShowDialog())
				{
					_ui_tbFilename.Text = cOFD.File.Name;
					_ui_tbFilename.Tag = cOFD.File;
					if (_ui_tcPresets.TabGet().cData.bBatch)
					{
						_aBatch = sio.Directory.EnumerateFiles(cOFD.File.DirectoryName).Where(o => o.ToLower().EndsWith(cOFD.File.Extension.ToLower())).Select(o => new BatchItem() { sSourceFilename = sio.Path.GetFileName(o) }).ToList();
						_ui_tbAdvertisementLog.Text = "Getting TSR info about files...";   //+ cOFD.File.Extension + "):\n\t" + _aBatchFilenames.ToEnumerationString("\n\t", "", null, null, false);
						_dlgProgress.Show();
						_cDBI.TSRItemsGetAsync(_aBatch.Select(o => o.sSourceFilename).ToArray());
					}
					if (!_ui_tbFilename.Text.IsNullOrEmpty() && _ui_lbFiles.SelectedItem != null)
						_ui_btnReplaceMove.IsEnabled = _ui_btnReplaceCopy.IsEnabled = true;

                    if (_ui_tbDesignName.Text.Trim().IsNullOrEmpty())
                    {
                        _bNameMakeBlock = true;
                        _ui_tbDesignName.Text = sio.Path.GetFileNameWithoutExtension(_ui_tbFilename.Text);
                        _bNameMakeBlock = false;
                    }
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
			Button cSave = (Button)sender;
			if (cSave.Name == "_ui_btnMove")
				_bMove = true;
			else
				_bMove = false;

			Tab cTab = _ui_tcPresets.TabGet();

			if (Preferences.cServer.bIsPgIdNeeded)
			{
				if (Preferences.Ingest.Tab.Type.clip == cTab.cData.eType && (_ui_tbClipPGID.Text == null || _ui_tbClipPGID.Text.Length < 1))
				{
					ShowError(g.Replica.sErrorIngest4.Fmt("PG_ID"), true, null);
					//_dlgProgress.Show();
					//_dlgProgress.Close();
					return;
				}
			}

			_dlgProgress.Show();
			if (cTab.cData.eType== Preferences.Ingest.Tab.Type.advertisement && cTab.cData.bBatch)
			{
                if (_aBatch.IsNullOrEmpty())
                {
                    ShowNotice(g.Replica.sNoticeIngest15, true, null);
                    return;
                }
                _cDBI.AreThereSameFilesAsync(_aBatch.Select(o => o.sTargetFilename).ToArray());
			}
			else
				_cDBI.IsThereSameFileAsync(sio.Path.GetFileName(_ui_txtTargetName.Text));
		}
		private void _ui_btnSave_BatchClick_Do()
		{
			cbNoAssetChecked(null, null);
			if (_nBatchIndex >= _aBatch.Count)
			{
				string sLog="", sErr="";
				foreach (BatchItem cBI in _aBatch)
				{
					if(cBI.sError.IsNullOrEmpty())
						sLog += "\n\t"+ cBI.sSourceFilename;
					else
						sErr += "\n\t" + cBI.sSourceFilename + "\t" + cBI.sError;
				}
				foreach (BatchItem cBI in _aBatchErrors)
				{
					if (!cBI.sError.IsNullOrEmpty())
						sErr += "\n\t" + cBI.sSourceFilename + "\t" + cBI.sError;
				}
				_ui_tbAdvertisementLog.Text += "\n____________________RESULT____________________";
				if (!sErr.IsNullOrEmpty())
					_ui_tbAdvertisementLog.Text += "\nErrors:" + sErr;
				if (!sLog.IsNullOrEmpty())
					_ui_tbAdvertisementLog.Text += "\nfiles copied successfully:" + sLog;
				_nBatchIndex = 0;
				//_cDBI.IngestsAsync(_aBatchIngests.ToArray());
				PresetDefaults(false);
				_dlgProgress.Close();
				return;
			}
				
			try
			{
				Tab cTab = _ui_tcPresets.TabGet();
				BatchItem cBI = _aBatch[_nBatchIndex];
				cBI.cIngest.bCreateAsset = cTab.cIngestInfo.bCreateAsset;
				((IngestAdvertisement)cBI.cIngest).cTSR = cBI.cTSR;
				_nBatchIndex++;
				var eBlock = cBI.cTSR.eBlock;
				sio.FileInfo cFileInfo = new sio.FileInfo(sio.Path.Combine(eBlock == Block.РЕКЛАМА ? cTab.cData.sPath : cTab.cData.sPathAdvanced, cBI.sTargetFilename));
				if (cFileInfo.Exists)
				{
					if (MessageBoxResult.OK == MessageBox.Show(g.Common.sDoYouWantToReplaceExistedFile, g.Common.sAttention, MessageBoxButton.OKCancel))
						cFileInfo.Delete();
					else
					{
						_ui_btnSave_BatchClick_Do();
						return;
					}
				}

				FileTask cFileTask = new FileTask() { cInfo = cBI.cIngest };
				cFileTask.bCopy = !_bMove;
				cFileTask.nIndx = _nBatchIndex - 1;
				cFileTask.cInfo.cStorage = eBlock == Block.РЕКЛАМА ? cTab.cData.cStorage : cTab.cData.cStorageAdvanced;
				sio.FileInfo sFNSrc = (sio.FileInfo)_ui_tbFilename.Tag;
				cFileTask.Create(
					new FileTask.Source(new sio.FileInfo(sio.Path.Combine(sFNSrc.DirectoryName, cBI.sSourceFilename))),
					new FileTask.Target(cFileInfo)
				);
				cFileTask.Completed += cFileTaskUI_Completed;
				FileTask.UI cFileTaskUI = new FileTask.UI(cFileTask);
				cFileTaskUI.Create(_ui_gTasksLayout, cBI.sSourceFilename, cBI.sTargetFilename);
				cFileTask.Start();
				if (!_ui_rpTasks.IsOpen)
					_ui_rpTasks.IsOpen = true;
			}
			catch (Exception ex)
			{
				ShowError(ex.Message + Environment.NewLine + ex.StackTrace, true, null);
			}
		}
		private void _ui_btnSave_Click_Do()
		{
			try
			{
				cbNoAssetChecked(null, null);
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
				cFileTask.bCopy = !_bMove;
				cFileTask.Create(
					new FileTask.Source((sio.FileInfo)_ui_tbFilename.Tag),
					new FileTask.Target(cFileInfo)
				);
				cFileTask.Completed += cFileTaskUI_Completed;
				FileTask.UI cFileTaskUI = new FileTask.UI(cFileTask);
				cFileTaskUI.Create(_ui_gTasksLayout, _ui_tbFilename.Text, _ui_txtTargetName.Text);
				cFileTask.Start();
				if (Preferences.Ingest.Tab.Type.program == cTab.cData.eType)
				{
					int nN;
					if (int.TryParse(_ui_tbProgramPart.Text, out nN))
						_ui_tbProgramPart.Text = "" + (nN + 1); //we don't reset to defaults for program, just advance part number
					else
						_ui_tbProgramPart.Text = "";
				}
				else
					PresetDefaults(false);
				if (!_ui_rpTasks.IsOpen)
					_ui_rpTasks.IsOpen = true;
			}
			catch (Exception ex)
			{
				ShowError(ex.Message + Environment.NewLine + ex.StackTrace, true, null);
			}
		}
		private void _ui_btnReplace_Click_Do()
		{
			if (_cSelectedFile == null)
				return;
			try
			{
				Tab cTab = _ui_tcPresets.TabGet();
				sio.FileInfo cFileInfoTarget = new sio.FileInfo(sio.Path.Combine(cTab.cData.sPath, _cSelectedFile.sFilename));
				if (cFileInfoTarget.Exists)
				{
					if (MessageBoxResult.OK == MessageBox.Show(g.Common.sDoYouWantToReplaceExistedFile, g.Common.sAttention, MessageBoxButton.OKCancel))
						cFileInfoTarget.Delete();
					else
						return;
				}

				FileTask cFileTask = new FileTask() { cInfo = cTab.cIngestInfo, cSelectedFile = _cSelectedFile };
				cFileTask.bCopy = !_bMove;
				cFileTask.Create(
					new FileTask.Source((sio.FileInfo)_ui_tbFilename.Tag),
					new FileTask.Target(cFileInfoTarget)
				);
				cFileTask.Completed += cFileReplaceTaskUI_Completed;
				FileTask.UI cFileTaskUI = new FileTask.UI(cFileTask);
				cFileTaskUI.Create(_ui_gTasksLayout, _ui_tbFilename.Text, _cSelectedFile.sFilename);
                cFileTask.Start();
				PresetDefaults(false);
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
            switch (_nFullRightsAsset)
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
			Tab cTab;
			if (_ui_cbAdvanced.IsChecked.Value)
			{
				if (_ui_tbPathAdverts.Text.IsNullOrEmpty() || _ui_tbPathTrailers.Text.IsNullOrEmpty() || null == _ui_ddlStorageAdverts.SelectedItem || null == _ui_ddlStorageTrailers.SelectedItem)
				{
					MsgBox.Error(g.Replica.sErrorIngest2);
					return;
				}
				cTab = new Tab()
				{
					cData = new Preferences.Ingest.Tab()
					{
						sPath = _ui_tbPathAdverts.Text,
						sPathAdvanced = _ui_tbPathTrailers.Text,
						cStorage = (Storage)_ui_ddlStorageAdverts.SelectedItem,
						cStorageAdvanced = (Storage)_ui_ddlStorageTrailers.SelectedItem,
						eType = (Preferences.Ingest.Tab.Type)((IdNamePair)_ui_ddlNewTypes.SelectedItem).nID,
						sCaption = _ui_tbNewCaption.Text,
						bBatch = true
					}
				};
			}
			else
			{
				if (null == _ui_tvNewFolder.SelectedItem || _ui_tbNewCaption.Text.IsNullOrEmpty() || null == _ui_ddlNewTypes.SelectedItem || null == _ui_ddlNewStorages.SelectedItem)
				{
					MsgBox.Error(g.Replica.sErrorIngest2);
					return;
				}
				cTab = new Tab()
				{
					cData = new Preferences.Ingest.Tab()
					{
						sPath = (string)((TreeViewItem)_ui_tvNewFolder.SelectedItem).Tag,
						cStorage = (Storage)_ui_ddlNewStorages.SelectedItem,
						eType = (Preferences.Ingest.Tab.Type)((IdNamePair)_ui_ddlNewTypes.SelectedItem).nID,
						sCaption = _ui_tbNewCaption.Text
					}
				};
			}
			Preferences.Ingest.TabAdd(cTab.cData);
			_ui_tcPresets.SelectedItem = TabAdd(cTab);
			((TreeViewItem)_ui_tvNewFolder.SelectedItem).IsSelected = false;
		}

		void _cMsgBox_Closed(object sender, EventArgs e)
        {
			_cMsgBox.Closed -= _cMsgBox_Closed;
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
		void cFileReplaceTaskUI_Completed(object sender, EventArgs e)
		{
			if (null != e)
			{
				FileTask cFileTask = (FileTask)sender;
                cFileTask.cSelectedFile.dtModification = cFileTask.cInfo.dtSourceModification;
                cFileTask.cSelectedFile.sSourceFile = cFileTask.cInfo.sOriginalFile;
                _cDBI.IngestForReplacedFileAsync(cFileTask.cSelectedFile, cFileTask);
            }
		}

		private void hbPathAdvertsClick(object sender, RoutedEventArgs e)
		{
			if (null == _ui_tvNewFolder.SelectedItem)
			{
				MsgBox.Error(g.Replica.sErrorIngest2);
				return;
			}
			_ui_tbPathAdverts.Text = (string)((TreeViewItem)_ui_tvNewFolder.SelectedItem).Tag;
		}
		private void hbPathTrailersClick(object sender, RoutedEventArgs e)
		{
			if (null == _ui_tvNewFolder.SelectedItem)
			{
				MsgBox.Error(g.Replica.sErrorIngest2);
				return;
			}
			_ui_tbPathTrailers.Text = (string)((TreeViewItem)_ui_tvNewFolder.SelectedItem).Tag;
		}
		private void cbAdvancedChecked(object sender, RoutedEventArgs e)
		{
			if (null != _ui_cbAdvanced.IsChecked && _ui_cbAdvanced.IsChecked.Value)
			{
				_ui_spAdv1.Visibility = Visibility.Visible;
				_ui_spAdv2.Visibility = Visibility.Visible;
				_ui_ddlNewStorages.IsEnabled = false;
			}
			else
			{
				_ui_spAdv1.Visibility = Visibility.Collapsed;
				_ui_spAdv2.Visibility = Visibility.Collapsed;
				_ui_ddlNewStorages.IsEnabled = true;
			}
		}

		private void _ui_btnProgramsReload_Click(object sender, RoutedEventArgs e)
		{
			PresetInit();
        }
        private string FileInfoGet(File cFile)
        {
            string sAge = cFile.nAge < 0 ? "will delete at %AGE%" : (cFile.nAge > 0 ? "will move at %AGE%" : "will never delete");
            return "Info about file:\n" + "id = " + cFile.nID + "\n" + "filename = " + cFile.sFilename + "\n" + "status = " + cFile.eStatus + "\n"
                                    + "error = " + cFile.eError + "\n" + "storage = " + cFile.cStorage.sName + "\t(" + cFile.cStorage.sPath + ")\n"
                                    + "last event = " + cFile.dtLastEvent.ToString("yyyy-MM-dd HH:mm:ss") + "\t(status changed to 'in_stock')\n" + "age = " + cFile.nAge + "\t(" + sAge + ")\n";
        }
        private string FileAdditionalInfoGet(File cFile)
        {
            string sAspect = null;
            if (cFile.nAspectRatioDivd != null && cFile.nAspectRatioDivr != null)
                sAspect = cFile.nAspectRatioDivd + ":" + cFile.nAspectRatioDivr;
            return "\n" + "modification date = " + cFile.dtModification.ToString("yyyy-MM-dd HH:mm:ss") + "\t(of source file)\n" + "FPS = " + cFile.nFPS + "\n" + "source file = " + cFile.sSourceFile + "\n" + "song = " + cFile.sSong + "\n"
                                    + "series = " + cFile.sSeries + "\n" + "episode = " + cFile.sEpisode + "\n" + "custom id = " + cFile.sCustomValue + "\n"
                                    + "aspect ratio = " + sAspect + "\n" + "power gold ID = " + cFile.nPGID + "\n" + "width = " + cFile.nWidth + "\n" + "height = " + cFile.nHeight + "\n"
                                    + "frames quantity = " + cFile.nFramesQTY + "\n" + "to delete = " + cFile.bToDelete + "\n";
        }
        private string FileTextCorrect(string sText, File cFile)
        { // cFile.dtModification - date in the src file, not in DB-file for air! So we need to take last event date, wich is date of modification DB-file (+- 5 min) (only sync sets last event)
            if (null == cFile)
                return sText;
            int nAge = cFile.nAge < 0 ? -cFile.nAge : cFile.nAge;
            return sText.Replace("%AGE%", cFile.dtLastEvent.AddMonths(nAge).ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private void _ui_lbFilesChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_ui_lbFiles.SelectedItem != null)
			{
				_cSelectedFile = (File)_ui_lbFiles.SelectedItem;
				if (!_ui_tbFilename.Text.IsNullOrEmpty())
					_ui_btnReplaceMove.IsEnabled = _ui_btnReplaceCopy.IsEnabled = true;

				File cF = (File)_ui_lbFiles.SelectedItem;
                _ui_tbFileInfo.Text = FileInfoGet(cF);
                _ui_tbFileInfo.Text = FileTextCorrect(_ui_tbFileInfo.Text, cF);
                _ui_tbFileInfo.Tag = cF.nID;
				if (!_bAdditionalInfoGetting && _cRTStrings != null && _cRTAssets != null)
				{
					_bAdditionalInfoGetting = true;
					_cDBI.FileAdditionalInfoGetAsync(cF, _cRTStrings, _cRTAssets, _cRTDates);
				}
			}
			else
			{
				_cSelectedFile = null;
				_ui_btnReplaceMove.IsEnabled = false;
				_ui_btnReplaceCopy.IsEnabled = false;
				_ui_tbFileInfo.Text = "";
			}
		}
		private void _ui_btnReplace_Click(object sender, RoutedEventArgs e)
		{
			if (_nFullRightsFile == 1 && _cSelectedFile != null)
			{
				Button btnReplace = (Button)sender;
				if (((Button)sender).Name == "_ui_btnReplaceMove")
					_bMove = true;
				else
					_bMove = false;

                string sFName = sio.Path.GetFileNameWithoutExtension(((sio.FileInfo)_ui_tbFilename.Tag).FullName).ToLower();
                if (sio.Path.GetFileNameWithoutExtension(_cSelectedFile.sFilename).ToLower() != sFName && sio.Path.GetFileNameWithoutExtension(_cSelectedFile.sSourceFile).ToLower() != sFName)
                {
                    _cMsgBox.Closed += _cMsgBox_Closed2;
                    _cMsgBox.Show(g.Replica.sErrorIngest6.Fmt(Environment.NewLine), g.Common.sAttention, MsgBox.MsgBoxButton.OKCancel);
                }
                else
                {
                    _dlgProgress.Show();
                    _cDBI.FileCheckIsInPlaylistAsync(_cSelectedFile.nID, 100);
                }
            }
		}
        private void _cMsgBox_Closed2(object sender, EventArgs e)
        {
            _cMsgBox.Closed -= _cMsgBox_Closed2;
            if ((_cMsgBox).enMsgResult== MsgBox.MsgBoxButton.OK)
            {
                _dlgProgress.Show();
                _cDBI.FileCheckIsInPlaylistAsync(_cSelectedFile.nID, 100);
            }
        }

        private void _ui_cmFiles_Opened(object sender, RoutedEventArgs e)
		{
			_cSelectedFile = (File)_ui_lbFiles.SelectedItem;
			if (null != _cSelectedFile)
			{
				_ui_cmFileDelete.Header = g.Common.sDelete + ": " + _cSelectedFile.sFilename;
			}
			switch (_nFullRightsFile)
			{
				case -1:
					_ui_cmFileDelete.IsEnabled = false;
					break;
				case 0:
					_ui_cmFileDelete.IsEnabled = false;
					break;
				case 1:
					_ui_cmFileDelete.IsEnabled = false; // пока тут удаления не будет
					break;
				default:
					break;
			}
		}
		private void _ui_cmFiles_Refresh(object sender, RoutedEventArgs e)
		{
			Tab cTab = _ui_tcPresets.TabGet();
			if (cTab.cData != null)
			{
				_dlgProgress.Show();
				_cDBI.FilesWithSourcesGetAsync(cTab.cData.cStorage.nID);
			}
		}
		private void _ui_cmFiles_Delete(object sender, RoutedEventArgs e)
		{
			if (null == _cSelectedPerson)
				_cMsgBox.Show(g.Common.sNoItemsSelected);
			else
			{
				_dlgProgress.Show();
				_dlgProgress.Close();   // пока без удаления - это будет в другом месте - для медиапланера, но если что, то всё есть  _cDBI.public void FilesRemove(long[] aFileIDs)
										//_ui_lbClipArtistSelectedRemove(_cSelectedPerson);
										//_cDBI.PersonsRemoveAsync(new Person[] { _cSelectedPerson });
			}
		}

		private void rpAdvancedOpenedClosed(object sender, EventArgs e)
		{
			if (_ui_tcPresets == null)
				return;
			ReducePanel cRP = (ReducePanel)sender;
			Tab cTab = _ui_tcPresets.TabGet();

            if (cRP.IsOpen)
            {
                _ui_btnSave.IsEnabled = false;
                _ui_btnMove.IsEnabled = false;
            }
            else
            {
                _ui_btnMove.IsEnabled = _ui_btnSave.IsEnabled = (null == _ui_txtTargetName.Tag);
            }

            if (cRP.IsOpen && _aFiles == null && cTab != null && cTab.cData != null)
			{
				_dlgProgress.Show();
				_cDBI.RegisteredTablesGetAsync();
				_ui_scFilesSearch.ItemAdd = null;
				_ui_scFilesSearch.sCaption = "";
				_ui_scFilesSearch.nGap2nd = 0;
				_ui_scFilesSearch.AddButtonWidth = 0;
				_ui_scFilesSearch.sDisplayMemberPath = "sFilename";
				_ui_scFilesSearch.aAdditionalSearchFields = new string[2] { "nID", "sSourceFile" };
				_ui_scFilesSearch.DataContext = _ui_lbFiles;
				_ui_scFilesSearch.nMaxItemsInListOrTable = 200;
			}
		}
        #endregion
        #region DBI
        private void _cDBI_FilesAgeGetCompleted(object sender, FilesAgeGetCompletedEventArgs e)
        {
            if (e.Result != null)
                AgeSet(e.Result.Value);
            else
                AgeSet(_nDefaultAgeProgram);
            TargetNameMake();
        }
        void _cDBI_TransliterationGetCompleted(object sender, TransliterationGetCompletedEventArgs e)
        {
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

			if (null != e.Result && 0 < e.Result.Length)
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
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

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
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

			if (1 > e.Result)
                _cMsgBox.ShowError();
            _cDBI.ArtistsGetAsync(e.UserState);
        }
        void _cDBI_IngestCompleted(object sender, IngestCompletedEventArgs e)
        {
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

			FileTask cFT = (FileTask)e.UserState;
			if (null == e.Result)
			{
				ListBox cLB = new ListBox();
				cLB.ItemsSource = new string[] { cFT.cInfo.sFilename };
				ShowError(" " + g.Common.sErrorDataSave.ToLower() + ": ", false, cLB);
				((FileTask)e.UserState).cUI.ShowError();
				_cDBI.ErrorLastGetAsync(g.Common.sErrorDataSave.ToLower() + ": " + cFT.cInfo.sFilename);
				if (cFT.nIndx >= 0)
				{
					_ui_tbAdvertisementLog.Text += "\nError creating file passport and asset: "+ cFT.cInfo.sOriginalFile;
					_ui_btnSave_BatchClick_Do();
				}
			}
			else
			{
				cFT.RestoreFilename();
				if (cFT.nIndx >= 0)
					_ui_btnSave_BatchClick_Do();
			}

        }
        private void _cDBI_IngestForReplacedFileCompleted(object sender, IngestForReplacedFileCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _dlgProgress.Close();
                _cMsgBox.ShowError(e.Error);
                return;
            }
            FileTask cFT = (FileTask)e.UserState;
            cFT.RestoreFilename();
            if (_cSelectedFile.nID == cFT.cSelectedFile.nID)
            {
                _cSelectedFile = cFT.cSelectedFile;
                _ui_tbFileInfo.Text = FileInfoGet(_cSelectedFile) + FileAdditionalInfoGet(_cSelectedFile);
                _ui_tbFileInfo.Text = FileTextCorrect(_ui_tbFileInfo.Text, _cSelectedFile);
            }
        }
        private void _cDBI_ErrorLastGetCompleted(object sender, ErrorLastGetCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

			_cMsgBox.Closed += _cMsgBox_Closed1;
			if (null != e.Result)
				_cMsgBox.ShowError((string)e.UserState + "\n" + e.Result.sMessage);
		}
		void _cDBI_PersonsRemoveCompleted(object sender, PersonsRemoveCompletedEventArgs e)
        {
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

			_cDBI.ArtistsGetAsync();
        }
        void _cDBI_ProgramsGetCompleted(object sender, ProgramsGetCompletedEventArgs e)
        {
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}
            _bNameMakeBlock = true;
			Program[] aSeries;
            if (null != e.Result && 0 < (aSeries = e.Result.Where(o => null != o.cType && AssetType.series == o.cType.eType && 0 < e.Result.Count(o1 => o.nID == o1.nIDParent)).OrderBy(o => o.sName).ToArray()).Length)
            {//aSeries has only series with episodes
                _ui_ddlProgramEpisodes.Tag = e.Result.Where(o => 0 < aSeries.Count(o1 => o.nIDParent == o1.nID)).ToArray(); //only episodes
				_ui_ddlProgramParts.Tag = e.Result.Where(o => 0 < ((Program[])_ui_ddlProgramEpisodes.Tag).Count(o1 => o.nIDParent == o1.nID)).ToArray(); //only parts
				_ui_ddlProgramSeries.ItemsSource = aSeries;
				_ui_ddlProgramEpisodes.ItemsSource = null;
				_ui_ddlProgramParts.ItemsSource = null;
			}
            else
			{
				_ui_ddlProgramEpisodes.Tag = null;
				_ui_ddlProgramParts.Tag = null;
				_ui_ddlProgramSeries.ItemsSource = _ui_ddlProgramEpisodes.ItemsSource = _ui_ddlProgramParts.ItemsSource = new Program[] { new Program() { nID = -1, sName = g.Common.sMissing1.ToLower() } };
                _ui_ddlProgramEpisodes.SelectedIndex = 0;
				_ui_ddlProgramParts.SelectedIndex = 0;
			}

			Tab cTab = _ui_tcPresets.TabGet();
			if (null != cTab.cData.cStorage && cTab.cData.cStorage.sName == "новости" && null != _ui_ddlProgramSeries.ItemsSource)
			{
				Program[] aPs = (Program[])_ui_ddlProgramSeries.ItemsSource;
				Program cP = (aPs).FirstOrDefault(o => o.sName.ToLower() == "новости");
				if (null != cP)
					_ui_ddlProgramSeries.SelectedItem = cP;
				else
					_ui_ddlProgramSeries.SelectedIndex = 0;
			}
			else
				_ui_ddlProgramSeries.SelectedIndex = 0;
			_ui_tcPresets.TabGet().bInited = true;

            _bNameMakeBlock = false;
            PresetDefaults();

			_dlgProgress.Close();
		}
		void _cDBI_StoragesGetCompleted(object sender, StoragesGetCompletedEventArgs e)
        {
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

			try
			{
                if (null != e.Error)
                    throw e.Error;
				_ui_ddlNewStorages.ItemsSource = e.Result;
				_ui_ddlStorageAdverts.ItemsSource = e.Result;
				_ui_ddlStorageTrailers.ItemsSource = e.Result;
			}
            catch (Exception ex)
            {
                (new MsgBox()).ShowError(ex);
            }
            _dlgProgress.Close();
        }
		private void _cDBI_IsThereSameFileCompleted(object sender, IsThereSameFileCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

			if (e.Result && (MessageBoxResult.Cancel == MessageBox.Show(g.Replica.sWarningIngest1, g.Common.sAttention, MessageBoxButton.OKCancel)))
			{
				_dlgProgress.Close();
				return;
            }
			Tab cTab = _ui_tcPresets.TabGet();
			if (cTab.cData.eType == Preferences.Ingest.Tab.Type.advertisement)
				_cDBI.IsThereSameCustomValueAsync("vi_id", _ui_tbAdvertisementID.Text);
			else if (cTab.cData.eType == Preferences.Ingest.Tab.Type.clip)
				_cDBI.IsThereSameCustomValueAsync("id", _ui_tbClipPGID.Text);
			else
			{
				_dlgProgress.Close();
				_ui_btnSave_Click_Do();
            }
        }
		private void _cDBI_IsThereSameCustomValueCompleted(object sender, IsThereSameCustomValueCompletedEventArgs e)
		{
			_dlgProgress.Close();
			if (e.Error != null)
			{
				_cMsgBox.ShowError(e.Error);
				return;
			}

			if (null != e.Result && (_ui_cbNoAsset.IsChecked == null || false == _ui_cbNoAsset.IsChecked.Value)) //  в вопросе нет смысла похоже...   && (MessageBoxResult.Cancel == MessageBox.Show(g.Replica.sWarningIngest2.Fmt(Environment.NewLine, "[asset_id=" + e.Result.nID + "][name=" + e.Result.sName + "]", (null == e.Result.cFile ? " [no file]" : " [file_id=" + e.Result.cFile.nID + "][name=" + e.Result.cFile.sFilename + "]")), g.Common.sAttention, MessageBoxButton.OKCancel))
			{
				MessageBox.Show(g.Replica.sWarningIngest2.Fmt(Environment.NewLine, "[asset_id=" + e.Result.nID + "][name=" + e.Result.sName + "]", (null == e.Result.cFile ? " [no file]" : " [file_id=" + e.Result.cFile.nID + "][name=" + e.Result.cFile.sFilename + "]")), g.Common.sAttention, MessageBoxButton.OK);
				return;
			}
			_ui_btnSave_Click_Do();
		}
		private void _cDBI_AreThereSameFilesCompleted(object sender, AreThereSameFilesCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

			if (!e.Result.IsNullOrEmpty())
			{
				string sLog = "";
				foreach(string sFN in e.Result)
				{
					BatchItem cBI = _aBatch.FirstOrDefault(o => o.sTargetFilename == sFN);
					if (null != cBI)
					{
						sLog += "\n\t"+ cBI.sSourceFilename + "\t" + sFN;
						_aBatch.Remove(cBI);
						//cBI.sError = "file was found in HD: " + sFN;
						//_aBatchErrors.Add(cBI);
					}
				}
				if (!sLog.IsNullOrEmpty())
					_ui_tbAdvertisementLog.Text += "\nFiles was found in HD: (will not process!) ("+ _aBatch.Count + " to process)" + sLog + "\n";
			}
			_cDBI.IsThereSameCustomValuesAsync("vi_id", _aBatch.Select(o => o.cTSR.sS_Code).ToArray());
		}
		private void _cDBI_IsThereSameCustomValuesCompleted(object sender, IsThereSameCustomValuesCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}

			if (!e.Result.IsNullOrEmpty())
			{
				string sLog = "";
				foreach (Asset cAsset in e.Result)
				{
					BatchItem cBI = _aBatch.FirstOrDefault(o => o.cTSR.sS_Code == cAsset.aCustomValues[0].sValue);
					if (null != cBI)
					{
						sLog += "\n\t" + cBI.sSourceFilename + "\t" + cBI.cTSR.sS_Code + "\t[found asset= " + cAsset.sName + "]\t[id asset= " + cAsset.nID + "]";
						_aBatch.Remove(cBI);
						//cBI.sError = "s-code was found in HD: " + cBI.cTSR.sS_Code + "\t[found in asset= " + cAsset.sName + "]\t[id asset= " + cAsset.nID + "]";
						//_aBatchErrors.Add(cBI);
					}
				}
				if (!sLog.IsNullOrEmpty())
					_ui_tbAdvertisementLog.Text += "\nS-codes of these Files was found in HD: (will not process!) (" + _aBatch.Count + " to process)" + sLog;
			}
			_nBatchIndex = 0;
			_ui_btnSave_BatchClick_Do();
		}
		private void _cDBI_TSRItemsGetCompleted(object sender, TSRItemsGetCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}
			if (!e.Result.IsNullOrEmpty())
			{
				List<BatchItem> aNotFound = _aBatch.ToList();
				BatchItem cBItem;
				foreach (TSRItem cTI in e.Result)
				{
					cBItem = _aBatch.FirstOrDefault(o => o.sSourceFilename == cTI.oTag.ToString());
					if (cBItem != null)
					{
						aNotFound.Remove(cBItem);
						cBItem.cTSR = cTI;
					}
				}
				_aBatch.RemoveRange(aNotFound);
				string sLog = aNotFound.Select(o =>o.sSourceFilename).ToEnumerationString("\n\t", "", null, null, false);
				if (!sLog.IsNullOrEmpty())
					_ui_tbAdvertisementLog.Text = "Files not found in TSR base:\n" + "\t" + sLog;

				aNotFound = _aBatch.Where(o => o.cTSR.eType == TSRType.NULL || o.cTSR.eBlock == Block.NULL).ToList();
				if (!aNotFound.IsNullOrEmpty())
				{
					_aBatch.RemoveRange(aNotFound);
					_ui_tbAdvertisementLog.Text += "\nFiles with NULL block or type in TSR: \n" + aNotFound.Select(o => "\t" + o.cTSR.sS_Code + "\t" + o.cTSR.eBlock + "\t" + o.cTSR.eType + "\t" + o.sSourceFilename).ToEnumerationString("\n", "", null, null, false);
				}

				_ui_tbAdvertisementLog.Text += "\nFiles to process: \n" + _aBatch.Select(o => "\t" + o.cTSR.sS_Code + "\t" + o.cTSR.eBlock + "\t" + o.cTSR.eType + "\t" + o.sSourceFilename).ToEnumerationString("\n", "", null, null, false);

				string sDir = ((sio.FileInfo)_ui_tbFilename.Tag).DirectoryName;
				foreach (BatchItem cBI in _aBatch)
				{
					_ui_txtTargetName.Text = "";
					_ui_tbFilename.Text = cBI.sSourceFilename;
					_ui_tbFilename.Tag = new sio.FileInfo(sio.Path.Combine(sDir, cBI.sSourceFilename));
					_ui_tbAdvertisementID.Text =cBI.cTSR.sS_Code;
					TargetNameMake(); 
					if (!_ui_txtTargetName.Text.IsNullOrEmpty())
					{
						cBI.cIngest = _ui_tcPresets.TabGet().cIngestInfo;
						cBI.sTargetFilename = sio.Path.GetFileName(_ui_txtTargetName.Text);
					}
				}
			}
			else
				_ui_tbAdvertisementLog.Text = "error getting TSR information!";
			_dlgProgress.Close();
		}
		private void _cDBI_FileAdditionalInfoGetCompleted(object sender, FileAdditionalInfoGetCompletedEventArgs e)
        {
            if (e != null && e.Error != null)
            {
                _dlgProgress.Close();
                _cMsgBox.ShowError(e.Error);
				return;
			}
			_bAdditionalInfoGetting = false;
			if (null != e.Result && null != _ui_tbFileInfo.Tag && e.Result.nID == _ui_tbFileInfo.Tag.ToLong())
			{
                _ui_tbFileInfo.Text += FileAdditionalInfoGet(e.Result);
                if (null != _cSelectedFile)
                {
                    _cSelectedFile.nFPS = e.Result.nFPS;
                    _cSelectedFile.sSourceFile = e.Result.sSourceFile;
                    _cSelectedFile.sSong = e.Result.sSong;
                    _cSelectedFile.sSeries = e.Result.sSeries;
                    _cSelectedFile.sEpisode = e.Result.sEpisode;
                    _cSelectedFile.sCustomValue = e.Result.sCustomValue;
                    _cSelectedFile.nAspectRatioDivd = e.Result.nAspectRatioDivd;
                    _cSelectedFile.nAspectRatioDivr = e.Result.nAspectRatioDivr;
                    _cSelectedFile.nPGID = e.Result.nPGID;
                    _cSelectedFile.nWidth = e.Result.nWidth;
                    _cSelectedFile.nHeight = e.Result.nHeight;
                    _cSelectedFile.nFramesQTY = e.Result.nFramesQTY;
                    _cSelectedFile.bToDelete = e.Result.bToDelete;
                    _cSelectedFile.dtModification = e.Result.dtModification;
                }
            }
		}
		private void _cDBI_FileCheckIsInPLCompleted(object sender, FileCheckIsInPlaylistCompletedEventArgs e)
		{
			_dlgProgress.Close();
			if (e.Error != null)
			{
				_cMsgBox.ShowError(e.Error);
				return;
			}
            switch (e.Result)
            {
                case FileIsInPlaylist.IsNot:
                    _ui_btnReplace_Click_Do();
                    break;
                case FileIsInPlaylist.YesItIs:
                    _cMsgBox.Closed += _cMsgBox_Closed4;
                    _cMsgBox.Show(g.Replica.sWarningIngest3.Fmt(Environment.NewLine), g.Common.sAttention, MsgBox.MsgBoxButton.OKCancel);
                    break;
                case FileIsInPlaylist.IsInFragment:
                    _cMsgBox.Closed += _cMsgBox_Closed3;
                    _cMsgBox.Show(g.Replica.sErrorIngest5.Fmt(Environment.NewLine), g.Common.sAttention, MsgBox.MsgBoxButton.OKCancel);
                    break;
                default:
                    break;
            }
		}

        private void _cMsgBox_Closed4(object sender, EventArgs e)
        {
            _cMsgBox.Closed -= _cMsgBox_Closed4;
            if ((_cMsgBox).enMsgResult == MsgBox.MsgBoxButton.OK)
            {
                _ui_btnReplace_Click_Do();
            }
        }

        private void _cMsgBox_Closed3(object sender, EventArgs e)
        {
            _cMsgBox.Closed -= _cMsgBox_Closed3;
            if ((_cMsgBox).enMsgResult == MsgBox.MsgBoxButton.OK)
            {
                _cMsgBox.Closed += _cMsgBox_Closed5;
                _cMsgBox.Show(g.Common.sAreYouSureStrong, g.Common.sAttention, MsgBox.MsgBoxButton.OKCancel);
            }
        }

        private void _cMsgBox_Closed5(object sender, EventArgs e)
        {
            _cMsgBox.Closed -= _cMsgBox_Closed5;
            if ((_cMsgBox).enMsgResult == MsgBox.MsgBoxButton.OK)
            {
                _ui_btnReplace_Click_Do();
            }
        }

        private void _cDBI_RegisteredTablesGetCompleted(object sender, RegisteredTablesGetCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}
			if (!e.Result.IsNullOrEmpty())
			{
				_aRegisteredTables = e.Result;
				_cRTAssets = _aRegisteredTables.FirstOrDefault(o => o.sSchema == "mam" && o.sName == "tAssets");
				_cRTStrings = _aRegisteredTables.FirstOrDefault(o => o.sSchema == "media" && o.sName == "tStrings");
                _cRTDates = _aRegisteredTables.FirstOrDefault(o => o.sSchema == "media" && o.sName == "tDates");
            }
			Tab cTab = _ui_tcPresets.TabGet();
			_cDBI.FilesWithSourcesGetAsync(cTab.cData.cStorage.nID);
		}

        private void _cDBI_FilesWithSourcesGetCompleted(object sender, FilesWithSourcesGetCompletedEventArgs e)
        {
            if (e.Error != null)
			{
				_dlgProgress.Close();
				_cMsgBox.ShowError(e.Error);
				return;
			}
			if (null != e.Result)
			{
                long nSelectedID = _cSelectedFile == null ? -1 : _cSelectedFile.nID;
				_ui_lbFiles.Tag = e.Result;
				_ui_lbFiles.ItemsSource = e.Result;
				_ui_scFilesSearch.DataContextUpdateInitial();
				//_ui_scFilesSearch.Search();
				_ui_lbFiles.UpdateLayout();

                if (nSelectedID > 0)
                {
                    _cSelectedFile = e.Result.FirstOrDefault(o => o.nID == nSelectedID);
                    if (null != _cSelectedFile)
                    {
                        _ui_lbFiles.ScrollIntoView(_cSelectedFile);
                        _ui_lbFiles.SelectedItem = _cSelectedFile;
                    }
                }
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
            PresetDefaults(true);
        }
        private void AgeSet(int nAge)
        {
            _ui_ddlAction.SelectedIndex = nAge >= 0 ? 0 : 1;
            _ui_ddlAge.SelectedIndex = 1;
            _ui_nudAge.Value = Math.Abs(nAge);
        }
        private void PresetDefaults(bool bTotlalRefresh)
		{
            _bNameMakeBlock = true;
            Tab cTab = _ui_tcPresets.TabGet();
			swc.Grid ui_g = _ahGrids[_ui_tcPresets.TabGet().cData.eType];
			if (null != ui_g.Parent)
				((TabItem)ui_g.Parent).Content = null;
			((TabItem)_ui_tcPresets.SelectedItem).Content = ui_g;
			if (!cTab.bInited)
            {
                PresetInit();
                if (!cTab.bInited)
                {
                    _bNameMakeBlock = false;
                    return;
                }
            }
			_ui_btnSave.IsEnabled = false;
			_ui_btnMove.IsEnabled = false;
			_ui_btnReplaceCopy.IsEnabled = false;
			_ui_btnReplaceMove.IsEnabled = false;
            if (bTotlalRefresh)
            {
                _ui_rpAdvanced.IsOpen = false;
                _ui_scFilesSearch.DataContextUpdateInitial();
            }
			_ui_tbFileInfo.Text = "";
			switch (cTab.cData.eType)
            {
				case Preferences.Ingest.Tab.Type.clip:
                    AgeSet(_nDefaultAgeClip);

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
					_ui_tbClipPGID.Text = "";
					break;
				case Preferences.Ingest.Tab.Type.advertisement:
					_ui_tbAdvertisementCompany.Text = "VI";
                    AgeSet(_nDefaultAgeAdverts);

					_ui_tbAdvertisementID.Text = "";
					if (cTab.cData.bBatch)
					{
						_ui_tbAdvertisementLogName.Visibility = Visibility.Visible;
						_ui_svAdvertisementLog.Visibility = Visibility.Visible;
						_ui_tbAdvertisementID.IsEnabled = false;
					}
					else
					{
						_ui_tbAdvertisementLogName.Visibility = Visibility.Collapsed;
						_ui_svAdvertisementLog.Visibility = Visibility.Collapsed;
						_ui_tbAdvertisementID.IsEnabled = true;
					}
					break;
				case Preferences.Ingest.Tab.Type.program:
					if (null != cTab.cData.cStorage && cTab.cData.cStorage.sName == "новости")
					{
                        AgeSet(_nDefaultAgeProgramNews);
						_ui_cbNoAsset.IsChecked = true;
					}
					else
					{
                        AgeSet(_nDefaultAgeProgram);
                        _ui_cbNoAsset.IsChecked = false;
					}
					_ui_tbAdvertisementID.Text = "";
					_ui_tbProgramPart.Text = "" + 1;
					break;
				case Preferences.Ingest.Tab.Type.design:
                    AgeSet(_nDefaultAgeDesign);

                    if (bTotlalRefresh)
                        _ui_ddlDesignSeason.SelectedIndex = _ui_ddlDesignType.SelectedIndex = _ui_ddlDesignDTMF.SelectedIndex = 0;
                    _ui_tbDesignTags.Text = _ui_tbDesignName.Text = "";
                    break;
            }
            _bNameMakeBlock = false;
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
            if (null == _ui_txtTargetName || null == _ui_tcPresets.SelectedItem || _bNameMakeBlock)
                return;
            Tab cTab = _ui_tcPresets.TabGet();
            string sDelimeter = "__", sPath = "???", sExtention = "", sPartCommon, sPartItem = "", sPartVersion = "", sValue;
            _ui_txtTargetName.Tag = null;
			int nTryID;

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

						if (_ui_ddlClipQuality.SelectedItem != null)
							sPartItem += "q" + ((IdNamePair)_ui_ddlClipQuality.SelectedItem).nID;
						if (_ui_ddlClipShow.SelectedItem != null)
							sPartItem += "s" + ((IdNamePair)_ui_ddlClipShow.SelectedItem).nID;
						if (_ui_ddlClipRemix.SelectedItem != null)
							sPartItem += "r" + ((IdNamePair)_ui_ddlClipRemix.SelectedItem).nID;
						if (_ui_ddlClipPromo.SelectedItem != null)
							sPartItem += "p" + ((IdNamePair)_ui_ddlClipPromo.SelectedItem).nID;
						if (_ui_ddlClipCut.SelectedItem != null)
							sPartItem += "c" + ((IdNamePair)_ui_ddlClipCut.SelectedItem).nID;
						if (_ui_ddlClipForeign.SelectedItem != null)
							sPartItem += "f" + ((IdNamePair)_ui_ddlClipForeign.SelectedItem).nID;
						cTab.cIngestInfo = new IngestClip()
						{
							aArtists = ((IEnumerable<Person>)_ui_lbClipArtistsSelected.ItemsSource).ToArray(),
							sSongName = _ui_tbClipSong.Text,
							nQuality = (byte)(_ui_ddlClipQuality.SelectedItem == null ? 0 : ((IdNamePair)_ui_ddlClipQuality.SelectedItem).nID),
							bLocation = (0 < _ui_ddlClipShow.SelectedIndex),
							bRemix = (0 < _ui_ddlClipRemix.SelectedIndex),
							bPromo = (0 < _ui_ddlClipPromo.SelectedIndex),
							bCutted = (0 < _ui_ddlClipCut.SelectedIndex),
							bForeign = (0 < _ui_ddlClipForeign.SelectedIndex),
                            nPG_ID = (_ui_tbClipPGID.Text == null || !int.TryParse(_ui_tbClipPGID.Text, out nTryID) ? -1 : int.Parse(_ui_tbClipPGID.Text)),
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
						if (!_ui_tbFilename.Text.IsNullOrEmpty())
							sPartItem += sDelimeter + Transliterate(_ui_tbFilename.Text.ToLower());

						cTab.cIngestInfo = cIngestAdvertisement;
                    break;
					case Preferences.Ingest.Tab.Type.program:
                        _ui_txtTargetName.Tag = false;
                        sPartItem = "???" + sDelimeter + "???";
                        if (null != _ui_ddlProgramEpisodes.Tag)
                        {
                            Program cSeries = (Program)_ui_ddlProgramSeries.SelectedItem;
							if (null != cSeries && -1 < cSeries.nID)
							{
								string sSeriesName = cSeries.sName.Trim().ToLower();
                                sPartItem = Transliterate(sSeriesName) + sDelimeter;
                                Program cEpisode = (Program)_ui_ddlProgramEpisodes.SelectedItem;
                                if (null == cEpisode || cSeries.nID != cEpisode.nIDParent)
                                {
                                    _ui_ddlProgramEpisodes.ItemsSource = ((Program[])_ui_ddlProgramEpisodes.Tag).Where(o => cSeries.nID == o.nIDParent).ToArray();
                                    _ui_ddlProgramEpisodes.SelectedIndex = 0;
                                    cEpisode = (Program)_ui_ddlProgramEpisodes.SelectedItem;

								}
                                sValue = cEpisode.sName.Trim().ToLower();
                                if (sValue.StartsWith(sSeriesName))
                                    sValue = sValue.Remove(sSeriesName).Trim(' ', ':'); //episodes names contain a series name by default. if so we will skip it 
                                sPartItem += Transliterate(sValue);
                                _ui_txtTargetName.Tag = null;

								Program cPart = (Program)_ui_ddlProgramParts.SelectedItem;
								if (null == cPart || cEpisode.nID != cPart.nIDParent)
								{
									Program[] aTMP = _ui_ddlProgramParts.Tag == null ? null : ((Program[])_ui_ddlProgramParts.Tag).Where(o => cEpisode.nID == o.nIDParent).ToArray();
									int nCount;
									if (aTMP != null)
									{
										_ui_ddlProgramParts.ItemsSource = aTMP;
										nCount = ((Program[])_ui_ddlProgramParts.ItemsSource).Length;
									}
									else
										nCount = 0;

									if (nCount > 0)
										_ui_ddlProgramParts.SelectedIndex = 0;

									if (null == _ui_tbProgramPart.Tag)
									{
										_ui_tbProgramPart.Text = "" + (_ui_ddlProgramParts.ItemsSource == null ? 0 : nCount + 1);
										_ui_tbProgramPart.Tag = new object();
									}

								}
							}
                            else
                            {
                                _ui_ddlProgramEpisodes.ItemsSource = (Program[])_ui_ddlProgramSeries.Tag;
                                _ui_ddlProgramEpisodes.SelectedIndex = 0;
								_ui_ddlProgramParts.ItemsSource = (Program[])_ui_ddlProgramParts.Tag;
								_ui_ddlProgramParts.SelectedIndex = 0;
							}
						}
                        else
                            _ui_ddlProgramSeries.SelectedIndex = _ui_ddlProgramEpisodes.SelectedIndex = _ui_ddlProgramParts.SelectedIndex = 0;

                        if (!_ui_tbProgramPart.Text.IsNullOrEmpty())
                            sPartItem += sDelimeter + _ui_tbProgramPart.Text;

                        cTab.cIngestInfo = new IngestProgram()
                        {
                            cSeries = (Asset)_ui_ddlProgramSeries.SelectedItem,
                            cEpisode = (Asset)_ui_ddlProgramEpisodes.SelectedItem,
							sPart = _ui_tbProgramPart.Text
						};
                        cTab.cIngestInfo.aClasses = ((Program)_ui_ddlProgramSeries.SelectedItem).aClasses;
                        bool bIsChacked = null == _ui_cbNoAsset.IsChecked ? false : _ui_cbNoAsset.IsChecked.Value;
                        _ui_cbNoAsset.IsChecked = null;
                        _ui_cbNoAsset.IsChecked = bIsChacked;
                        break;
					case Preferences.Ingest.Tab.Type.design:
						if (0 < _ui_ddlDesignSeason.SelectedIndex)
                            sPartItem += Transliterate((string)((ComboBoxItem)_ui_ddlDesignSeason.SelectedItem).Content) + sDelimeter;

                        if (1 > _ui_ddlDesignType.SelectedIndex)
                        {
                            sValue = "???";
                            _ui_txtTargetName.Tag = false;
                        }
                        else
							sValue = Transliterate((string)((ComboBoxItem)_ui_ddlDesignType.SelectedItem).Content);
						sPartItem += sValue + sDelimeter;

                        sValue = Transliterate(_ui_tbDesignTags.Text.ToLower().Trim());
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

				if (false) // делает синнкер, люди ошибаются
				{
					IdNamePair cINP = (IdNamePair)_ui_ddlFormat.SelectedItem;
					sPartCommon = Math.Abs(cINP.nID) + (0 > cINP.nID ? "p" : "i");
					sPartCommon += ((IdNamePair)_ui_ddlFPS.SelectedItem).nID;
				}
				sPartCommon = "b" + _ui_ddlAir.SelectedIndex;

				cTab.cIngestInfo.nFormat = (int)((IdNamePair)_ui_ddlFormat.SelectedItem).nID;
				cTab.cIngestInfo.nFPS = (byte)((IdNamePair)_ui_ddlFPS.SelectedItem).nID;
				cTab.cIngestInfo.bBroadcast = (0 < _ui_ddlAir.SelectedIndex);

				sPartCommon += "a";
                if (0 < _ui_ddlAge.SelectedIndex && _ui_nudAge.Value > 0)
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


				if (0 < _ui_tbFilename.Text.Length && null != _ui_tbFilename.Tag)
				{
					sExtention += sio.Path.GetExtension(_ui_tbFilename.Text).ToLower();
					cTab.cIngestInfo.sOriginalFile = ((sio.FileInfo)_ui_tbFilename.Tag).FullName;
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
            if (_ui_rpAdvanced.IsOpen)
                _ui_btnMove.IsEnabled = _ui_btnSave.IsEnabled = false;
            else
                _ui_btnMove.IsEnabled = _ui_btnSave.IsEnabled = (null == _ui_txtTargetName.Tag);

            if (cTab.cData.bBatch)
            {
                _ui_btnSave.IsEnabled = false;
			}
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
			{
				_cMsgBox.ShowAttention(sText, cLB);
				_cMsgBox.Closed += _cMsgBox_Closed1;
			}
		}
		private void _cMsgBox_Closed1(object sender, EventArgs e)
		{
			_cMsgBox.Closed -= _cMsgBox_Closed1;
			//_dlgProgress.Show();
			//_dlgProgress.Close();
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
		private void cbNoAssetChecked(object sender, RoutedEventArgs e)
		{
			Tab cTab = _ui_tcPresets.TabGet();
            if (null == cTab.cIngestInfo)
                return;
            if (null != _ui_cbNoAsset.IsChecked && _ui_cbNoAsset.IsChecked.Value)
			{
				cTab.cIngestInfo.bCreateAsset = false;
			}
			else
			{
				cTab.cIngestInfo.bCreateAsset = true;
			}
		}



	}
}
