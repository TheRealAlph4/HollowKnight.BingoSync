using BingoSync.CustomGoals;
using BingoSync.GameUI;
using BingoSync.Helpers;
using Modding;
using System;
using UnityEngine;

namespace BingoSync.Interfaces
{
    public static class OrderedLoader
    {
        /// <summary>
        /// Fires when mods are able to register custom goals and gamemodes, exactly once after all mods have loaded.
        /// </summary>
        public static event EventHandler OnReadyForGoalsGameModes;
        /// <summary>
        /// Fires after OnReadyForGoalsGameModes has completed, so that mods that depend on goals/gamemodes from other mods can load.
        /// </summary>
        public static event EventHandler OnStandaloneGoalsGameModesLoaded;
        /// <summary>
        /// Fires when mods are able to register UI pages, exactly once after all mods have loaded.
        /// </summary>
        public static event EventHandler OnReadyForUIPages;
        /// <summary>
        /// Fires when mods are able to access the default session, exactly once after all mods have loaded.
        /// </summary>
        public static event EventHandler OnDefaultSessionReady;
        /// <summary>
        /// Fires when all custom goals, gamemodes and UI pages have been registered, after all other events.
        /// </summary>
        public static event EventHandler OnCompletelyLoaded;

        private static Action<string> Log;

        internal static void Setup(Action<string> log)
        {
            Log = log;
        }

        internal static void LoadInternal()
        {
            ModHooks.FinishedLoadingModsHook += OnFinishedLoadingMods;

            Controller.Setup(Log);
            Variables.Setup(Log);
            Hooks.Setup();
            RetryHelper.Setup(Log);
            BingoTracker.Setup(Log);
            GameModesManager.Setup(Log);
            MenuUI.Setup(Log);
            BingoBoardUI.Setup(Log);

            // creates a permanent GameObject which calls GlobalKeybindHelper.Update every frame
            GameObject.DontDestroyOnLoad(new GameObject("update_object", [typeof(GlobalKeybindHelper)]));
        }

        private static void OnFinishedLoadingMods()
        {
            ItemSyncInterop.Initialize(Log);

            OnReadyForGoalsGameModes?.Invoke(null, EventArgs.Empty);
            OnStandaloneGoalsGameModesLoaded?.Invoke(null, EventArgs.Empty);
            OnReadyForUIPages?.Invoke(null, EventArgs.Empty);
            OnDefaultSessionReady?.Invoke(null, EventArgs.Empty);

            OnCompletelyLoaded?.Invoke(null, EventArgs.Empty);

            GameModesManager.RefreshCustomGameModes();
            MenuUI.SetupGameModeButtons();
        }
    }
}
