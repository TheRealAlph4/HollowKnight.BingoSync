using Modding;
using BingoSync.ModMenu;
using BingoSync.Settings;
using BingoSync.CustomGoals;
using BingoSync.GameUI;
using BingoSync.Interfaces;

namespace BingoSync
{
    public class BingoSync : Mod, ILocalSettings<SaveSettings>, IGlobalSettings<ModSettings>, ICustomMenuMod
    {
        new public string GetName() => "BingoSync";

        public static string version = "1.4.3.0";
        public override string GetVersion() => version;

        public override void Initialize()
        {
            OrderedLoader.Setup(Log);
            OrderedLoader.LoadInternal();
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
            GoalCompletionTracker.Settings = s;
        }

        public SaveSettings OnSaveLocal()
        {
            return GoalCompletionTracker.Settings;
        }

        public void OnLoadGlobal(ModSettings s)
        {
            Controller.GlobalSettings = s;

            // transition from having CustomGameModes in the global settings to separate files for each
            // it'll be removed at some point after a few updates, after everyone transitions
            Controller.MoveGameModesFromSettings();

            GameModesManager.LoadCustomGameModesFromFiles();
            MenuUI.LoadDefaults();
            MainMenu.RefreshMenu();
        }

        public ModSettings OnSaveGlobal()
        {
            GameModesManager.SaveCustomGameModesToFiles();
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