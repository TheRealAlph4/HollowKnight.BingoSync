using BingoSync.Settings;
using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace BingoSync.ModMenu
{
    internal static class ProfileMenu
    {
        private static MenuScreen _ProfilesScreen;
        private static readonly Dictionary<GoalGroup, MenuScreen> _GroupScreens = [];
        private static readonly Dictionary<GoalGroup, MenuOptionHorizontal> _GroupOptions = [];

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu, CustomGameMode gameMode)
        {
            MenuButton backButton;
            MenuBuilder builder = MenuUtils.CreateMenuBuilderWithBackButton("BingoSync", parentMenu, out backButton);

            builder.AddContent(default(NullContentLayout), CreateScrollbarMenuWith(parentMenu, backButton, gameMode));

            _ProfilesScreen = builder.Build();

            return _ProfilesScreen;
        }

        private static Action<ContentArea> CreateScrollbarMenuWith(MenuScreen parentMenu, MenuButton backButton, CustomGameMode gameMode)
        {
            ScrollbarConfig scrollbar = new()
            {
                CancelAction = delegate
                {
                    UIManager.instance.UIGoToDynamicMenu(parentMenu);
                },
                Navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = backButton,
                    selectOnDown = backButton
                },
                Position = new AnchoredPosition
                {
                    ChildAnchor = new Vector2(0f, 1f),
                    ParentAnchor = new Vector2(1f, 1f),
                    Offset = new Vector2(-310f, 0f)
                }
            };
            int entryCount = 2 * gameMode.GetGoalSettings().Count;
            RelLength contentHeight = new(entryCount * 105f);
            return c => c.AddScrollPaneContent(scrollbar, contentHeight, RegularGridLayout.CreateVerticalLayout(105f), AddContent(parentMenu, gameMode));
        }

        private static Action<ContentArea> AddContent(MenuScreen parentMenu, CustomGameMode gameMode)
        {
            void ExitMenu(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(parentMenu);
            string[] allOnOffCustom = ["All on", "All off", "Custom"];
            return c =>
            {
                foreach (GoalGroup group in gameMode.GetGoalSettings())
                {
                    MenuOptionHorizontal option;
                    c.AddHorizontalOption(group.Name, new HorizontalOptionConfig()
                    {
                        Label = group.Name,
                        Options = allOnOffCustom,
                        Description = new DescriptionInfo()
                        {
                            Text = string.Empty
                        },
                        CancelAction = ExitMenu,
                        RefreshSetting = GetGroupSettingsLoaderFor(group),
                        ApplySetting = GetGroupSettingsUpdaterFor(group),
                    }, out option);

                    _GroupOptions[group] = option;
                    option.menuSetting.RefreshValueFromGameSettings();

                    c.AddMenuButton("Edit", new MenuButtonConfig
                    {
                        Label = "Edit",
                        Description = new DescriptionInfo
                        {
                            Text = string.Empty
                        },
                        CancelAction = ExitMenu,
                        SubmitAction = EditButtonClickedFor(group)
                    });
                }
            };
        }

        private static MenuSetting.RefreshSetting GetGroupSettingsLoaderFor(GoalGroup goalGroup)
        {
            return (MenuSetting setting, bool alsoApplySetting) =>
            {
                int settingIndex = 2;
                if(!goalGroup.CustomSettingsOn)
                {
                    settingIndex = goalGroup.AllGoalsOn ? 0 : 1;
                }
                _GroupOptions[goalGroup].SetOptionTo(settingIndex);
            };
        }

        private static MenuSetting.ApplySetting GetGroupSettingsUpdaterFor(GoalGroup goalGroup)
        {
            return (MenuSetting setting, int settingIndex) =>
            {
                goalGroup.CustomSettingsOn = false;
                switch(settingIndex)
                {
                    case 0: goalGroup.AllGoalsOn = true; break;
                    case 1: goalGroup.AllGoalsOn = false; break;
                    case 2:
                    default:
                        goalGroup.CustomSettingsOn = true; break;
                }
            };
        }

        private static Action<MenuButton> EditButtonClickedFor(GoalGroup goalGroup)
        {
            return _ =>
            {
                if(!_GroupScreens.ContainsKey(goalGroup))
                {
                    _GroupScreens[goalGroup] = GoalGroupMenu.CreateMenuScreen(_ProfilesScreen, goalGroup);
                }
                UIManager.instance.UIGoToDynamicMenu(_GroupScreens[goalGroup]);
            };
        }

        public static void RefreshMenu()
        {
        }
    }
}
