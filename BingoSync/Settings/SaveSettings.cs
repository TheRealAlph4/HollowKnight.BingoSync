using System.Collections.Generic;

namespace Settings
{
    public class SaveSettings
    {
        public Dictionary<string, bool> Booleans = new Dictionary<string, bool>();
        public Dictionary<string, int> Integers = new Dictionary<string, int>();
        public Dictionary<string, int> IntegersTotalAdded = new Dictionary<string, int>();
        public Dictionary<string, int> IntegersTotalRemoved = new Dictionary<string, int>();
    }
}
