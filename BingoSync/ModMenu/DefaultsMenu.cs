using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine.UI;

namespace BingoSync.ModMenu
{
    internal static class DefaultsMenu
    {
        private static MenuButton saveNicknameButton;
        private static MenuButton savePasswordButton;
        private static MenuButton saveColorButton;

        public static MenuScreen CreateMenuScreen(MenuScreen parentMenu)
        {
            void ExitMenu(MenuSelectable _) => UIManager.instance.UIGoToDynamicMenu(parentMenu);
            MenuBuilder builder = MenuUtils.CreateMenuBuilderWithBackButton("BingoSync", parentMenu, out _);

            builder.AddContent(
                RegularGridLayout.CreateVerticalLayout(105f),
                c =>
                {
                    c.AddMenuButton("Save Nickname", new MenuButtonConfig
                    {
                        Label = "Save Default Nickname",
                        Description = new DescriptionInfo
                        {
                            Text = GetSaveNicknameDescriptionText()
                        },
                        CancelAction = ExitMenu,
                        SubmitAction = _ =>
                        {
                            Controller.RefreshDefaultsFromUI();
                            BingoSync.modSettings.DefaultNickname = Controller.RoomNickname;
                            RefreshMenu();
                        }
                    }, out saveNicknameButton)
                    .AddMenuButton("Save Password", new MenuButtonConfig
                    {
                        Label = "Save Default Password",
                        Description = new DescriptionInfo
                        {
                            Text = GetSavePasswordDescriptionText()
                        },
                        CancelAction = ExitMenu,
                        SubmitAction = _ =>
                        {
                            Controller.RefreshDefaultsFromUI();
                            BingoSync.modSettings.DefaultPassword = Controller.RoomPassword;
                            RefreshMenu();
                        }
                    }, out savePasswordButton)
                    .AddMenuButton("Save Color", new MenuButtonConfig
                    {
                        Label = "Save Default Color",
                        Description = new DescriptionInfo
                        {
                            Text = GetSaveColorDescriptionText()
                        },
                        CancelAction = ExitMenu,
                        SubmitAction = _ =>
                        {
                            BingoSync.modSettings.DefaultColor = Controller.RoomColor;
                            RefreshMenu();
                        }
                    }, out saveColorButton);
                }
            );

            return builder.Build();
        }

        public static void RefreshMenu()
        {
            var nickDesc = saveNicknameButton?.descriptionText?.GetComponentInParent<Text>();
            if (nickDesc != null)
            {
                nickDesc.text = GetSaveNicknameDescriptionText();
            }

            var passDesc = savePasswordButton?.descriptionText?.GetComponentInParent<Text>();
            if (passDesc != null)
            {
                passDesc.text = GetSavePasswordDescriptionText();
            }

            var colorDesc = saveColorButton?.descriptionText?.GetComponentInParent<Text>();
            if (colorDesc != null)
            {
                colorDesc.text = GetSaveColorDescriptionText();
            }
        }
        private static string GetSaveNicknameDescriptionText()
        {
            return $"Save current nickname as default. Current default: {BingoSync.modSettings.DefaultNickname}";
        }

        private static string GetSavePasswordDescriptionText()
        {
            return $"Save current password as default. Current default: {BingoSync.modSettings.DefaultPassword}";
        }

        private static string GetSaveColorDescriptionText()
        {
            return $"Save current selected color as default. Current default: {BingoSync.modSettings.DefaultColor}";
        }
    }
}
