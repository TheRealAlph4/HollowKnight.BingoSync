using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class DreamNailDialogue
    {
        private static readonly string fsmName = "npc_dream_dialogue";
        private static readonly string hitStateName = "Take Control";

        public static void CreateDreamNailDialogueTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || self.gameObject == null || !self.HasState(hitStateName)) return;
            self.AddCustomAction(hitStateName, () => {
                string variableName = $"dreamDialogue_{self.gameObject.scene.name}_{self.gameObject.name}";
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
