using BingoSync.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace BingoSync
{
    [Serializable]
    [JsonObject("CustomGameMode")]
    public class CustomGameMode : GameMode
    {
        [JsonProperty("GameModeName")]
        public string InternalName
        {
            get
            {
                return base.GetDisplayName();
            }
            set
            {
                SetName(value);
            }
        }
        [JsonProperty("GoalGroups")]
        private List<GoalGroup> goalSettings;

        public CustomGameMode(string name, Dictionary<string, BingoGoal> goals, List<GoalGroup> loadedGoalSettings = null) 
            : base(name, goals)
        {
            if (loadedGoalSettings != null)
            {
                goalSettings = loadedGoalSettings;
            }
            else
            {
                goalSettings = GameModesManager.CreateDefaultCustomSettings();
            }
        }

        public List<GoalGroup> GetGoalSettings()
        {
            return goalSettings;
        }

        private void SetGoalsFromSettings()
        {
            Dictionary<string, BingoGoal> goals = [];
            foreach (GoalGroup goalGroup in goalSettings)
            {
                List<string> activeGoals = goalGroup.GetActiveGoals();
                List<BingoGoal> activeBingoGoals = GameModesManager.GetGoalsFromNames(activeGoals);
                foreach(BingoGoal goal in activeBingoGoals)
                {
                    goals[goal.name] = goal;
                }
            }
            SetGoals(goals);
        }

        public override string GenerateBoard(int seed)
        {
            SetGoalsFromSettings();
            return base.GenerateBoard(seed);
        }

        public override string GetDisplayName()
        {
            return base.GetDisplayName() + "*";
        }
    }
}
