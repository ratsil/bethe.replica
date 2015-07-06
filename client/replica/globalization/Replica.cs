using g = globalization;

namespace globalization.xaml
{
    public class Replica : Helper
    {
        private static g.Replica _cReplica = new g.Replica();
        public g.Replica cReplica { get { return _cReplica; } }
    }
}
