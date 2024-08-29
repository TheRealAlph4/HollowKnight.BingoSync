using Modding;
using BingoSync.ModMenu;
using BingoSync.Settings;
using BingoSync.CustomGoals;
using BingoSync.GameUI;
using UnityEngine;
using BingoSync.Helpers;

namespace BingoSync
{
    public class BingoSync : Mod, ILocalSettings<SaveSettings>, IGlobalSettings<ModSettings>, ICustomMenuMod
    {
        new public string GetName() => "BingoSync";

        public static string version = "1.3.0.0";
        public override string GetVersion() => version;

        public override int LoadPriority() => 0;

        public override void Initialize()
        {
            Controller.Setup(Log);
            Variables.Setup(Log);
            Hooks.Setup();
            RetryHelper.Setup(Log);
            MenuUI.Setup(Log);
            BingoSyncClient.Setup(Log);
            BingoTracker.Setup(Log);
            BingoBoardUI.Setup(Log);
            GameModesManager.Setup(Log);

            ModHooks.FinishedLoadingModsHook += Controller.AfterGoalPacksLoaded;
            // creates a permanent GameObject which calls GlobalKeybindHelper.Update every frame
            GameObject.DontDestroyOnLoad(new GameObject("update_object", [typeof(GlobalKeybindHelper)]));
       }

        public static void ShowMenu()
        {
            Controller.MenuIsVisible = true;
        }

        public static void HideMenu()
        {
            Controller.MenuIsVisible = false;
        }

        public void OnLoadLocal(SaveSettings s)
        {
            BingoTracker.Settings = s;
        }

        public SaveSettings OnSaveLocal()
        {
            return BingoTracker.Settings;
        }

        public void OnLoadGlobal(ModSettings s)
        {
            Controller.GlobalSettings = s;
            MenuUI.LoadDefaults();
            MainMenu.RefreshMenu();
        }

        public ModSettings OnSaveGlobal()
        {
            return Controller.GlobalSettings;
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) {
            MenuScreen menu = MainMenu.CreateMenuScreen(modListMenu);
            MainMenu.RefreshMenu();
            return menu;
        }

        public bool ToggleButtonInsideMenu => false;
    }
}