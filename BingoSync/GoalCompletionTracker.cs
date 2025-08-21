using BingoSync.CustomGoals;
using BingoSync.Helpers;
using BingoSync.Sessions;
using BingoSync.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ItemSyncMarkDelay = BingoSync.Settings.ModSettings.ItemSyncMarkDelay;

namespace BingoSync
{
    internal class GoalCompletionTracker
    {
        private static List<BingoSquare> _allKnownSquares;
        private static Action<string> Log;
        public static SaveSettings Settings { get; set; }

        public static void Setup(Action<string> log)
        {
            _allKnownSquares = [];

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
                _allKnownSquares.AddRange(squares);
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
                _allKnownSquares.Add(square);
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

        public static void UpdateAllKnownSquares(Session session, bool isItemSyncUpdate = false)
        {
            if (!session.IsPlayable()) return;
            _allKnownSquares.ForEach(square =>
            {
                bool wasSolved = square.Condition.Solved;
                bool isSolved = IsSolved(square);
                if (wasSolved != isSolved)
                    UpdateSquare(session, square.Name, shouldUnmark: !isSolved, isItemSyncUpdate);
            });
        }

        private static bool IsSolved(BingoSquare square)
        {
            if (square.Condition.Solved && (!Controller.GlobalSettings.UnmarkGoals || !square.CanUnmark))
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
            _allKnownSquares.ForEach(square => {
                ClearCondition(square.Condition);
            });
        }

        public static void ClearCondition(Condition condition) {
            condition.Solved = false;
            condition.Conditions.ForEach(ClearCondition);
        }

        public static void GoalUpdateFromServer(Session session, string goal, int index)
        {
            if (!Controller.GlobalSettings.UnmarkGoals)
                return;
            bool marked = session.Board.GetSlot(index).MarkedBy.Contains(ColorExtensions.FromName(session.RoomColor.GetName()));
            if (marked)
                return;
            var square = _allKnownSquares.Find(x => x.Name == goal);
            if (!square.CanUnmark)
                return;
            if (!square.Condition.Solved)
                return;
            UpdateSquare(session, goal, shouldUnmark: false);
        }

        public static void UpdateSquare(Session session, string goal, bool shouldUnmark, bool isItemSyncUpdate = false)
        {
            if (!session.IsPlayable()) return;
            int index = -1;
            foreach(Square square in session.Board)
            {
                if(square.Name == goal)
                {
                    index = square.GoalNr;
                    break;
                }
            }
            if (index == -1)
                return;
            bool marked = session.Board.GetSlot(index).MarkedBy.Contains(session.RoomColor);
            bool isBlank = session.Board.GetSlot(index).MarkedBy.Contains(Colors.Blank);
            bool isUnmarking = shouldUnmark && marked;
            bool isMarkable = !session.RoomIsLockout || isBlank;
            bool isMarking = !shouldUnmark && !marked && isMarkable;
            if (isUnmarking || isMarking)
            {
                Task.Run(() => MarkingThread(session, goal, shouldUnmark, index, isItemSyncUpdate));
            }
        }

        private static void MarkingThread(Session session, string goal, bool shouldUnmark, int index, bool isItemSyncUpdate)
        {
            ItemSyncMarkDelay setting = Controller.GlobalSettings.ItemSyncMarkSetting;
            if (setting == ItemSyncMarkDelay.NoMark && isItemSyncUpdate)
            {
                Log($"Ignoring mark of goal '{goal}'");
                return;
            }
            if (setting == ItemSyncMarkDelay.Delay && isItemSyncUpdate)
            {
                Log($"Waiting {ItemSyncInterop.MarkDelay} to mark goal '{goal}'");
                Thread.Sleep(ItemSyncInterop.MarkDelay);
                bool marked = session.Board.GetSlot(index).MarkedBy.Contains(session.RoomColor);
                bool isBlank = session.Board.GetSlot(index).MarkedBy.Contains(Colors.Blank);
                bool isUnmarking = shouldUnmark && marked;
                bool isMarkable = !session.RoomIsLockout || isBlank;
                bool isMarking = !shouldUnmark && !marked && isMarkable;
                if (!(isUnmarking || isMarking))
                {
                    return;
                }
            }
            session.SelectSquare(index + 1, () =>
            {
                UpdateSquare(session, goal, shouldUnmark);
            }, shouldUnmark);
        }
    }

    class BingoSquare
    {
        public string Name = string.Empty;
        public Condition Condition = new();
        public bool CanUnmark = false;
    }

    class Condition
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
