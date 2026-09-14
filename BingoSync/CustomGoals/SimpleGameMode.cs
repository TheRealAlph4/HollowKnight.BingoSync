using System;
using System.Collections.Generic;
using System.Linq;

namespace BingoSync.CustomGoals
{
    public class SimpleGameMode(string name, Dictionary<string, BingoGoal> goals) : IGameMode
    {
        public bool CanBeRenamed => false;
        public string DisplayName => _name;
        
        private readonly string _name = name;
        private readonly Dictionary<string, BingoGoal> _goals = goals;

        public string SetName(string _)
        {
            return _name;
        }

        public List<string> GenerateBoard(int seed)
        {
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

            return [.. board.Select(goal => goal.Name)];
        }

        public static List<string> GetErrorBoard()
        {
            List<string> board = ["Error generating board"];
            for(int i = 0; i < 24; ++i)
            {
                board.Add("-");
            }
            return board;
        }
    }
}
