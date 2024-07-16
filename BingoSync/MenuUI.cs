using MagicUI.Core;
using MagicUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BingoSync
{
    public static class MenuUI
{
        public static string selectedColor = "";
        public static TextInput roomCode, nickname, password;
        private static List<Button> colorButtons;
        private static Button roomButton;

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
            Button selectedColorButton = layoutRoot.GetElement<Button>(selectedColor);
            if (selectedColorButton != null) {
                selectedColorButton.BorderColor = Color.white;
            }
        }

        public static void Setup()
        {
            layoutRoot = new(true, "Persistent layout");
            StackLayout layout = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Orientation = Orientation.Vertical,
                Padding = new Padding(0, 50, 20, 0),
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
            return new string(input.ToCharArray()
            .Where(c => !Char.IsWhiteSpace(c))
            .ToArray()).Split('/').Last();
        }

        private static void RoomButtonClicked(Button sender)
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

        private static Button CreateColorButton(string text, Color color)
        {
            Button button = new(layoutRoot, text.ToLower())
            {
                Content = text,
                FontSize = 15,
                Margin = 20,
                BorderColor = color,
                ContentColor = color,
                MinWidth = buttonSize,
            };
            button.Click += SelectColor;
            return button;
        }

        private static StackLayout CreateButtons()
        {
            StackLayout buttonLayout = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Orientation = Orientation.Vertical,
            };

            StackLayout row1 = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
            };

            var orange = CreateColorButton("Orange", Colors.Orange);
            row1.Children.Add(orange);
            var red = CreateColorButton("Red", Colors.Red);
            row1.Children.Add(red);
            var blue = CreateColorButton("Blue", Colors.Blue);
            row1.Children.Add(blue);
            var green = CreateColorButton("Green", Colors.Green);
            row1.Children.Add(green);
            var purple = CreateColorButton("Purple", Colors.Purple);
            row1.Children.Add(purple);

            StackLayout row2 = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
            };

            var navy = CreateColorButton("Navy", Colors.Navy);
            row2.Children.Add(navy);
            var teal = CreateColorButton("Teal", Colors.Teal);
            row2.Children.Add(teal);
            var brown = CreateColorButton("Brown", Colors.Brown);
            row2.Children.Add(brown);
            var pink = CreateColorButton("Pink", Colors.Pink);
            row2.Children.Add(pink);
            var yellow = CreateColorButton("Yellow", Colors.Yellow);
            row2.Children.Add(yellow);

            buttonLayout.Children.Add(row1);
            buttonLayout.Children.Add(row2);

            colorButtons = [orange, red, blue, green, purple, navy, teal, brown, pink, yellow];

            return buttonLayout;
        }

        private static void SelectColor(Button sender)
        {
            Button previousSelectedColor = layoutRoot.GetElement<Button>(selectedColor);
            previousSelectedColor.BorderColor = previousSelectedColor.ContentColor;
            selectedColor = sender.Name;
            sender.BorderColor = Color.white;
        }
    }
}