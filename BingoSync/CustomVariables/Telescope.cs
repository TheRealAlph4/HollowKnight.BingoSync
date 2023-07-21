using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Telescope
    {
        private static string variableName = "telescopeInteract";
        private static string objectName = "Telescope Inspect";
        private static string fsmName = "Conversation Control";
        private static string interactStateName = "Fade";

        public static void CreateTelescopeTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(interactStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
