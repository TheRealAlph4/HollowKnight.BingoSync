using GlobalEnums;

namespace BingoSync.CustomVariables
{
    internal static class Myla
    {
        private static string variableName = "killedMyla";
        public static int CheckIfMylaWasKilled(string name, int orig)
        {
            if (name != nameof(PlayerData.instance.killsZombieMiner))
                return orig;

            var zone = GameManager.instance.sm.mapZone;
            if (zone != MapZone.CROSSROADS)
                return orig;

            BingoTracker.UpdateBoolean(variableName, true);
            return orig;
        }
    }
}
