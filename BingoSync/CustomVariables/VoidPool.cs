using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class VoidPool
    {
        private static readonly string variableName = "voidPoolSwim";
        private static readonly string sceneNamePrefix = "Abyss";
        private static readonly string fsmName = "Surface Water Region";
        private static readonly string poolEnterStateName = "In";

        public static void CreateVoidPoolTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || !self.HasState(poolEnterStateName)) return;
            if (self.gameObject == null || !self.gameObject.scene.name.StartsWith(sceneNamePrefix)) return;
            self.AddCustomAction(poolEnterStateName, () => {
                BingoTracker.UpdateBoolean(variableName, true);
            });
        }
    }
}
