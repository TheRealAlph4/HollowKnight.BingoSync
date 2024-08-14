using System.Collections.Generic;
using System.IO;
using BingoSync.CustomGoals;

namespace BingoSync
{
    public static class Goals
    {
        public static Dictionary<string, BingoGoal> ProcessGoalsFile(string filepath)
        {
            return BingoTracker.ProcessGoalsFile(filepath);
        }

        public static Dictionary<string, BingoGoal> ProcessGoalsStream(Stream goalstream)
        {
            return BingoTracker.ProcessGoalsStream(goalstream);
        }

        public static Dictionary<string, BingoGoal> GetVanillaGoals()
        {
            return GameModesManager.GetVanillaGoals();
        }

        public static void RegisterGoalsForCustom(string groupName, Dictionary<string, BingoGoal> goals)
        {
            GameModesManager.RegisterGoalsForCustom(groupName, goals);
        }

        public static void AddGameMode(GameMode gameMode)
        {
            GameModesManager.AddGameMode(gameMode);
        }
    }
}
