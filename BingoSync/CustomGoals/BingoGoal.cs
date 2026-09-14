using System.Collections.Generic;

namespace BingoSync.CustomGoals
{
    public class BingoGoal(string goalName, List<string>? exclusions = null)
    {
        public string Name = goalName;
        public List<string> Exclusions = exclusions ?? [];

        public bool Excludes(string other)
        {
            return Exclusions.Contains(other);
        }
        public bool Excludes(BingoGoal other)
        {
            return Exclusions.Contains(other.Name);
        }
    }
}
