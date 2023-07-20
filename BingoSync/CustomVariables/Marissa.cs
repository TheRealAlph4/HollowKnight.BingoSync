using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class Marissa
    {
        private static string variableName = "killedMarissa";
        private static string roomName = "Ruins_Bathhouse";
        private static string objectName = "Ghost NPC";
        private static string fsmName = "ghost_npc_death";
        private static string killedStateName = "Kill";

        public static void CreateMarissaKilledTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (GameManager.instance.GetSceneNameString() != roomName) return;
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(killedStateName, () => BingoTracker.UpdateBoolean(variableName, true));
        }
    }
}
