using UnityEngine;

namespace BingoSync.CustomVariables
{
    internal static class GiantGeoEgg
    {
        private static string variableName = "destroyedGiantGeoEgg";
        private static string objectName = "Giant Geo Egg";
        private static string fsmName = "Geo Rock";
        private static string destroyedStateName = "Destroy";
        private static GameObject giantGeoEgg = null;

        public static void CheckIfGiantGeoEggWasDestroyed()
        {
            if (giantGeoEgg == null) return;
            var fsm = giantGeoEgg.LocateMyFSM(fsmName);
            if (fsm == null) return;
            if (fsm.ActiveStateName != destroyedStateName) return;
            BingoTracker.UpdateBoolean(variableName, true);
        }

        public static void FindGiantGeoEggGameObject(GameObject obj)
        {
            if (obj == null || obj.name != objectName) return;
            giantGeoEgg = obj;
        }

    }
}
