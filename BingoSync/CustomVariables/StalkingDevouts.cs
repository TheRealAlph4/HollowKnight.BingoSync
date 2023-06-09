namespace BingoSync.CustomVariables
{
    internal static class StalkingDevouts
    {
        private static string stalkingDevoutPrefix = "Slash Spider";

        public static void CheckIfStalkingDevoutWasKilled(EnemyDeathEffects enemyDeathEffects, bool _0, ref float? _1, ref bool _2, ref bool _3, ref bool _4)
        {
            if (!enemyDeathEffects.gameObject.name.StartsWith(stalkingDevoutPrefix)) return;
            var scene = GameManager.instance.GetSceneNameString();
            var variableName = $"killedStalkingDevout_{enemyDeathEffects.gameObject.name}_{scene}";
            BingoTracker.UpdateBoolean(variableName, true);
        }
    }
}
