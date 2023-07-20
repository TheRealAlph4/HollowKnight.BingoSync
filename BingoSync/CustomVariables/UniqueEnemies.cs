using System.Reflection;

namespace BingoSync.CustomVariables
{
    internal static class UniqueEnemies
    {
        public static void CheckIfUniqueEnemyWasKilled(EnemyDeathEffects enemyDeathEffects, bool _0, ref float? _1, ref bool _2, ref bool _3, ref bool _4)
        {
            var field = typeof(EnemyDeathEffects).GetField("playerDataName", BindingFlags.Instance | BindingFlags.NonPublic);
            var enemyName = field.GetValue(enemyDeathEffects);
            var scene = GameManager.instance.GetSceneNameString();
            var variableName = $"killed_{enemyDeathEffects.name}_{scene}";
            var alreadyKilled = BingoTracker.GetBoolean(variableName);
            BingoTracker.UpdateBoolean(variableName, true);
            if (alreadyKilled) {
                return;
            }
            var countVariableName = $"killedUnique_{enemyName}";
            var uniqueCount = BingoTracker.GetInteger(countVariableName);
            BingoTracker.UpdateInteger(countVariableName, uniqueCount + 1);
        }
    }
}
