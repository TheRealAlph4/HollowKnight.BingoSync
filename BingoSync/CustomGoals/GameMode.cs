using System;
using System.Collections.Generic;
using System.Linq;

namespace BingoSync
{
    public class GameMode(string name, Dictionary<string, BingoGoal> goals)
    {
        private readonly string name = name;
        private Dictionary<string, BingoGoal> goals = goals;

        public Dictionary<string, BingoGoal> GetGoals()
        {
            return goals;
        }

        public void SetGoals(Dictionary<string, BingoGoal> goals)
        {
            this.goals = goals;
        }

        virtual public string GetName()
        {
            return name;
        }

        virtual public string GenerateBoard(int seed)
        {
            List<BingoGoal> board = [];
            List<BingoGoal> availableGoals = new(goals.Values);
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

            return Jsonify(board);
        }

        public static string Jsonify(List<BingoGoal> board)
        {
            string output = "[";
            for (int i = 0; i < board.Count; i++)
            {
                output += "{\"name\": \"" + board.ElementAt(i).name + "\"}" + (i < 24 ? "," : "");
            }
            output += "]";
            return output;
        }

        public static string GetErrorBoard()
        {
            return "[{\"name\": \"Error generating board\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"},{\"name\": \"-\"}]";
        }
    }
}
