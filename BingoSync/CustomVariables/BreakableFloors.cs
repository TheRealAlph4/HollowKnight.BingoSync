using Satchel;

namespace BingoSync.CustomVariables
{
    internal static class BreakableFloors
    {
        private static readonly string variableName = "floorsBroken";
        private static readonly string fsmName = "quake_floor";
        private static readonly string breakStateName = "Destroy";

        public static void CreateBreakableFloorsTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName || self.gameObject == null || !self.HasState(breakStateName)) return;
            self.AddCustomAction(breakStateName, () => {
                var floorsBroken = BingoTracker.GetInteger(variableName);
                BingoTracker.UpdateInteger(variableName, floorsBroken + 1);
            });
        }
    }
}
