using System.Collections.Generic;

namespace BingoSync.CustomGoals
{
    public interface IGameMode
    {
        bool CanBeRenamed { get; }
        string DisplayName { get; }
        string SetName(string newName);
        List<string> GenerateBoard(int seed);
    }
}
