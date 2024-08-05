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
    internal static class GoalGroupMenu
    {
        private static readonly Dictionary<string, MenuOptionHorizontal> _GoalButtons = [];
        private static Dictionary<string, bool> _GoalSettings;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu, GoalGroup goalGroup)
        {
            MenuButton backButton;

            _GoalSettings = goalGroup.customSettings;

            MenuBuilder builder = MenuUtils.CreateMenuBuilderWithBackButton("BingoSync", parentMenu, out backButton);

            builder.AddContent(default(NullContentLayout), CreateScrollbarMenuWith(parentMenu, backButton, goalGroup));

            return builder.Build();
        }

        private static Action<ContentArea> CreateScrollbarMenuWith(MenuScreen parentMenu, MenuButton backButton, GoalGroup goalGroup)
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
            int entryCount = goalGroup.customSettings.Count;
            RelLength contentHeight = new(entryCount * 105f);
            return c => c.AddScrollPaneContent(scrollbar, contentHeight, RegularGridLayout.CreateVerticalLayout(105f), AddContent(parentMenu, goalGroup));
        }

        private static Action<ContentArea> AddContent(MenuScreen parentMenu, GoalGroup goalGroup)
        {
            void ExitMenu(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(parentMenu);
            string[] onOff = ["On", "Off"];
            int lineCutoff = 30;
            return c =>
            {
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

        private static MenuSetting.RefreshSetting GetGroupSettingsLoaderFor(string goalName)
        {
            return (MenuSetting setting, bool alsoApplySetting) =>
            {
                int settingIndex = _GoalSettings[goalName] ? 0 : 1;
                _GoalButtons[goalName].SetOptionTo(settingIndex);
            };
        }

        private static MenuSetting.ApplySetting GetGroupSettingsUpdaterFor(string goalName)
        {
            return (MenuSetting setting, int settingIndex) =>
            {
                _GoalSettings[goalName] = settingIndex == 0;
            };
        }

    }
}
