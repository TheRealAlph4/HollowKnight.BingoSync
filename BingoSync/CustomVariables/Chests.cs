using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Chests
    {
        private static string fsmName = "Chest Control";
        private static string summonStateName = "Opened";

        public static void CreateChestOpenTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            self.AddCustomAction(summonStateName, () => {
                string variableName = $"chestOpen_{GameManager.instance.GetSceneNameString()}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
