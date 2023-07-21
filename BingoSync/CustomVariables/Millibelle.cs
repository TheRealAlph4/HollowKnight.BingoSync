using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Millibelle
    {
        private static string variableName = "millibelleHit";
        private static string objectName = "Banker Spa NPC";
        private static string fsmName = "Hit Around";
        private static string slashStateName = "Give Geo";

        public static void CreateMillibelleHitTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(slashStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
