using ItemSyncMod;
using Modding;
using MonoMod.RuntimeDetour;
using MultiWorldLib;
using System;
using System.Reflection;

namespace BingoSync.Helpers
{
    internal static class ItemSyncInterop
    {
        private const int ItemReceivedBufferFrameCount = 2;
        private static Action<string> Log;
        private static Hook itemReceivedHook;
        private static MethodInfo itemReceivedMethod;

        private static int framesSinceItemReceived = 0;

        public static bool ShouldIgnoreMark => (Controller.GlobalSettings.ItemSyncMarkSetting == Settings.ModSettings.ItemSyncMarkDelay.NoMark) && (framesSinceItemReceived > 0);
        public static bool ShouldDelayMark => (Controller.GlobalSettings.ItemSyncMarkSetting == Settings.ModSettings.ItemSyncMarkDelay.Delay) && (framesSinceItemReceived > 0);
        public static TimeSpan MarkDelay => TimeSpan.FromMilliseconds(Controller.GlobalSettings.ItemSyncMarkDelayMilliseconds);

        public static void Initialize(Action<string> log)
        {
            Log = log;

            if (ModHooks.GetMod(nameof(ItemSyncMod.ItemSyncMod)) is not Mod)
            {
                return;
            }

            ModHooks.HeroUpdateHook += OnEachFrame;

            itemReceivedMethod = typeof(ClientConnection).GetMethod("InvokeDataReceived", BindingFlags.NonPublic | BindingFlags.Instance);
            itemReceivedHook = new Hook(itemReceivedMethod, OnItemSyncInvokeDataReceived);
        }

        private static void OnItemSyncInvokeDataReceived(Action<ClientConnection, DataReceivedEvent> orig, ClientConnection self, DataReceivedEvent dataReceivedEvent)
        {
            orig(self, dataReceivedEvent);
            if (string.IsNullOrEmpty(dataReceivedEvent.Content) || dataReceivedEvent.Content[0] == '{')
            {
                return;
            }
            framesSinceItemReceived = ItemReceivedBufferFrameCount;
        }

        private static void OnEachFrame()
        {
            --framesSinceItemReceived;
            if (framesSinceItemReceived <= 0)
            {
                framesSinceItemReceived = 0;
            }
        }
    }
}
