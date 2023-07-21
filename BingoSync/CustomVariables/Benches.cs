using System;
using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Benches
    {
        private static string fsmName = "Bench Control";
        private static string startRestStateName = "Start Rest";
        private static string restingStateName = "Init Resting";

        public static void CreateBenchTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            self.AddCustomAction(startRestStateName, () => {
                string variableName = $"bench_{GameManager.instance.GetSceneNameString()}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
            self.AddCustomAction(restingStateName, () => {
                string variableName = $"bench_{GameManager.instance.GetSceneNameString()}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
