using MagicUI.Core;
using MagicUI.Elements;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BingoSync
{
    public static class MenuUI
    {
        public static string selectedColor = "";
        public static TextInput roomCode, nickname, password;
        private static List<MagicUI.Elements.Button> colorButtons;
        private static MagicUI.Elements.Button roomButton;

        private static int buttonSize = 100;
        private static int inputSize = buttonSize * 5 + 40;

        public static LayoutRoot layoutRoot;
        private static bool isBingoMenuUIVisible = true;

        public static void SetVisible(bool visible)
        {
            isBingoMenuUIVisible = visible;
        }

        public static void LoadDefaults() {
            if (nickname != null)
                nickname.Text = BingoSync.modSettings.DefaultNickname;
            if (password != null)
                password.Text = BingoSync.modSettings.DefaultPassword;
            selectedColor = BingoSync.modSettings.DefaultColor;
            if (layoutRoot == null)
                return;
            MagicUI.Elements.Button selectedColorButton = layoutRoot.GetElement<MagicUI.Elements.Button>(selectedColor);
            if (selectedColorButton != null) {
                selectedColorButton.BorderColor = Color.white;
            }
        }

        public static void Setup()
        {
            layoutRoot = new(true, "Persistent layout");
            StackLayout layout = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Orientation = Orientation.Vertical,
                Padding = new(20),
            };

            roomCode = new(layoutRoot, "RoomCode")
            {
                FontSize = 22,
                MinWidth = inputSize,
                Placeholder = "Room Code",
            };
            nickname = new(layoutRoot, "NickName")
            {
                FontSize = 22,
                MinWidth = inputSize,
                Placeholder = "Nickname",
            };
            password = new(layoutRoot, "Password")
            {
                FontSize = 22,
                MinWidth = inputSize,
                Placeholder = "Password",
            };
            roomButton = new(layoutRoot, "roomButton")
            {
                Content = "Join Room",
                FontSize = 22,
                Margin = 20,
                MinWidth = inputSize,
            };
            roomButton.Click += RoomButtonClicked;

            layout.Children.Add(roomCode);
            layout.Children.Add(nickname);
            layout.Children.Add(password);
            layout.Children.Add(CreateButtons());
            layout.Children.Add(roomButton);

            LoadDefaults();

            layoutRoot.VisibilityCondition = () => {
                BingoSyncClient.Update();
                Update();
                return isBingoMenuUIVisible;
            };
            layoutRoot.ListenForPlayerAction(BingoSync.modSettings.Keybinds.HideMenu, () => {
                isBingoMenuUIVisible = !isBingoMenuUIVisible;
            });
        }

        private static void Update()
        {
            if (BingoSyncClient.GetState() == BingoSyncClient.State.Connected)
            {
                roomButton.Content = "Exit Room";
                roomButton.Enabled = true;
                SetEnabled(false);
            } else if (BingoSyncClient.GetState() == BingoSyncClient.State.Loading)
            {
                roomButton.Content = "Loading...";
                roomButton.Enabled = false;
                SetEnabled(false);
            } else
            {
                roomButton.Content = "Join Room";
                roomButton.Enabled = true;
                SetEnabled(true);
            }
        }

        private static string SanitizeRoomCode(string input)
        {
            return input.Split('/').Last();
        }

        private static void RoomButtonClicked(MagicUI.Elements.Button sender)
        {
            if (BingoSyncClient.GetState() != BingoSyncClient.State.Connected)
            {
                BingoSyncClient.room = SanitizeRoomCode(MenuUI.roomCode.Text);
                BingoSyncClient.nickname = MenuUI.nickname.Text;
                BingoSyncClient.password = MenuUI.password.Text;
                BingoSyncClient.color = MenuUI.selectedColor;
                BingoSyncClient.JoinRoom((ex) =>
                {
                    Update();
                });
                Update();
            } else
            {
                BingoSyncClient.ExitRoom(() =>
                {
                    Update();
                });
                Update();
            }
        }

        public static void SetEnabled(bool enabled)
        {
            roomCode.Enabled = enabled;
            nickname.Enabled = enabled;
            password.Enabled = enabled;
            colorButtons.ForEach(button =>
            {
                button.Enabled = enabled;
            });
        }

        private static StackLayout CreateButtons()
        {
            StackLayout buttonLayout = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
            };

            MagicUI.Elements.Button orange = new(layoutRoot, "orange")
            {
                Content = "Orange",
                FontSize = 15,
                Margin = 20,
                BorderColor = new Color(1, 0.612f, 0.071f),
                ContentColor = new Color(1, 0.612f, 0.071f),
                MinWidth = buttonSize,
            };
            MagicUI.Elements.Button red = new(layoutRoot, "red")
            {
                Content = "Red",
                FontSize = 15,
                Margin = 20,
                BorderColor = Color.red,
                ContentColor = Color.red,
                MinWidth = buttonSize,
            };
            MagicUI.Elements.Button blue = new(layoutRoot, "blue")
            {
                Content = "Blue",
                FontSize = 15,
                Margin = 20,
                BorderColor = Color.blue,
                ContentColor = Color.blue,
                MinWidth = buttonSize,
            };
            MagicUI.Elements.Button green = new(layoutRoot, "green")
            {
                Content = "Green",
                FontSize = 15,
                Margin = 20,
                BorderColor = Color.green,
                ContentColor = Color.green,
                MinWidth = buttonSize,
            };
            MagicUI.Elements.Button purple = new(layoutRoot, "purple")
            {
                Content = "Purple",
                FontSize = 15,
                Margin = 20,
                BorderColor = new Color(0.51f, 0.18f, 0.75f),
                ContentColor = new Color(0.51f, 0.18f, 0.75f),
                MinWidth = buttonSize,
            };

            buttonLayout.Children.Add(orange);
            orange.Click += SelectColor;
            buttonLayout.Children.Add(red);
            red.Click += SelectColor;
            buttonLayout.Children.Add(blue);
            blue.Click += SelectColor;
            buttonLayout.Children.Add(green);
            green.Click += SelectColor;
            buttonLayout.Children.Add(purple);
            purple.Click += SelectColor;

            colorButtons = new List<MagicUI.Elements.Button> {orange, red, blue, green, purple};

            return buttonLayout;
        }

        private static void SelectColor(MagicUI.Elements.Button sender)
        {
            MagicUI.Elements.Button previousSelectedColor = layoutRoot.GetElement<MagicUI.Elements.Button>(selectedColor);
            previousSelectedColor.BorderColor = previousSelectedColor.ContentColor;
            selectedColor = sender.Name;
            sender.BorderColor = Color.white;
        }
    }
}