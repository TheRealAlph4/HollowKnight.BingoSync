using System.Collections.Generic;

namespace BingoSync.CustomGoals
{
    public class BingoGoal
    {
        public string name;
        public List<string> exclusions;

        public BingoGoal(string goalName)
        {
            name = goalName;
            exclusions = [];
        }

        public BingoGoal(string name, List<string> exclusions)
        {
            this.name = name;
            this.exclusions = exclusions;
        }

        public bool Excludes(string other)
        {
            return exclusions.Contains(other);
        }
        public bool Excludes(BingoGoal other)
        {
            return exclusions.Contains(other.name);
        }
    }
}
