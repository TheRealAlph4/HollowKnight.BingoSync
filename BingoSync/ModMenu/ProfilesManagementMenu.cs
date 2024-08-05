using Modding.Menu;
using Modding.Menu.Config;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Linq;


namespace BingoSync.ModMenu
{
    internal static class ProfilesManagementMenu
    {
        private static MenuScreen _ProfilesScreen;
        private static readonly Dictionary<CustomGameMode, MenuScreen> _GameModeScreens = [];
        private static MenuOptionHorizontal gameModeSelector = null;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu)
        {
            void ExitMenu(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(parentMenu);
            MenuBuilder builder = MenuUtils.CreateMenuBuilderWithBackButton("BingoSync", parentMenu, out _);

            List<CustomGameMode> gameModes = BingoSync.modSettings.CustomGameModes;
            string[] gameModeNames = gameModes.Select(x => x.Name).ToArray();

            builder.AddContent(
                RegularGridLayout.CreateVerticalLayout(105f),
                c =>
                {
                    c.AddHorizontalOption("Select Gamemode", new HorizontalOptionConfig
                    {
                        Label = "Select Gamemode",
                        Options = gameModeNames,
                        CancelAction = ExitMenu,
                        ApplySetting = SwitchedGameMode,
                        Style = HorizontalOptionStyle.VanillaStyle
                    }, out gameModeSelector);

                    c.AddMenuButton("Edit Profile", new MenuButtonConfig
                    {
                        Label = "Edit Profile",
                        CancelAction = ExitMenu,
                        SubmitAction = EditSelectedProfile
                    });

                    c.AddMenuButton("Delete Profile", new MenuButtonConfig
                    {
                        Label = "Delete Profile",
                        CancelAction = ExitMenu,
                        SubmitAction = DeleteSelectedProfile
                    });

                    c.AddMenuButton("Add New Profile", new MenuButtonConfig
                    {
                        Label = "Add New Profile",
                        CancelAction = ExitMenu,
                        SubmitAction = AddNewProfile
                    });
                }
            );

            _ProfilesScreen = builder.Build();

            RefreshMenu();

            return _ProfilesScreen;
        }

        public static void RefreshMenu()
        {
            List<CustomGameMode> gameModes = BingoSync.modSettings.CustomGameModes;
            string[] gameModeNames = gameModes?.Select(x => x.Name).ToArray();
            if (gameModeSelector == null) return;
            gameModeSelector.optionList = gameModeNames;
            if (BingoSync.modSettings.CustomGameModes.Count == 0)
            {
                gameModeSelector.optionText.text = "No Profiles";
            }
        }

        private static void SwitchedGameMode(MenuSetting self, int settingIndex)
        {
            RefreshMenu();
        }

        private static void EditSelectedProfile(MenuButton _)
        {
            if (BingoSync.modSettings.CustomGameModes.Count == 0) return;

            int gameModeIndex = gameModeSelector.selectedOptionIndex;
            CustomGameMode gameMode = BingoSync.modSettings.CustomGameModes.ElementAt(gameModeIndex);

            if (!_GameModeScreens.ContainsKey(gameMode))
            {
                _GameModeScreens[gameMode] = ProfileMenu.CreateMenuScreen(_ProfilesScreen, gameMode);
            }
            UIManager.instance.UIGoToDynamicMenu(_GameModeScreens[gameMode]);
        }

        private static void DeleteSelectedProfile(MenuButton _)
        {
            if (BingoSync.modSettings.CustomGameModes.Count == 0) return;
            int currentIndex = gameModeSelector.selectedOptionIndex;
            BingoSync.modSettings.CustomGameModes.RemoveAt(currentIndex);
            int nextIndex = Math.Max(currentIndex - 1, 0);
            gameModeSelector.SetOptionTo(nextIndex);
            string next = BingoSync.modSettings.CustomGameModes.Count == 0 ? "No Profiles" : BingoSync.modSettings.CustomGameModes.ElementAt(nextIndex).Name;
            gameModeSelector.optionText.text = next;
            gameModeSelector.optionText.FontTextureChanged();
            RefreshMenu();
        }

        private static void AddNewProfile(MenuButton _)
        {
            List<string> gameModeNames = BingoSync.modSettings.CustomGameModes.Select(x => x.Name).ToList();
            string name = "Profile ";
            int nr = 1;
            for (; gameModeNames.Contains(name + nr); ++nr) ;
            BingoSync.modSettings.CustomGameModes.Add(new CustomGameMode(name + nr, []));
            if(BingoSync.modSettings.CustomGameModes.Count == 1)
            {
                gameModeSelector.SetOptionTo(0);
                gameModeSelector.optionText.text = BingoSync.modSettings.CustomGameModes.ElementAt(0).Name;
                gameModeSelector.Select();
            }
            Modding.Logger.Log($"Added {name + nr}");
            RefreshMenu();
        }
    }
}
