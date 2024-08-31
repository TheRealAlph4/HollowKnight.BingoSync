using BingoSync.Settings;
using Modding.Menu.Config;
using Modding.Menu;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

namespace BingoSync.ModMenu
{
    class GoalGroupMenu
    {
        private readonly Dictionary<string, MenuOptionHorizontal> _GoalButtons = [];
        private GoalGroup _GoalGroup;
        private MenuOptionHorizontal _ToggleAllButton;

        public MenuScreen CreateMenuScreen(MenuScreen parentMenu, GoalGroup goalGroup)
        {
            MenuButton backButton;

            _GoalGroup = goalGroup;

            MenuBuilder builder = MenuUtils.CreateMenuBuilderWithBackButton("BingoSync", parentMenu, out backButton);

            builder.AddContent(default(NullContentLayout), CreateScrollbarMenuWith(parentMenu, backButton, goalGroup));

            return builder.Build();
        }

        private Action<ContentArea> CreateScrollbarMenuWith(MenuScreen parentMenu, MenuButton backButton, GoalGroup goalGroup)
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
            int entryCount = goalGroup.customSettings.Count + 1;
            RelLength contentHeight = new(entryCount * 105f);
            return c => c.AddScrollPaneContent(scrollbar, contentHeight, RegularGridLayout.CreateVerticalLayout(105f), AddContent(parentMenu, goalGroup));
        }

        private Action<ContentArea> AddContent(MenuScreen parentMenu, GoalGroup goalGroup)
        {
            void ExitMenu(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(parentMenu);
            string[] onOff = ["On", "Off"];
            int lineCutoff = 30;
            return c =>
            {
                c.AddHorizontalOption("Toggle all", new HorizontalOptionConfig()
                {
                    Label = "Toggle all",
                    Options = onOff,
                    CancelAction = ExitMenu,
                    ApplySetting = ToggleAll,
                }, out _ToggleAllButton);
                foreach (var goalInfo in goalGroup.customSettings)
                {
                    MenuOptionHorizontal option;

                    string goalName = goalInfo.Key;
                    bool goalIsOn = goalInfo.Value;

                    string displayName = goalName.Count() > lineCutoff ? goalName.Substring(0, lineCutoff) : goalName;
                    string description = goalName.Count() > lineCutoff ? goalName.Substring(lineCutoff) : string.Empty;
                    c.AddHorizontalOption(displayName, new HorizontalOptionConfig()
                    {
                        Label = displayName,
                        Options = onOff,
                        Description = new DescriptionInfo()
                        {
                            Text = description
                        },
                        CancelAction = ExitMenu,
                        RefreshSetting = GetGroupSettingsLoaderFor(goalName),
                        ApplySetting = GetGroupSettingsUpdaterFor(goalName),
                    }, out option);

                    _GoalButtons[goalName] = option;
                    option.menuSetting.RefreshValueFromGameSettings();
                }
            };
        }

        private MenuSetting.RefreshSetting GetGroupSettingsLoaderFor(string goalName)
        {
            return (MenuSetting setting, bool alsoApplySetting) =>
            {
                int settingIndex = _GoalGroup.customSettings[goalName] ? 0 : 1;
                _GoalButtons[goalName].SetOptionTo(settingIndex);
            };
        }

        private MenuSetting.ApplySetting GetGroupSettingsUpdaterFor(string goalName)
        {
            return (MenuSetting setting, int settingIndex) =>
            {
                _GoalGroup.customSettings[goalName] = settingIndex == 0;
            };
        }

        private void ToggleAll(MenuSetting setting, int settingIndex)
        {
            _ToggleAllButton.SetOptionTo(settingIndex);
            foreach (MenuOptionHorizontal option in _GoalButtons.Values)
            {
                option.SetOptionTo(settingIndex);
            }
            foreach (string goal in _GoalGroup.customSettings.Keys.ToList())
            {
                _GoalGroup.customSettings[goal] = settingIndex == 0;
            }
        }
    }
}
