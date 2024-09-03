using Satchel;

namespace BingoSync.CustomVariables
{
    // This is needed because rando does not set the openedCityGate PlayerData variable
    internal static class CityGate
    {
        private static readonly string variableName = "openedCityGate";
        private static readonly string objectName = "City Gate Control";
        private static readonly string fsmName = "Conversation Control";
        private static readonly string openStateName = "Activate";

        public static void CreateCityGateOpenedTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(openStateName)) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(openStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
