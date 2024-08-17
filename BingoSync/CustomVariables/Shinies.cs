using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Shinies
    {
        private static string fsmName = "Shiny Control";
        private static string trinketStateName = "Trink Flash";

        private static string GetVariableName(int trinketNum)
        {
            var roomName = GameManager.instance.GetSceneNameString();
            return $"gotShiny_{trinketNum}_{roomName}";
        }

        public static void CreateTrinketTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if(self.GetState(trinketStateName) == null) return;
            self.AddCustomAction(trinketStateName, () => {
                var trinketNum = self.FsmVariables.GetFsmInt("Trinket Num").Value;
                var variableName = GetVariableName(trinketNum);
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
