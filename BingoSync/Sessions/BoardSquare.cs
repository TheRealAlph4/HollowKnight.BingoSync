using System.Collections.Generic;

namespace BingoSync.Sessions
{
    public class BoardSquare
    {
        public string Name { get; set; }
        public HashSet<int> MarkedBy { get; set; }
        public bool Highlighted { get; set; }
        public int GoalIndex { get; set; }
    }
}
