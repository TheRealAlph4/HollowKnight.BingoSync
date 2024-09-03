using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Telescope
    {
        private static readonly string variableName = "telescopeInteract";
        private static readonly string objectName = "Telescope Inspect";
        private static readonly string fsmName = "Conversation Control";
        private static readonly string interactStateName = "Fade";

        public static void CreateTelescopeTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(interactStateName)) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(interactStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
