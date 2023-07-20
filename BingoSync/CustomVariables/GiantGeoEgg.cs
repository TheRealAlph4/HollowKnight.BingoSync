using Satchel;
using UnityEngine;

namespace BingoSync.CustomVariables
{
    internal static class GiantGeoEgg
    {
        private static string variableName = "destroyedGiantGeoEgg";
        private static string objectName = "Giant Geo Egg";
        private static string fsmName = "Geo Rock";
        private static string destroyedStateName = "Destroy";

        public static void CreateGiantGeoRockTrigger(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self == null || self.FsmName != fsmName) return;
            if (self.gameObject == null || self.gameObject.name != objectName) return;
            self.AddCustomAction(destroyedStateName, () => BingoTracker.UpdateBoolean(variableName, true));
        }
    }
}
