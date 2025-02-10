using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine.UI;

namespace BingoSync.ModMenu
{
    internal static class MainMenu
    {
        private static MenuScreen _MainMenuScreen;
        private static MenuScreen _KeybindsScreen;
        private static MenuScreen _TogglesScreen;
        private static MenuScreen _DefaultsScreen;
        private static MenuScreen _BoardSettingsScreen;
        private static MenuScreen _ProfilesScreen;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu) {
            void ExitMenu(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(parentMenu);
            void GoToKeybinds(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(_KeybindsScreen);
            void GoToToggles(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(_TogglesScreen);
            void GoToDefaults(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(_DefaultsScreen);
            void GoToBoardSettings(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(_BoardSettingsScreen);
            void GoToProfiles(MenuSelectable _) {
                ProfilesManagementMenu.RefreshMenu();
                UIManager.instance.UIGoToDynamicMenu(_ProfilesScreen);
            };

            MenuBuilder mainMenuBuilder = MenuUtils.CreateMenuBuilderWithBackButton("BingoSync", parentMenu, out _);

            mainMenuBuilder.AddContent(
                    RegularGridLayout.CreateVerticalLayout(105f),
                    c =>
                    {
                        c.AddMenuButton("Keybinds", new MenuButtonConfig
                        {
                            Label = "Keybinds",
                            Proceed = true,
                            SubmitAction = GoToKeybinds,
                            CancelAction = ExitMenu,
                        })
                        .AddMenuButton("Toggles", new MenuButtonConfig
                        {
                            Label = "Toggles",
                            Proceed = true,
                            SubmitAction = GoToToggles,
                            CancelAction = ExitMenu,
                        })
                        .AddMenuButton("Defaults", new MenuButtonConfig
                        {
                            Label = "Defaults",
                            Proceed = true,
                            SubmitAction = GoToDefaults,
                            CancelAction = ExitMenu,
                        })
                        .AddMenuButton("Board Opacity", new MenuButtonConfig
                        {
                            Label = "Board Settings",
                            Proceed = true,
                            SubmitAction = GoToBoardSettings,
                            CancelAction = ExitMenu,
                        })
                        .AddMenuButton("Profiles", new MenuButtonConfig
                        {
                            Label = "Profiles",
                            Proceed = true,
                            SubmitAction = GoToProfiles,
                            CancelAction = ExitMenu,
                        });
                    });

            _MainMenuScreen = mainMenuBuilder.Build();

            _KeybindsScreen = KeybindsMenu.CreateMenuScreen(_MainMenuScreen);
            _TogglesScreen = TogglesMenu.CreateMenuScreen(_MainMenuScreen);
            _DefaultsScreen = DefaultsMenu.CreateMenuScreen(_MainMenuScreen);
            _ProfilesScreen = ProfilesManagementMenu.CreateMenuScreen(_MainMenuScreen);
            _BoardSettingsScreen = BoardSettingsMenu.CreateMenuScreen(_MainMenuScreen);

            return _MainMenuScreen;
        }

        public static void RefreshMenu()
        {
            KeybindsMenu.RefreshMenu();
            TogglesMenu.RefreshMenu();
            DefaultsMenu.RefreshMenu();
            ProfilesManagementMenu.RefreshMenu();
            BoardSettingsMenu.RefreshMenu();
        }

    }
}