using System.Collections;
using GlobalEnums;

namespace BingoSync.CustomVariables
{
    internal static class Scenes
    {
        private static string overgrownMoundRoomName = "Room_Fungus_Shaman";
        private static string lifebloodCoreRoomName = "Abyss_08";

        public static IEnumerator EnterRoom(On.HeroController.orig_EnterScene orig, HeroController self, TransitionPoint enterGate, float delayBeforeEnter)
        {
            var zone = GameManager.instance.GetCurrentMapZone();
            var room = GameManager.instance.GetSceneNameString();

            // Overgrown Mound normally counts as royal gardens, which makes no sense.
            if (room == overgrownMoundRoomName)
            {
                zone = MapZone.OVERGROWN_MOUND.ToString();
            }

            CheckIfInLifebloodCoreRoom(room);

            string zoneVisitedVariableName = $"zoneVisited_{zone}";
            BingoTracker.UpdateBoolean(zoneVisitedVariableName, true);
            string roomVisitedVariableName = $"roomVisited_{room}";
            BingoTracker.UpdateBoolean(roomVisitedVariableName, true);
            return orig(self, enterGate, delayBeforeEnter);
        }

        public static void CheckIfInLifebloodCoreRoom(string roomName)
        {
            var variableName = "inLifebloodCoreRoom";
            BingoTracker.UpdateBoolean(variableName, roomName == lifebloodCoreRoomName);
        }
    }
}
