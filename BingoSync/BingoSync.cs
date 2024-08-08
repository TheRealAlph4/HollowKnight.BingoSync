using Modding;
using BingoSync.ModMenu;
using BingoSync.Settings;

namespace BingoSync
{
    public class BingoSync : Mod, ILocalSettings<SaveSettings>, IGlobalSettings<ModSettings>, ICustomMenuMod
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

            ModHooks.FinishedLoadingModsHook += Controller.AfterGoalPacksLoaded;
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
            modSettings = s;
            MenuUI.LoadDefaults();
            MainMenu.RefreshMenu();
        }

        public ModSettings OnSaveGlobal()
        {
            return modSettings;
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) {
            MenuScreen menu = MainMenu.CreateMenuScreen(modListMenu);
            MainMenu.RefreshMenu();
            return menu;
        }

        public bool ToggleButtonInsideMenu => false;
    }
}