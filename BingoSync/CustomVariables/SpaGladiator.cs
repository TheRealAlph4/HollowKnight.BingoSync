using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class SpaGladiator
    {
        private static string variableName = "spaGladiatorSplashed";
        private static string objectName = "Spa Gladiator";
        private static string fsmName = "Control";
        private static string splashedStateName = "Splashed";

        public static void CreateSplashedTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(splashedStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
