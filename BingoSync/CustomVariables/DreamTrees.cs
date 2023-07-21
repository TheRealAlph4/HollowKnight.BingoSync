using System;
using System.Reflection;
using BingoSync.Helpers;
using MonoMod.Cil;

namespace BingoSync.CustomVariables
{
    internal static class DreamTrees
    {
        private static string variableName = "dreamTreesCompleted";

        public static void TrackDreamTrees(ILContext il)
        {
            FieldInfo completed = typeof(DreamPlant).GetField("completed", BindingFlags.NonPublic | BindingFlags.Instance);
            ILCursor cursor = new ILCursor(il).Goto(0);
            while (cursor.TryGotoNext(i => i.MatchStfld(completed)))
            {
                cursor.GotoNext();
                cursor.EmitDelegate<Action>(() =>
                {
                    var dreamTreesCompleted = BingoTracker.GetInteger(variableName);
                    BingoTracker.UpdateInteger(variableName, dreamTreesCompleted + 1);
                    var zone = ZoneHelper.GreaterZone(GameManager.instance.sm.mapZone);
                    string regionVariableName = $"dreamTreeCompleted_{zone}";
                    BingoTracker.UpdateBoolean(regionVariableName, true);
                });
            }
        }
    }
}
