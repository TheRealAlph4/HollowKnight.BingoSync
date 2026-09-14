using Satchel.BetterMenus;
using static BingoSync.Settings.ModSettings;

namespace BingoSync.ModMenu
{
    internal static class GeneralMenu
    {
        private static HorizontalOption revealBoardOnStartSelector;
        private static HorizontalOption revealBoardOnOthersRevealSelector;
        private static HorizontalOption markCompletedOnNewBoardSelector;
        private static HorizontalOption markCompletedOnLoadSavefileSelector;
        private static HorizontalOption unmarkGoalsSelector;
        private static HorizontalOption itemSyncSelector;
        private static CustomSlider itemSyncDelay;

        private static Menu _TogglesMenu;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu)
        {
            revealBoardOnStartSelector = new HorizontalOption(
                name: "Reveal Board On Start",
                description: "Reveal the board when starting a new savefile",
                values: ["No", "Yes"],
                applySetting: (index) => Controller.GlobalSettings.RevealBoardOnGameStart = (index == 1),
                loadSetting: () => Controller.GlobalSettings.RevealBoardOnGameStart ? 1 : 0
            );

            revealBoardOnOthersRevealSelector = new HorizontalOption(
                name: "Reveal With Others",
                description: "Reveal the board, when notified that another player did",
                values: ["No", "Yes"],
                applySetting: (index) => Controller.GlobalSettings.RevealBoardWhenOthersReveal = (index == 1),
                loadSetting: () => Controller.GlobalSettings.RevealBoardWhenOthersReveal ? 1 : 0
            );

            markCompletedOnNewBoardSelector = new HorizontalOption(
                name: "Mark Goals On New Board",
                description: "Mark all completed goals when a new board is received/revealed",
                values: ["No", "Yes"],
                applySetting: (index) => Controller.GlobalSettings.MarkCompletedGoalsOnNewBoardReceived = (index == 1),
                loadSetting: () => Controller.GlobalSettings.MarkCompletedGoalsOnNewBoardReceived ? 1 : 0
            );

            markCompletedOnLoadSavefileSelector = new HorizontalOption(
                name: "Mark Goals On Load",
                description: "Mark all completed goals when loading a savefile",
                values: ["No", "Yes"],
                applySetting: (index) => Controller.GlobalSettings.MarkCompletedGoalsOnLoadSavefile = (index == 1),
                loadSetting: () => Controller.GlobalSettings.MarkCompletedGoalsOnLoadSavefile ? 1 : 0
            );

            unmarkGoalsSelector = new HorizontalOption(
                name: "Unmark Goals",
                description: "Some goals will be unmarked if their conditions are no longer met. WARNING: Can cause board inconsistencies on rare situations",
                values: ["No", "Yes"],
                applySetting: (index) =>
                {
                    Controller.GlobalSettings.DefaultSessionUnmarkGoals = (index == 1);
                    Controller.DefaultSession.IsAutoUnmarking = (index == 1);
                },
                loadSetting: () => Controller.GlobalSettings.DefaultSessionUnmarkGoals ? 1 : 0
            );

            itemSyncSelector = new HorizontalOption(
                name: "Marks from ItemSync",
                description: "What to do when a goal gets completed by something received through ItemSync.",
                values: ["Mark", "Delay", "Ignore"],
                applySetting: (index) => Controller.GlobalSettings.ItemSyncMarkSetting = (ItemSyncMarkDelay)index,
                loadSetting: () => (int)Controller.GlobalSettings.ItemSyncMarkSetting
            );

            itemSyncDelay = new CustomSlider(
                    name: "ItemSync Delay (seconds)",
                    storeValue: value => Controller.GlobalSettings.ItemSyncMarkDelayMilliseconds = (int) (value * 1000),
                    loadValue: () => (float) Controller.GlobalSettings.ItemSyncMarkDelayMilliseconds / 1000,
                    minValue: 0f,
                    maxValue: 2.5f,
                    wholeNumbers: false
                );

            Element[] elements =
            [
                revealBoardOnStartSelector,
                revealBoardOnOthersRevealSelector,
                markCompletedOnNewBoardSelector,
                markCompletedOnLoadSavefileSelector,
                unmarkGoalsSelector,
                itemSyncSelector,
                itemSyncDelay,
            ];

            _TogglesMenu = new Menu("BingoSync", elements);
            return _TogglesMenu.GetMenuScreen(parentMenu);
        }

        public static void RefreshMenu()
        {
            revealBoardOnStartSelector?.LoadSetting();
            revealBoardOnOthersRevealSelector?.LoadSetting();
            unmarkGoalsSelector?.LoadSetting();
            itemSyncSelector?.LoadSetting();
            itemSyncDelay?.LoadValue();
        }
    }
}
