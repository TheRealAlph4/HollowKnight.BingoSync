using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Tiso
    {
        private static string variableName = "tisoShieldHit";
        private static string objectName = "Tiso Shield Bone";
        private static string fsmName = "Head Control";
        private static string hitStateName = "Hit Effects";

        public static void CreateTisoShieldTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(hitStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
