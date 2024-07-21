using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string GetName()
        {
            return name;
        }

        virtual public string GenerateBoard()
        {
            List<BingoGoal> board = [];
            List<BingoGoal> availableGoals = new(goals.Values);
            Random r = new();
            while (board.Count < 25)
            {
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
                if (availableGoals.Count == 0)
                {
                    Modding.Logger.Log("Could not generate board");
                    return GetErrorBoard();
                }
            }

            return Jsonify(board);
        }

        private static string Jsonify(List<BingoGoal> board)
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
