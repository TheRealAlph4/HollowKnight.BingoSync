using BingoSync.GameUI;
using Satchel.BetterMenus;

namespace BingoSync.ModMenu
{
    static class AccessibilityMenu
    {
        private static Menu _AccessibilityMenu;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu)
        {
            int elementCount = 1;
            Element[] elements = new Element[elementCount];

            int elementId = 0;
            elements[elementId] = new HorizontalOption(
                name: "Color Scheme",
                description: "Higher contrast color schemes should be more distinguishable on low opacity",
                values: ["Default", "Contrast", "High Contrast"],
                applySetting: (index) =>
                {
                    Controller.GlobalSettings.ColorScheme = index;
                    ConnectionMenuUI.UpdateColorScheme();
                    BingoBoardUI.UpdateColorScheme();
                    Controller.BoardUpdate();
                },
                loadSetting: () => Controller.GlobalSettings.ColorScheme
            );
            ++elementId;
            _AccessibilityMenu = new Menu("BingoSync", elements);
            return _AccessibilityMenu.GetMenuScreen(parentMenu);
        }
    }
}
