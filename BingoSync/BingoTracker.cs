using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Settings;

namespace BingoSync
{
    internal class BingoTracker
    {
        private static List<BingoSquare> _allPossibleSquares;
        private static Action<string> Log;
        public static SaveSettings settings { get; set; }

        public static void Setup(Action<string> log)
        {
            _allPossibleSquares = new List<BingoSquare>();

            Log = log;

            string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var resource in resources)
            {
                if (!resource.StartsWith("BingoSync.Resources.Squares"))
                {
                    continue;
                }
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
                using (StreamReader reader = new StreamReader(s))
                using (JsonTextReader jsonReader = new JsonTextReader(reader))
                {
                    JsonSerializer ser = new JsonSerializer();
                    var squares = ser.Deserialize<List<BingoSquare>>(jsonReader);
                    _allPossibleSquares.AddRange(squares);
                }
            }
        }

        public static bool GetBoolean(string name)
        {
            if (settings == null)
            {
                return false;
            }

            bool current;
            if (settings.Booleans.TryGetValue(name, out current))
            {
                return current;
            }
            return false;
        }

        public static void UpdateBoolean(string name, bool value)
        {
            if (settings.Booleans.ContainsKey(name))
            {
                settings.Booleans[name] = value;
            }
            else
            {
                settings.Booleans.Add(name, value);
            }
        }

        public static int GetInteger(string name)
        {
            if (settings == null)
            {
                return 0;
            }

            int current;
            if (settings.Integers.TryGetValue(name, out current))
            {
                return current;
            }
            return 0;
        }

        public static void UpdateInteger(string name, int current)
        {
            if (settings == null)
            {
                return;
            }

            int previous;
            if (!settings.Integers.TryGetValue(name, out previous))
            {
                previous = 0;
            }
            UpdateInteger(name, previous, current);
        }

        public static void UpdateInteger(string name, int previous, int current)
        {
            if (settings == null)
            {
                return;
            }

            var added = Math.Max(0, current - previous);
            var removed = Math.Max(0, previous - current);
            if (!settings.Integers.ContainsKey(name))
            {
                settings.Integers.Add(name, current);
                settings.IntegersTotalAdded.Add(name, added);
                settings.IntegersTotalRemoved.Add(name, removed);
            }
            else
            {
                settings.Integers[name] = current;
                settings.IntegersTotalAdded[name] += added;
                settings.IntegersTotalRemoved[name] += removed;
            }
        }

        public static bool IsBoardAvailable()
        {
            BingoSyncClient.Update();
            if (BingoSyncClient.board == null || BingoSyncClient.isHidden)
                return false;
            if (BingoSyncClient.GetState() != BingoSyncClient.State.Connected)
                return false;
            return true;
        }

        public static void ProcessBingo()
        {
            if (!IsBoardAvailable()) return;
            _allPossibleSquares.ForEach(square =>
            {
                bool wasSolved = square.Condition.Solved;
                bool isSolved = IsSolved(square);
                if (wasSolved != isSolved)
                    UpdateGoal(square.Name, shouldUnmark: !isSolved);
            });
        }

        private static bool IsSolved(BingoSquare square)
        {
            if (square.Condition.Solved && (!BingoSync.modSettings.UnmarkGoals || !square.CanUnmark))
                return square.Condition.Solved;
            UpdateCondition(square.Condition);
            return square.Condition.Solved;
        }

        private static void UpdateCondition(Condition condition)
        {
            condition.Solved = false;
            if (condition.Type == ConditionType.Bool)
            {
                settings.Booleans.TryGetValue(condition.VariableName, out var value);
                if (value == condition.ExpectedValue)
                {
                    condition.Solved = true;
                }
            }
            else if (condition.Type == ConditionType.Int)
            {
                int quantity = 0;
                int current, added, removed;
                if (!settings.Integers.TryGetValue(condition.VariableName, out current)
                    || !settings.IntegersTotalAdded.TryGetValue(condition.VariableName, out added)
                    || !settings.IntegersTotalRemoved.TryGetValue(condition.VariableName, out removed))
                {
                    return;
                }
                switch (condition.State)
                {
                    case BingoRequirementState.Current:
                        quantity = current;
                        break;
                    case BingoRequirementState.Added:
                        quantity = added;
                        break;
                    case BingoRequirementState.Removed:
                        quantity = removed;
                        break;
                }
                if (quantity >= condition.ExpectedQuantity)
                {
                    condition.Solved = true;
                }
            }
            else {
                condition.Conditions.ForEach(UpdateCondition);
                if (condition.Type == ConditionType.Or) {
                    condition.Solved = condition.Conditions.Any(cond => cond.Solved);
                }
                else if (condition.Type == ConditionType.And) {
                    condition.Solved = condition.Conditions.All(cond => cond.Solved);
                }
                else if (condition.Type == ConditionType.Some) {
                    condition.Solved = condition.Conditions.FindAll(cond => cond.Solved).Count >= condition.Amount;
                }
            }
        }

        public static void ClearFinishedGoals() {
            _allPossibleSquares.ForEach(square => {
                ClearCondition(square.Condition);
            });
        }

        public static void ClearCondition(Condition condition) {
            condition.Solved = false;
            condition.Conditions.ForEach(ClearCondition);
        }

        public static void GoalUpdated(string goal, int index)
        {
            if (!BingoSync.modSettings.UnmarkGoals)
                return;
            bool marked = BingoSyncClient.board[index].Colors.Contains(BingoSyncClient.color);
            if (marked)
                return;
            var square = _allPossibleSquares.Find(x => x.Name == goal);
            if (!square.CanUnmark)
                return;
            if (!square.Condition.Solved)
                return;
            UpdateGoal(goal, shouldUnmark: false);
        }

        public static void UpdateGoal(string goal, bool shouldUnmark)
        {
            if (!IsBoardAvailable()) return;
            var index = BingoSyncClient.board.FindIndex(x => x.Name == goal);
            if (index == -1)
                return;
            bool marked = BingoSyncClient.board[index].Colors.Contains(BingoSyncClient.color);
            if ((shouldUnmark && marked) || (!shouldUnmark && !marked))
            {
                Log($"Updating Goal: {goal}, [Unmarking: {shouldUnmark}]");
                BingoSyncClient.SelectSquare(index + 1, () =>
                {
                    UpdateGoal(goal, shouldUnmark);
                }, shouldUnmark);
            }
        }
    }

    internal class BingoSquare
    {
        public string Name = string.Empty;
        public Condition Condition = new Condition();
        public bool CanUnmark = false;
    }

    internal class Condition
    {
        public ConditionType Type = ConditionType.And;
        public int Amount = 0;
        public bool Solved = false;
        public string VariableName = string.Empty;
        public BingoRequirementState State = BingoRequirementState.Current;
        public int ExpectedQuantity = 0;
        public bool ExpectedValue = false;
        public List<Condition> Conditions = new List<Condition>();
    }

    enum ConditionType
    {
        Bool,
        Int,
        Or,
        And,
        Some,
    }

    enum BingoRequirementState
    {
        Current,
        Added,
        Removed,
    }
}
