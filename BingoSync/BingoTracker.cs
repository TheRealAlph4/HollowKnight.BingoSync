using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Settings;
using System.Collections;

namespace BingoSync
{
    internal class BingoTracker
    {
        private static List<BingoSquare> _allPossibleSquares;
        private static Action<string> Log;
        public static SaveSettings Settings { get; set; }

        public static void Setup(Action<string> log)
        {
            _allPossibleSquares = [];

            Log = log;

            string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var resource in resources)
            {
                if (!resource.StartsWith("BingoSync.Resources.Squares"))
                {
                    continue;
                }
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
                using StreamReader reader = new(s);
                using JsonTextReader jsonReader = new(reader);
                JsonSerializer ser = new();
                var squares = ser.Deserialize<List<BingoSquare>>(jsonReader);
                _allPossibleSquares.AddRange(squares);
            }
        }

        public static Dictionary<string, BingoGoal> ProcessGoalsFile(string filepath)
        {
            using FileStream filestream = File.Open(filepath, FileMode.Open);
            return ProcessGoalsStream(filestream);
        }

        public static Dictionary<string, BingoGoal> ProcessGoalsStream(Stream goalstream)
        {
            Dictionary<string, BingoGoal> goals = [];
            List<BingoSquare> squares = [];

            using (StreamReader reader = new(goalstream))
            using (JsonTextReader jsonReader = new(reader))
            {
                JsonSerializer ser = new();
                var squaresSer = ser.Deserialize<List<BingoSquare>>(jsonReader);
                squares.AddRange(squaresSer);
            }

            foreach (BingoSquare square in squares)
            {
                goals.Add(square.Name, new BingoGoal(square.Name, []));
                _allPossibleSquares.Add(square);
            }
            return goals;
        }

        public static bool GetBoolean(string name)
        {
            if (Settings == null)
            {
                return false;
            }

            if (Settings.Booleans.TryGetValue(name, out bool current))
            {
                return current;
            }
            return false;
        }

        public static void UpdateBoolean(string name, bool value)
        {
            if (Settings.Booleans.ContainsKey(name))
            {
                Settings.Booleans[name] = value;
            }
            else
            {
                Settings.Booleans.Add(name, value);
            }
        }

        public static int GetInteger(string name)
        {
            if (Settings == null)
            {
                return 0;
            }

            if (Settings.Integers.TryGetValue(name, out int current))
            {
                return current;
            }
            return 0;
        }

        public static void UpdateInteger(string name, int current)
        {
            if (Settings == null)
            {
                return;
            }

            if (!Settings.Integers.TryGetValue(name, out int previous))
            {
                previous = 0;
            }
            UpdateInteger(name, previous, current);
        }

        public static void UpdateInteger(string name, int previous, int current)
        {
            if (Settings == null)
            {
                return;
            }

            var added = Math.Max(0, current - previous);
            var removed = Math.Max(0, previous - current);
            if (!Settings.Integers.ContainsKey(name))
            {
                Settings.Integers.Add(name, current);
                Settings.IntegersTotalAdded.Add(name, added);
                Settings.IntegersTotalRemoved.Add(name, removed);
            }
            else
            {
                Settings.Integers[name] = current;
                Settings.IntegersTotalAdded[name] += added;
                Settings.IntegersTotalRemoved[name] += removed;
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
                Settings.Booleans.TryGetValue(condition.VariableName, out var value);
                if (value == condition.ExpectedValue)
                {
                    condition.Solved = true;
                }
            }
            else if (condition.Type == ConditionType.Int)
            {
                int quantity = 0;
                int current = -1;
                int added = -1;
                int removed = -1;
                if (!Settings.Integers.TryGetValue(condition.VariableName, out current)
                    || !Settings.IntegersTotalAdded.TryGetValue(condition.VariableName, out added)
                    || !Settings.IntegersTotalRemoved.TryGetValue(condition.VariableName, out removed))
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
            bool isBlank = BingoSyncClient.board[index].Colors.Contains(BingoSyncClient.BLANK_COLOR);
            if ((shouldUnmark && marked) || (!shouldUnmark && !marked && (!BingoSyncClient.isLockout || isBlank)))
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
        public Condition Condition = new();
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
        public List<Condition> Conditions = [];
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
