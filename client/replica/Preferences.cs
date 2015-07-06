using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using helpers.replica.services.dbinteract;
using g = globalization;
using p = replica.sl.services.preferences;

namespace replica.sl
{
    public class Preferences
    {
        public class Ingest
        {
            public class Tab
            {
				public enum Type
				{
					clip = 0,
					advertisement = 1,
					program = 2,
					design = 3
				}
				public Type eType;
				public Storage cStorage;
				public string sCaption;
                public string sPath;
            }
            static public Tab[] aTabs
            {
                get
                {
                    return _cInstance._cIngest._aTabs;
                }
            }

            static public void Save()
            {
                lock (_cInstance)
                {
                    IsolatedStorageSettings.ApplicationSettings["ingest"] = _cInstance._cIngest._aTabs;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
            static public Ingest Load()
            {
                Ingest cRetVal = new Ingest();
                //IsolatedStorageSettings.ApplicationSettings.Remove("ingest");
                if (IsolatedStorageSettings.ApplicationSettings.Contains("ingest"))
                    cRetVal._aTabs = (Tab[])IsolatedStorageSettings.ApplicationSettings["ingest"];
                return cRetVal;
            }
            static public void Reset()
            {
                _cInstance._cIngest = new Ingest() { _aTabs = new Tab[0] };
                Save();
            }
            static public void TabAdd(Tab cTab)
            {
                _cInstance._cIngest._aTabs = _cInstance._cIngest._aTabs.Concat(new[] { cTab }).ToArray();
                Save();
            }
            static public void TabRemove(Tab cTab)
            {
                _cInstance._cIngest._aTabs = _cInstance._cIngest._aTabs.Where(o => cTab != o).ToArray();
                Save();
            }

            private Tab[] _aTabs;
            public Ingest()
            {
                _aTabs = new Tab[0];
            }
        }

        static private Preferences _cInstance;

        static public p.Preferences cServer
        {
            get
            {
                return _cInstance._cServer;
            }
        }
        static public string sUser
        {
            get
            {
                return _cInstance._sUser;
            }
            set
            {
                _cInstance._sUser = value;
                lock (_cInstance)
                {
                    IsolatedStorageSettings.ApplicationSettings["user"] = _cInstance._sUser;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }
        static public string sPassword
        {
            get
            {
                return _cInstance._sPassword;
            }
            set
            {
                _cInstance._sPassword = value;
                lock (_cInstance)
                {
                    IsolatedStorageSettings.ApplicationSettings["password"] = _cInstance._sPassword;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        static public void ServerLoad()
        {
            p.PreferencesSoapClient cServerSoap = new p.PreferencesSoapClient();
            cServerSoap.ReplicaGetCompleted += _cInstance.cServerSoap_ReplicaGetCompleted;
            cServerSoap.ReplicaGetAsync();
        }

        static Preferences()
        {
            _cInstance = new Preferences();
            _cInstance._cServer = null;
            _cInstance._sUser = _cInstance._sPassword = null;
            if (IsolatedStorageSettings.ApplicationSettings.Contains("user"))
                _cInstance._sUser = (string)IsolatedStorageSettings.ApplicationSettings["user"];
            if (IsolatedStorageSettings.ApplicationSettings.Contains("password"))
                _cInstance._sPassword = (string)IsolatedStorageSettings.ApplicationSettings["password"];
            _cInstance._cIngest = Ingest.Load();
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        private p.Preferences _cServer;
        private Ingest _cIngest;
        private string _sUser;
        private string _sPassword;

        private void cServerSoap_ReplicaGetCompleted(object sender, p.ReplicaGetCompletedEventArgs e)
        {
            try
            {
                _cServer = e.Result;
                if (null != _cServer.sLocale)
                {
                    try
                    {
                        g.Replica.Culture = new System.Globalization.CultureInfo(_cServer.sLocale);
                        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(_cServer.sLocale);
                        System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(_cServer.sLocale);
                    }
                    catch
                    {
                        MessageBox.Show("culture set error:" + _cServer.sLocale);
                    }
                }
            }
            catch { }
            MainPage cMainPage = (MainPage)Application.Current.RootVisual;
            cMainPage.Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
            cMainPage.Auth();
        }
    }
}
