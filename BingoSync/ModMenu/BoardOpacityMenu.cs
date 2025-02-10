using Satchel.BetterMenus;
using System;

namespace BingoSync.ModMenu
{
    static class BoardOpacityMenu
    {
        private static Menu _BoardOpacityMenu;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu)
        {
            int alphaCount = Controller.GlobalSettings.BoardAlphas.Count;
            Element[] sliders = new Element[alphaCount];
            for (int i = 0; i < alphaCount; ++i)
            {
                sliders[i] = new CustomSlider(
                    name: $"Opacity preset {i + 1}",
                    storeValue: MakeValueStoreAction(i),
                    loadValue: MakeValueLoadAction(i),
                    minValue: 0,
                    maxValue: 100,
                    wholeNumbers: true
                );
            }
            _BoardOpacityMenu = new Menu("Opacity Sliders", sliders);
            return _BoardOpacityMenu.GetMenuScreen(parentMenu);
        }

        public static Action<float> MakeValueStoreAction(int id)
        {
            return (val) =>
            {
                Controller.GlobalSettings.BoardAlphas[id] = val / 100;
                Controller.RefreshBoardOpacity();
            };
        }

        public static Func<float> MakeValueLoadAction(int id)
        {
            return () =>
            {
                return Controller.GlobalSettings.BoardAlphas[id] * 100;
            };
        }
    }
}
