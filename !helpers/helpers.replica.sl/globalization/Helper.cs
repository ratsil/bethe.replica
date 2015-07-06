using g = globalization;

namespace globalization.xaml
{
    public class Helper : Common
    {
        public Helper()
        { }

        private static g.Helper _cHelper = new g.Helper();
        public g.Helper cHelper { get { return _cHelper; } }
    }
}
