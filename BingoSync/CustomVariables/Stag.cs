using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Stag
    {
        private static string objectName = "Stag";
        private static string fsmName = "Stag Control";
        private static string travelStateName = "Go To Stag Cutscene";

        public static void CreateStagTravelTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(travelStateName, () => {
                var targetPos = self.FsmVariables.GetVariable("To Position");
                if (targetPos == null) return;
                string variableName = $"stagTravelTo_{targetPos.ToInt()}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
