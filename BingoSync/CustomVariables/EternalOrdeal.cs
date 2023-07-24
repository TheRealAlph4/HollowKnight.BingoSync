using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class EternalOrdeal
    {
        private static string variableName = "eternalOrdealCount";
        private static string objectName = "Battle Control";
        private static string incrementFsmName = "Kill Counter";
        private static string controlFsmName = "Control";
        private static string fsmVariableName = "Kills";
        private static string incrementStateName = "Increment";

        public static void CreateCounterTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != incrementFsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(incrementStateName, () => {
                var controlFsm = self.gameObject.LocateMyFSM(controlFsmName);
                if (controlFsm == null) return;
                var killCount = controlFsm.FsmVariables.GetFsmInt(fsmVariableName).Value;
                BingoTracker.UpdateInteger(variableName, killCount);
            });
        }
    }
}
