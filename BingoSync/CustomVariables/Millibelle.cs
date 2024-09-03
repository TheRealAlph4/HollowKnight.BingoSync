using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Millibelle
    {
        private static readonly string variableName = "millibelleHit";
        private static readonly string objectName = "Banker Spa NPC";
        private static readonly string fsmName = "Hit Around";
        private static readonly string slashStateName = "Give Geo";

        public static void CreateMillibelleHitTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(slashStateName)) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(slashStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
