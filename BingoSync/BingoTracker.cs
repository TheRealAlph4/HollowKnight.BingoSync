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
        private static List<string> _finishedGoals;
        private static Action<string> Log;
        public static SaveSettings settings { get; set; }

        public static void Setup(Action<string> log)
        {
            _finishedGoals = new List<string>();
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
                    _allPossibleSquares.AddRange(ser.Deserialize<List<BingoSquare>>(jsonReader));
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
            if (settings.Integers.TryGetValue(name, out previous))
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

        public static void ProcessBingo()
        {
            _allPossibleSquares.ForEach(square =>
            {
                if (IsSolved(square))
                {
                    FinishGoal(square.Name);
                }
            });
        }

        private static bool IsSolved(BingoSquare square)
        {
            if (square.Solved)
            {
                return true;
            }

            square.Requirements.ForEach(UpdateRequirement);
            switch (square.Criteria)
            {
                case RequirementCriteria.All:
                    square.Solved = square.Requirements.All(requirement => requirement.IsMet);
                    break;
                case RequirementCriteria.Some:
                    square.Solved = square.Requirements.FindAll(requirement => requirement.IsMet).Count() == square.Amount;
                    break;
            }

            return square.Solved;
        }

        private static void UpdateRequirement(BingoSquareRequirement requirement)
        {
            requirement.IsMet = false;
            if (requirement.Type == BingoRequirementType.Bool)
            {
                settings.Booleans.TryGetValue(requirement.VariableName, out var value);
                if (value == requirement.ExpectedValue)
                {
                    requirement.IsMet = true;
                }
            }
            if (requirement.Type == BingoRequirementType.Int)
            {
                int quantity = 0;
                int current, added, removed;
                if (!settings.Integers.TryGetValue(requirement.VariableName, out current)
                    || !settings.IntegersTotalAdded.TryGetValue(requirement.VariableName, out added)
                    || !settings.IntegersTotalRemoved.TryGetValue(requirement.VariableName, out removed))
                {
                    return;
                }
                switch (requirement.State)
                {
                    case BingoRequirementState.AtLeast:
                        quantity = current;
                        break;
                    case BingoRequirementState.Added:
                        quantity = added;
                        break;
                    case BingoRequirementState.Removed:
                        quantity = removed;
                        break;
                }
                if (quantity >= requirement.ExpectedQuantity)
                {
                    requirement.IsMet = true;
                }
            }
        }

        public static void ClearFinishedGoals() {
            _allPossibleSquares.ForEach(square => {
                square.Requirements.ForEach(requirement => requirement.IsMet = false);
                square.Solved = false;
            });
            _finishedGoals = new List<string>();
        }

        public static void FinishGoal(string goal)
        {
            if (BingoSyncClient.board == null || BingoSyncClient.isHidden)
            {
                return;
            }
            if (_finishedGoals.Contains(goal))
            {
                return;
            }
            if (BingoSyncClient.joined == false)
            {
                return;
            }
            
            Log($"Finishing Goal: {goal}");

            _finishedGoals.Add(goal);
            var index = BingoSyncClient.board.FindIndex(x => x.Name == goal);
            if (index >= 0)
            {
                BingoSyncClient.SelectSquare(index + 1, (ex) =>
                {
                    if (ex != null)
                    {
                        _finishedGoals.Remove(goal);
                    }
                });
            }
        }
    }

    internal class BingoSquare
    {
        public List<BingoSquareRequirement> Requirements = new List<BingoSquareRequirement>();
        public RequirementCriteria Criteria = RequirementCriteria.All;
        public int Amount = 0;
        public string Name = string.Empty;
        public bool Solved;
    }

    enum RequirementCriteria
    {
        All,
        Some,
    }

    enum BingoRequirementType
    {
        Bool,
        Int,
    }

    enum BingoRequirementState
    {
        AtLeast,
        Added,
        Removed,
    }

    internal class BingoSquareRequirement
    {
        public BingoRequirementType Type = BingoRequirementType.Bool;
        public string VariableName = string.Empty;
        public BingoRequirementState State = BingoRequirementState.AtLeast;
        public int ExpectedQuantity = 0;
        public bool ExpectedValue = false;
        public bool IsMet = false;
    }
}
