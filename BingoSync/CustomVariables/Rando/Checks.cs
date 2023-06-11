using ItemChanger;

namespace BingoSync.CustomVariables.Rando
{
    internal static class Checks
    {
        public static void Checked(ReadOnlyGiveEventArgs args)
        {
            var variableName = $"checked_{args.Placement.Name}";
            BingoTracker.UpdateBoolean(variableName, true);
        }
    }
}

