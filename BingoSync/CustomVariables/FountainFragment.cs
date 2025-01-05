using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BingoSync.CustomVariables
{
    internal class FountainFragment
    {
        private static readonly string variableName = "fountainVesselFragmentCollected";
        private static readonly string fountainSceneName = "Abyss_04";
        private static readonly string fragmentVariableName = "vesselFragments";

        public static int CheckCollected(string name, int orig)
        {
            if (name == fragmentVariableName && GameManager.instance.GetSceneNameString() == fountainSceneName)
            {
                BingoTracker.UpdateBoolean(variableName, true);
            }
            return orig;
        }
    }
}
