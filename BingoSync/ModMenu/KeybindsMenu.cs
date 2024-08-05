using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine.UI;

namespace BingoSync.ModMenu
{
    internal static class KeybindsMenu
    {
        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu)
        {
            void ExitMenu(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(parentMenu);
            MenuBuilder builder = MenuUtils.CreateMenuBuilderWithBackButton("BingoSync", parentMenu, out _);

            builder.AddContent(
                RegularGridLayout.CreateVerticalLayout(105f),
                c =>
                {
                    c.AddKeybind("Toggle Board", BingoSync.modSettings.Keybinds.ToggleBoard, new KeybindConfig
                    {
                        Label = "Toggle Board",
                        CancelAction = ExitMenu,
                    })
                    .AddKeybind("Cycle Board Opacity", BingoSync.modSettings.Keybinds.CycleBoardOpacity, new KeybindConfig
                    {
                        Label = "Cycle Board Opacity",
                        CancelAction = ExitMenu,
                    })
                    .AddKeybind("Hide Menu", BingoSync.modSettings.Keybinds.HideMenu, new KeybindConfig
                    {
                        Label = "Hide Menu",
                        CancelAction = ExitMenu,
                    })
                    .AddKeybind("Reveal Card", BingoSync.modSettings.Keybinds.RevealCard, new KeybindConfig
                    {
                        Label = "Reveal Card",
                        CancelAction = ExitMenu,
                    });
                }
            );

            return builder.Build();
        }

        public static void RefreshMenu()
        {
        }
    }
}
