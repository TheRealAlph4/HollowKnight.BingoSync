using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class ShadeGates
    {
        private static string variableName = "shadeGatesHit";
        private static string objectName = "Slash Effect";
        private static string fsmName = "Control";
        private static string hitStateName = "Pause";

        public static void CreateShadeGateTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || !self.gameObject.name.StartsWith(objectName)) return;
            self.AddCustomAction(hitStateName, () => {
                string uniqueVariableName = $"hitShadeGate_{self.gameObject.scene.name}_{self.gameObject.GetPath()}";
                var alreadyHit = BingoTracker.GetBoolean(uniqueVariableName);
                if (alreadyHit)
                    return;
                BingoTracker.UpdateBoolean(uniqueVariableName, true);
                var shadeGatesHit = BingoTracker.GetInteger(variableName) + 1;
                BingoTracker.UpdateInteger(variableName, shadeGatesHit);
            });
        }
    }
}
