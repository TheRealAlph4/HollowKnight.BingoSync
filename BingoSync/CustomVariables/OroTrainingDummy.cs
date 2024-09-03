using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class OroTrainingDummy
    {
        private static readonly string variableName = "oroTrainingDummyTriggered";
        private static readonly string objectName = "Training Dummy";
        private static readonly string fsmName = "Hit";
        private static readonly string summonStateName = "Summon?";

        public static void CreateOroTrainingDummyTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(summonStateName)) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(summonStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
