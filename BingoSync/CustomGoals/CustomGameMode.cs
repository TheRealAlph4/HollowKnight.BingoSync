using BingoSync.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace BingoSync.CustomGoals
{
    [Serializable]
    [JsonObject("CustomGameMode")]
    public class CustomGameMode : IGameMode
    {
        public bool CanBeRenamed => true;
        public string DisplayName => _name + "*";

        private string _name;
        protected Dictionary<string, BingoGoal> _goals;
        [JsonProperty("GameModeName")]
        public string InternalName
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        [JsonProperty("GoalGroups")]
        private readonly List<GoalGroup> goalSettings;

        public CustomGameMode(string name, List<GoalGroup> loadedGoalSettings = null) 
        {
            _name = name;
            _goals = [];
            if (loadedGoalSettings != null)
            {
                goalSettings = loadedGoalSettings;
            }
            else
            {
                goalSettings = GameModesManager.CreateDefaultCustomSettings();
            }
        }

        public void AddGoalGroupToSettings(GoalGroup goalGroup)
        {
            goalSettings.Add(goalGroup);
        }

        public string SetName(string newName)
        {
            string oldName = _name;
            _name = newName;
            return oldName;
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
                if (!GameModesManager.GoalGroupExists(goalGroup.Name))
                {
                    Modding.Logger.Log($"Group \"{goalGroup.Name}\" is not registered, skipping");
                    continue;
                }
                List<string> activeGoals = goalGroup.GetActiveGoals();
                List<BingoGoal> activeBingoGoals = GameModesManager.GetGoalsFromNames(goalGroup.Name, activeGoals);
                foreach(BingoGoal goal in activeBingoGoals)
                {
                    if(goals.ContainsKey(goal.name))
                    {
                        goals[goal.name].exclusions.AddRange(goal.exclusions);
                    }
                    else
                    {
                        goals[goal.name] = goal;
                    }
                }
            }
            _goals = goals;
        }

        public List<BingoGoal> GenerateBoard(int seed)
        {
            SetGoalsFromSettings();
            List<BingoGoal> board = [];
            List<BingoGoal> availableGoals = [.. _goals.Values];
            Random r = new(seed);
            while (board.Count < 25)
            {
                if (availableGoals.Count == 0)
                {
                    Modding.Logger.Log("Could not generate board");
                    return GetErrorBoard();
                }
                int index = r.Next(availableGoals.Count);
                BingoGoal proposedGoal = availableGoals[index];
                bool valid = true;
                foreach (BingoGoal existing in board)
                {
                    if (existing.Excludes(proposedGoal) || proposedGoal.Excludes(existing))
                    {
                        valid = false;
                    }
                }
                if (valid)
                {
                    board.Add(proposedGoal);
                }
                availableGoals.Remove(proposedGoal);
            }
            return board;
        }

        public static List<BingoGoal> GetErrorBoard()
        {
            BingoGoal empty = new("-");
            List<BingoGoal> board = [new BingoGoal("Error generating board")];
            for (int i = 0; i < 24; ++i)
            {
                board.Add(empty);
            }
            return board;
        }
    }
}
