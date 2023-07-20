using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class OroTrainingDummy
    {
        private static string variableName = "oroTrainingDummyTriggered";
        private static string objectName = "Training Dummy";
        private static string fsmName = "Hit";
        private static string summonStateName = "Summon?";

        public static void CreateOroTrainingDummyTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(summonStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
