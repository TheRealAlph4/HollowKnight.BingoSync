using Modding;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;
using BingoSync.CustomVariables;
using BingoSync.CustomVariables.Rando;
using Settings;
using UnityEngine;
using ItemChanger;
using System.Threading.Tasks;

namespace BingoSync
{
    public class BingoSync : Mod, ILocalSettings<Settings.SaveSettings>, IGlobalSettings<ModSettings>, ICustomMenuMod
    {
        new public string GetName() => "BingoSync";

        public static string version = "1.3.0.0";
        public override string GetVersion() => version;

        public static ModSettings modSettings { get; set; } = new ModSettings();

        public override void Initialize()
        {
            Hooks.Setup();
            RetryHelper.Setup(Log);
            MenuUI.Setup();
            BingoSyncClient.Setup(Log);
            BingoTracker.Setup(Log);
            BingoBoardUI.Setup(Log);
        }

        private void ShowMenu()
        {
            MenuUI.SetVisible(true);
        }

        private void HideMenu()
        {
            MenuUI.SetVisible(false);
        }

        public void OnLoadLocal(Settings.SaveSettings s)
        {
            BingoTracker.settings = s;
        }

        public Settings.SaveSettings OnSaveLocal()
        {
            return BingoTracker.settings;
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