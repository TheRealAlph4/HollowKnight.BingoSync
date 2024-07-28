using Modding;
using Settings;
using System.Collections.Generic;

namespace BingoSync
{
    public class BingoSync : Mod, ILocalSettings<Settings.SaveSettings>, IGlobalSettings<ModSettings>, ICustomMenuMod
    {
        new public string GetName() => "BingoSync";

        public static string version = "1.3.0.0";
        public override string GetVersion() => version;

        public override int LoadPriority() => 0;

        public static ModSettings modSettings { get; set; } = new ModSettings();

        public override void Initialize()
        {
            Controller.Setup(Log);
            Variables.Setup(Log);
            Hooks.Setup();
            RetryHelper.Setup(Log);
            MenuUI.Setup();
            BingoSyncClient.Setup(Log);
            BingoTracker.Setup(Log);
            BingoBoardUI.Setup(Log);
            GameModesManager.Setup(Log);

            ModHooks.FinishedLoadingModsHook += MenuUI.SetupGameModeButtons;
        }

        public static void ShowMenu()
        {
            Controller.MenuIsVisible = true;
        }

        public static void HideMenu()
        {
            Controller.MenuIsVisible = false;
        }

        public static void AddGameMode(GameMode gameMode)
        {
            GameModesManager.AddGameMode(gameMode);
        }

        public static void RegisterGoalsForCustom(string groupName, Dictionary<string, BingoGoal> goals)
        {
            GameModesManager.RegisterGoalsForCustom(groupName, goals);
        }

        public static Dictionary<string, BingoGoal> GetVanillaGoals()
        {
            return GameModesManager.GetVanillaGoals();
        }


        public void OnLoadLocal(Settings.SaveSettings s)
        {
            BingoTracker.Settings = s;
        }

        public Settings.SaveSettings OnSaveLocal()
        {
            return BingoTracker.Settings;
        }

        public void OnLoadGlobal(ModSettings s)
        {
            modSettings = s;
            MenuUI.LoadDefaults();
            ModMenu.RefreshMenu();
        }

        public ModSettings OnSaveGlobal()
        {
            return modSettings;
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) {
            var menu = ModMenu.CreateMenuScreen(modListMenu).Build();
            ModMenu.RefreshMenu();
            return menu;
        }

        public bool ToggleButtonInsideMenu => false;
    }
}