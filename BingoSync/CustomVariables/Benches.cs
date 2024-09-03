using System;
using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Benches
    {
        private static readonly string fsmName = "Bench Control";
        private static readonly string startRestStateName = "Start Rest";
        private static readonly string restingStateName = "Init Resting";

        public static void CreateBenchTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(startRestStateName)) return;
            self.AddCustomAction(startRestStateName, () => {
                string variableName = $"bench_{GameManager.instance.GetSceneNameString()}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
            if (!self.HasState(restingStateName)) return;
            self.AddCustomAction(restingStateName, () => {
                string variableName = $"bench_{GameManager.instance.GetSceneNameString()}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
