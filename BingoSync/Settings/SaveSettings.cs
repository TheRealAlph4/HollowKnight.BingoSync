using System.Collections.Generic;

namespace Settings
{
    public class SaveSettings
    {
        public Dictionary<string, bool> Booleans = [];
        public Dictionary<string, int> Integers = [];
        public Dictionary<string, int> IntegersTotalAdded = [];
        public Dictionary<string, int> IntegersTotalRemoved = [];
    }
}
