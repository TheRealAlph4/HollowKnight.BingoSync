using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class BreakableFloors
    {
        private static string variableName = "floorsBroken";
        private static string fsmName = "quake_floor";
        private static string breakStateName = "Destroy";

        public static void CreateBreakableFloorsTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || self.gameObject == null) return;
            self.AddCustomAction(breakStateName, () => {
                var floorsBroken = BingoTracker.GetInteger(variableName);
                BingoTracker.UpdateInteger(variableName, floorsBroken + 1);
            });
        }
    }
}
