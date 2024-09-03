using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Chests
    {
        private static readonly string fsmName = "Chest Control";
        private static readonly string openStateName = "Opened";

        public static void CreateChestOpenTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(openStateName)) return;
            self.AddCustomAction(openStateName, () => {
                string variableName = $"chestOpen_{GameManager.instance.GetSceneNameString()}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
