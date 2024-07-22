using System;
using System.Collections.Generic;

namespace BingoSync
{
    public static class Variables
    {
        private static Action<string> Log;
        private static readonly HashSet<string> trackedVariables = [];

        public static void Setup(Action<string> log)
        {
            Log = log;
        }

        public static void Track(string variableName)
        {
            trackedVariables.Add(variableName);
        }

        public static void Untrack(string variableName)
        {
            trackedVariables.Remove(variableName);
        }

        public static int GetInteger(string variableName)
        {
            int value = BingoTracker.GetInteger(variableName);
            if (trackedVariables.Contains(variableName))
            {
                Log($"GetInteger: {variableName} = {value}");
            }
            return value;
        }

        public static void UpdateInteger(string variableName, int value)
        {
            if (trackedVariables.Contains(variableName))
            {
                Log($"UpdateInteger: {variableName} = {value}");
            }
            BingoTracker.UpdateInteger(variableName, value);
        }

        public static void SetInteger(string variableName, int value)
        {
            UpdateInteger(variableName, value);
        }

        public static void Increment(string variableName, int amount = 1)
        {
            SetInteger(variableName, GetInteger(variableName) + amount);
        }

        public static bool GetBoolean(string variableName)
        {
            bool value = BingoTracker.GetBoolean(variableName);
            if (trackedVariables.Contains(variableName))
            {
                Log($"GetBoolean: {variableName} = {value}");
            }
            return value;
        }

        public static void UpdateBoolean(string variableName, bool value)
        {
            if (trackedVariables.Contains(variableName))
            {
                Log($"UpdateBoolean: {variableName} = {value}");
            }
            BingoTracker.UpdateBoolean(variableName, value);
        }

        public static void SetBoolean(string variableName, bool value)
        {
            UpdateBoolean(variableName, value);
        }
    }
}
