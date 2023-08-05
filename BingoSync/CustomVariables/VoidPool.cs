using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class VoidPool
    {
        private static string variableName = "voidPoolSwim";
        private static string sceneNamePrefix = "Abyss";
        private static string fsmName = "Surface Water Region";
        private static string poolEnterStateName = "In";

        public static void CreateVoidPoolTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || !self.gameObject.scene.name.StartsWith(sceneNamePrefix)) return;
            self.AddCustomAction(poolEnterStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
