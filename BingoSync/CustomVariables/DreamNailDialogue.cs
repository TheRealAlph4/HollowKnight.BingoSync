using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class DreamNailDialogue
    {
        private static string fsmName = "npc_dream_dialogue";
        private static string hitStateName = "Take Control";

        public static void CreateDreamNailDialogueTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || self.gameObject == null) return;
            self.AddCustomAction(hitStateName, () => {
                string variableName = $"dreamDialogue_{self.gameObject.scene.name}_{self.gameObject.name}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
