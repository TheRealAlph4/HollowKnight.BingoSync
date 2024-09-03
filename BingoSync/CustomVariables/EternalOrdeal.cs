using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class EternalOrdeal
    {
        private static readonly string variableName = "eternalOrdealCount";
        private static readonly string objectName = "Battle Control";
        private static readonly string incrementFsmName = "Kill Counter";
        private static readonly string controlFsmName = "Control";
        private static readonly string fsmVariableName = "Kills";
        private static readonly string incrementStateName = "Increment";

        public static void CreateCounterTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != incrementFsmName || !self.HasState(incrementStateName)) return;
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
