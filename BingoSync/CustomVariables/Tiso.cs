using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Tiso
    {
        private static readonly string variableName = "tisoShieldHit";
        private static readonly string objectName = "Tiso Shield Bone";
        private static readonly string fsmName = "Head Control";
        private static readonly string hitStateName = "Hit Effects";

        public static void CreateTisoShieldTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(hitStateName)) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(hitStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
