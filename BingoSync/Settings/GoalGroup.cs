using ItemChanger.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BingoSync.Settings
{
    [Serializable]
    [JsonObject("GoalGroup")]
    public class GoalGroup
    {
        [JsonProperty("GroupName")]
        public string Name { get; set; }
        [JsonProperty("AllGoalsOn")]
        public bool AllGoalsOn { get; set; } = false;
        [JsonProperty("CustomSettingsOn")]
        public bool CustomSettingsOn { get; set; } = false;
        [JsonProperty("GoalSettings")]
        public Dictionary<string, bool> customSettings = [];

        public GoalGroup(string name, List<string> goals)
        {
            Name = name;
            if(goals == null)
            {
                return;
            }
            foreach (string goal in goals)
            {
                customSettings[goal] = true;
            }
        }

        public List<string> GetGoalNames()
        {
            return customSettings.Keys.ToList();
        }

        public bool GoalIsOnInCustom(string goalName)
        {
            return customSettings.GetOrDefault(goalName, false);
        }

        public List<string> GetActiveGoals()
        {
            return customSettings.Select(pair => pair.Key).Where(goal => GoalIsEffectivelyOn(goal)).ToList();
        }

        public bool GoalIsEffectivelyOn(string goalName)
        {
            if (CustomSettingsOn) return GoalIsOnInCustom(goalName);
            return AllGoalsOn;
        }
    }
}
