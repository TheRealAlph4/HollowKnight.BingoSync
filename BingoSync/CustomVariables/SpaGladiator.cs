using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class SpaGladiator
    {
        private static readonly string variableName = "spaGladiatorSplashed";
        private static readonly string objectName = "Spa Gladiator";
        private static readonly string fsmName = "Control";
        private static readonly string splashedStateName = "Splashed";

        public static void CreateSplashedTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(splashedStateName)) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(splashedStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
