using Modding.Menu;
using Modding.Menu.Config;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Linq;
using BingoSync.CustomGoals;

namespace BingoSync.ModMenu
{
    static class ProfilesManagementMenu
    {
        private static MenuScreen _ProfilesScreen;
        private static readonly Dictionary<CustomGameMode, MenuScreen> _GameModeScreens = [];
        private static MenuOptionHorizontal gameModeSelector = null;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu)
        {
            void ExitMenu(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(parentMenu);
            MenuBuilder builder = MenuUtils.CreateMenuBuilderWithBackButton("BingoSync", parentMenu, out _);

            List<CustomGameMode> gameModes = GameModesManager.CustomGameModes;
            string[] gameModeNames = gameModes.Select(x => x.InternalName).ToArray();

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
            List<CustomGameMode> gameModes = GameModesManager.CustomGameModes;
            string[] gameModeNames = gameModes?.Select(x => x.InternalName).ToArray();
            if (gameModeSelector == null) return;
            gameModeSelector.optionList = gameModeNames;
            if (GameModesManager.CustomGameModes.Count == 0)
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
            if (GameModesManager.CustomGameModes.Count == 0) return;

            int gameModeIndex = gameModeSelector.selectedOptionIndex;
            CustomGameMode gameMode = GameModesManager.CustomGameModes.ElementAt(gameModeIndex);

            if (!_GameModeScreens.ContainsKey(gameMode))
            {
                _GameModeScreens[gameMode] = ProfileMenu.CreateMenuScreen(_ProfilesScreen, gameMode);
            }
            UIManager.instance.UIGoToDynamicMenu(_GameModeScreens[gameMode]);
        }

        private static void DeleteSelectedProfile(MenuButton _)
        {
            if (GameModesManager.CustomGameModes.Count == 0) return;
            int currentIndex = gameModeSelector.selectedOptionIndex;
            GameModesManager.DeleteGameModeFile(GameModesManager.CustomGameModes.ElementAt(currentIndex).InternalName);
            GameModesManager.CustomGameModes.RemoveAt(currentIndex);
            int nextIndex = Math.Max(currentIndex - 1, 0);
            gameModeSelector.SetOptionTo(nextIndex);
            string next = GameModesManager.CustomGameModes.Count == 0 ? "No Profiles" : GameModesManager.CustomGameModes.ElementAt(nextIndex).InternalName;
            gameModeSelector.optionText.text = next;
            gameModeSelector.optionText.FontTextureChanged();
            Controller.RegenerateGameModeButtons();
            RefreshMenu();
        }

        private static void AddNewProfile(MenuButton _)
        {
            List<string> gameModeNames = GameModesManager.CustomGameModes.Select(x => x.InternalName).ToList();
            string name = "Profile ";
            int nr = 1;
            for (; gameModeNames.Contains(name + nr); ++nr) ;
            GameModesManager.CustomGameModes.Add(new CustomGameMode(name + nr, []));
            if(GameModesManager.CustomGameModes.Count == 1)
            {
                gameModeSelector.SetOptionTo(0);
                gameModeSelector.optionText.text = GameModesManager.CustomGameModes.ElementAt(0).InternalName;
                gameModeSelector.Select();
            }
            Controller.RegenerateGameModeButtons();
            RefreshMenu();
        }
    }
}
