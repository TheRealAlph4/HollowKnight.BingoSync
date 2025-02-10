using Satchel.BetterMenus;
using System;
using System.Collections.Generic;
using System.Linq;
using static BingoSync.Settings.ModSettings;

namespace BingoSync.ModMenu
{
    static class BoardSettingsMenu
    {
        private static Menu _BoardSettingsMenu;
        private static List<CustomSlider> _Sliders;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu)
        {
            int alphaCount = Controller.GlobalSettings.BoardAlphas.Count;
            int elementCount = alphaCount + 1;
            Element[] elements = new Element[elementCount];

            int elementId = 0;
            elements[elementId] = new HorizontalOption(
                name: "Highlight Type",
                description: "Which sprite to use for square highlighting",
                values: Enum.GetNames(typeof(HighlightType)),
                applySetting: (index) =>
                {
                    Controller.GlobalSettings.SelectedHighlightSprite = (HighlightType)index;
                    Controller.BoardUpdate();
                },
                loadSetting: () => (int)Controller.GlobalSettings.SelectedHighlightSprite
            );
            ++elementId;

            _Sliders = [];
            for (int i = 0; i < alphaCount; ++i)
            {
                _Sliders.Add(new CustomSlider(
                    name: $"Opacity preset {i + 1}",
                    storeValue: MakeValueStoreAction(i),
                    loadValue: MakeValueLoadAction(i),
                    minValue: 0,
                    maxValue: 100,
                    wholeNumbers: true
                ));
                elements[elementId] = _Sliders[i];
                ++elementId;
            }
            RefreshMenu();
            _BoardSettingsMenu = new Menu("Opacity Sliders", elements);
            return _BoardSettingsMenu.GetMenuScreen(parentMenu);
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

        public static void RefreshMenu()
        {
            for (int i = 0; i < _Sliders.Count; ++i)
            {
                _Sliders[i].Name = $"Opacity preset {i + 1}" + (i == Controller.GlobalSettings.BoardAlphaIndex ? " (active)" : "");
            }
            _BoardSettingsMenu.Update();
        }
    }
}
