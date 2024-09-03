﻿using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Switches
    {
        private static readonly string fsmName = "Switch Control";
        private static readonly string openStateName = "Open";

        public static void CreateSwitchOpenTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(openStateName)) return;
            if (self.GetState(openStateName) == null) return;
            self.AddCustomAction(openStateName, () => {
                string variableName = $"switchOpen_{GameManager.instance.GetSceneNameString()}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
