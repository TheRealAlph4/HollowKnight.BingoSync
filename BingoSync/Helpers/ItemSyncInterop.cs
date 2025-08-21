using BingoSync.Sessions;
using ItemSyncMod;
using Modding;
using MonoMod.RuntimeDetour;
using MultiWorldLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BingoSync.Helpers
{
    internal static class ItemSyncInterop
    {
        private static Action<string> Log;
        private static Hook itemReceivedHook;
        private static MethodInfo itemReceivedMethod;

        private static readonly List<Session> knownSessions = [];
        public static void AddSession(Session session) { knownSessions.Add(session); }
        public static void RemoveSession(Session session) { knownSessions.Remove(session); }

        public static TimeSpan MarkDelay => TimeSpan.FromMilliseconds(Controller.GlobalSettings.ItemSyncMarkDelayMilliseconds);

        public static void Initialize(Action<string> log)
        {
            Log = log;

            if (ModHooks.GetMod(nameof(ItemSyncMod.ItemSyncMod)) is not Mod)
            {
                return;
            }

            itemReceivedMethod = typeof(ClientConnection).GetMethod("InvokeDataReceived", BindingFlags.NonPublic | BindingFlags.Instance);
            itemReceivedHook = new Hook(itemReceivedMethod, OnItemSyncInvokeDataReceived);
        }

        private static void OnItemSyncInvokeDataReceived(Action<ClientConnection, DataReceivedEvent> orig, ClientConnection self, DataReceivedEvent dataReceivedEvent)
        {
            foreach (Session session in knownSessions)
            {
                GoalCompletionTracker.UpdateAllKnownSquares(session, false);
            }
            orig(self, dataReceivedEvent);
            if (string.IsNullOrEmpty(dataReceivedEvent.Content) || dataReceivedEvent.Content[0] == '{')
            {
                return;
            }
            foreach (Session session in knownSessions)
            {
                GoalCompletionTracker.UpdateAllKnownSquares(session, true);
            }
        }
    }
}
