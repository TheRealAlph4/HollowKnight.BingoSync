using Satchel;

namespace BingoSync.CustomVariables
{
    // This is needed because rando does not set the openedCityGate PlayerData variable
    internal static class CityGate
    {
        private static string variableName = "openedCityGate";
        private static string objectName = "City Gate Control";
        private static string fsmName = "Conversation Control";
        private static string openStateName = "Activate";

        public static void CreateCityGateOpenedTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(openStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
