using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine.UI;

namespace BingoSync.ModMenu
{
    internal static class TogglesMenu
    {
        private static MenuOptionHorizontal revealCardOnStartSelector;
        private static MenuOptionHorizontal unmarkGoalsSelector;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu)
        {
            void ExitMenu(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(parentMenu);
            MenuBuilder builder = MenuUtils.CreateMenuBuilderWithBackButton("BingoSync", parentMenu, out _);

            builder.AddContent(
                RegularGridLayout.CreateVerticalLayout(105f),
                c =>
                {
                    c.AddHorizontalOption("Reveal Card On Start", new HorizontalOptionConfig
                    {
                        Label = "Reveal Card on Game Start",
                        Options = ["No", "Yes"],
                        CancelAction = ExitMenu,
                        ApplySetting = (menu, index) =>
                        {
                            BingoSync.modSettings.RevealCardOnGameStart = (index == 1);
                        },
                        RefreshSetting = (menu, alsoApply) =>
                        {
                            var shouldRevealOnStart = BingoSync.modSettings.RevealCardOnGameStart;
                            menu.optionList.SetOptionTo(shouldRevealOnStart ? 1 : 0);
                        }
                    }, out revealCardOnStartSelector)
                    .AddHorizontalOption("Unmark Goals", new HorizontalOptionConfig
                    {
                        Label = "Unmark Goals",
                        Description = new DescriptionInfo
                        {
                            Text = "Some goals will be unmarked if their conditions are no longer met. WARNING: Can cause board inconsistencies on rare situations"
                        },
                        Options = ["No", "Yes"],
                        CancelAction = ExitMenu,
                        ApplySetting = (menu, index) =>
                        {
                            BingoSync.modSettings.UnmarkGoals = (index == 1);
                        },
                        RefreshSetting = (menu, alsoApply) =>
                        {
                            var shouldUnmarkGoals = BingoSync.modSettings.UnmarkGoals;
                            menu.optionList.SetOptionTo(shouldUnmarkGoals ? 1 : 0);
                        }
                    }, out unmarkGoalsSelector);

                }
            );

            return builder.Build();
        }

        public static void RefreshMenu()
        {
            revealCardOnStartSelector?.menuSetting?.RefreshValueFromGameSettings();
            unmarkGoalsSelector?.menuSetting?.RefreshValueFromGameSettings();
        }
    }
}
