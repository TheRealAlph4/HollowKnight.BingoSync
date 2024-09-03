using System.Collections.Generic;
using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class LoreTablets
    {
        private static readonly List<string> fsmNames = new List<string>{ "inspect_region", "Inspection" };
        private static readonly string readStateName = "Take Control";

        public static void MarkLoreTabletAsRead(string roomName, string objectName)
        {
            string variableName = $"readLoreTablet_{roomName}_{objectName}";
            BingoTracker.UpdateBoolean(variableName, true);
        }

        public static void CreateLoreTabletTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || !fsmNames.Contains(self.FsmName) || self.gameObject == null || !self.HasState(readStateName)) return;
            self.AddCustomAction(readStateName, () => {
                MarkLoreTabletAsRead(self.gameObject.scene.name, self.gameObject.name);
            });
        }
    }
}
