using System.Collections;

namespace BingoSync.CustomVariables
{
    internal static class Revek
    {
        private static readonly string variableName = "parryRevekConsecutive";
        private static readonly string revekScene = "RestingGrounds_08";

        public static IEnumerator EnterRoom(On.HeroController.orig_EnterScene orig, HeroController self, TransitionPoint enterGate, float delayBeforeEnter)
        {
            BingoTracker.UpdateInteger(variableName, 0);
            return orig(self, enterGate, delayBeforeEnter);
        }

        public static void CheckParry(On.HeroController.orig_NailParry orig, HeroController self)
        {
            orig(self);
            if (GameManager.instance.GetSceneNameString() != revekScene) {
                return;
            }
            var consecutiveParries = BingoTracker.GetInteger(variableName) + 1;
            BingoTracker.UpdateInteger(variableName, consecutiveParries);
        }
    }
}
