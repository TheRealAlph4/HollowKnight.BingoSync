using System;
using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine;
using UnityEngine.UI;

namespace BingoSync
{
    public static class ModMenu {
        private static MenuButton saveNicknameButton;
        private static MenuButton savePasswordButton;
        private static MenuButton saveColorButton;
        private static MenuOptionHorizontal revealCardOnStartSelector;
        private static MenuOptionHorizontal unmarkGoalsSelector;

        public static void RefreshMenu() {
            revealCardOnStartSelector?.menuSetting?.RefreshValueFromGameSettings();
            unmarkGoalsSelector?.menuSetting?.RefreshValueFromGameSettings();

            var nickDesc = saveNicknameButton?.descriptionText?.GetComponentInParent<Text>();
            if (nickDesc != null) {
                nickDesc.text = GetSaveNicknameDescriptionText();
            }

            var passDesc = savePasswordButton?.descriptionText?.GetComponentInParent<Text>();
            if (passDesc != null) {
                passDesc.text = GetSavePasswordDescriptionText();
            }

            var colorDesc = saveColorButton?.descriptionText?.GetComponentInParent<Text>();
            if (colorDesc != null) {
                colorDesc.text = GetSaveColorDescriptionText();
            }
        }

        public static MenuBuilder CreateMenuScreen(MenuScreen modListMenu) {
            void CancelAction(MenuSelectable selectable) => UIManager.instance.UIGoToDynamicMenu(modListMenu);
            return new MenuBuilder(UIManager.instance.UICanvas.gameObject, "BingoSync Menu")
                .CreateTitle("BingoSync Menu", MenuTitleStyle.vanillaStyle)
                .CreateContentPane(RectTransformData.FromSizeAndPos(
                    new RelVector2(new Vector2(1920f, 903f)),
                    new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -60f)
                    )
                ))
                .CreateControlPane(RectTransformData.FromSizeAndPos(
                    new RelVector2(new Vector2(1920f, 259f)),
                    new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -502f)
                    )
                ))
                .AddContent(
                    RegularGridLayout.CreateVerticalLayout(105f),
                    c =>
                    {
                        c.AddKeybind("Toggle Board", BingoSync.modSettings.Keybinds.ToggleBoard, new KeybindConfig
                        {
                            Label = "Toggle Board",
                            CancelAction = (Action<MenuSelectable>)CancelAction,
                        })
                        .AddKeybind("Cycle Board Opacity", BingoSync.modSettings.Keybinds.CycleBoardOpacity, new KeybindConfig
                        {
                            Label = "Cycle Board Opacity",
                            CancelAction = (Action<MenuSelectable>)CancelAction,
                        })
                        .AddKeybind("Hide Menu", BingoSync.modSettings.Keybinds.HideMenu, new KeybindConfig
                        {
                            Label = "Hide Menu",
                            CancelAction = (Action<MenuSelectable>)CancelAction,
                        })
                        .AddKeybind("Reveal Card", BingoSync.modSettings.Keybinds.RevealCard, new KeybindConfig
                        {
                            Label = "Reveal Card",
                            CancelAction = (Action<MenuSelectable>)CancelAction,
                        })
                        .AddHorizontalOption("Reveal Card On Start", new HorizontalOptionConfig
                        {
                            Label = "Reveal Card on Game Start",
                            Options = ["No", "Yes"],
                            CancelAction = CancelAction,
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
                        .AddMenuButton("Save Nickname", new MenuButtonConfig
                        {
                            Label = "Save Default Nickname",
                            Description = new DescriptionInfo
                            {
                                Text = GetSaveNicknameDescriptionText()
                            },
                            CancelAction = CancelAction,
                            SubmitAction = _ =>
                            {
                                Controller.RefreshInfoFromUI();
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
                            CancelAction = CancelAction,
                            SubmitAction = _ =>
                            {
                                Controller.RefreshInfoFromUI();
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
                            CancelAction = CancelAction,
                            SubmitAction = _ =>
                            {
                                BingoSync.modSettings.DefaultColor = Controller.RoomColor;
                                RefreshMenu();
                            }
                        }, out saveColorButton)
                        .AddHorizontalOption("Unmark Goals", new HorizontalOptionConfig
                        {
                            Label = "Unmark Goals",
                            Description = new DescriptionInfo
                            {
                                Text = "Some goals will be unmarked if their conditions are no longer met. WARNING: Can cause board inconsistencies on rare situations"
                            },
                            Options = ["No", "Yes"],
                            CancelAction = CancelAction,
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
                    })
                .AddControls(
                    new SingleContentLayout(new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -64f)
                    )), c => c.AddMenuButton(
                    "BackButton",
                    new MenuButtonConfig
                    {
                        Label = "Back",
                        CancelAction = CancelAction,
                        SubmitAction = (Action<MenuSelectable>)CancelAction,
                        Style = MenuButtonStyle.VanillaStyle,
                        Proceed = true
                    }));
        }

        private static string GetSaveNicknameDescriptionText() {
            return $"Save current nickname as default. Current default: {BingoSync.modSettings.DefaultNickname}";
        }

        private static string GetSavePasswordDescriptionText() {
            return $"Save current password as default. Current default: {BingoSync.modSettings.DefaultPassword}";
        }

        private static string GetSaveColorDescriptionText() {
            return $"Save current selected color as default. Current default: {BingoSync.modSettings.DefaultColor}";
        }
    }
}