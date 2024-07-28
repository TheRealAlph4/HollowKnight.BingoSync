using MagicUI.Core;
using MagicUI.Elements;
using UnityEngine;
using System.Collections.Generic;
using MagicUI.Graphics;
using System.Reflection;
using System.Linq;

namespace BingoSync
{
    internal static class ConnectionMenuUI
    {
        private static readonly TextureLoader Loader = new(Assembly.GetExecutingAssembly(), "BingoSync.Resources.Images");

        private static LayoutRoot layoutRoot;
        private static StackLayout connectionMenu;

        private static TextInput roomCodeInput;
        private static TextInput nicknameInput;
        private static TextInput passwordInput;
        private static List<Button> colorButtons;
        private static Button joinRoomButton;
        private static ToggleButton handModeToggleButton;

        public static void Setup(LayoutRoot layoutRoot)
        {
            Loader.Preload();
            ConnectionMenuUI.layoutRoot = layoutRoot;

            SetupTextFields();
            SetupColorButtons();
            SetupConnectionButtons();

            LoadDefaults();
        }

        private static void SetupTextFields()
        {
            connectionMenu = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Orientation = Orientation.Vertical,
                Padding = new Padding(0, 50, 20, 0),
            };
            roomCodeInput = new(layoutRoot, "RoomCode")
            {
                FontSize = MenuUI.fontSize,
                MinWidth = MenuUI.textFieldWidth,
                Placeholder = "Room Link",
            };
            nicknameInput = new(layoutRoot, "NickName")
            {
                FontSize = MenuUI.fontSize,
                MinWidth = MenuUI.textFieldWidth,
                Placeholder = "Nickname",
            };
            passwordInput = new(layoutRoot, "Password")
            {
                FontSize = MenuUI.fontSize,
                MinWidth = MenuUI.textFieldWidth,
                Placeholder = "Password",
            };

            connectionMenu.Children.Add(roomCodeInput);
            connectionMenu.Children.Add(nicknameInput);
            connectionMenu.Children.Add(passwordInput);
        }

        private static void SetupColorButtons()
        {
            colorButtons =
            [
                CreateColorButton("Orange", Colors.Orange),
                CreateColorButton("Red", Colors.Red),
                CreateColorButton("Blue", Colors.Blue),
                CreateColorButton("Green", Colors.Green),
                CreateColorButton("Purple", Colors.Purple),
                CreateColorButton("Navy", Colors.Navy),
                CreateColorButton("Teal", Colors.Teal),
                CreateColorButton("Brown", Colors.Brown),
                CreateColorButton("Pink", Colors.Pink),
                CreateColorButton("Yellow", Colors.Yellow)
            ];

            StackLayout colorButtonsLayout = new(layoutRoot)
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

            StackLayout row2 = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
            };

            for (int i = 0; i < 5; ++i)
            {
                row1.Children.Add(colorButtons.ElementAt(i));
                row2.Children.Add(colorButtons.ElementAt(5 + i));
            }

            colorButtonsLayout.Children.Add(row1);
            colorButtonsLayout.Children.Add(row2);

            connectionMenu.Children.Add(colorButtonsLayout);
        }

        private static void SetupConnectionButtons()
        {
            joinRoomButton = new(layoutRoot, "roomButton")
            {
                Content = "Join Room",
                FontSize = MenuUI.fontSize,
                Margin = 20,
                MinWidth = MenuUI.joinRoomButtonWidth,
            };
            Sprite handModeSprite = Loader.GetTexture("BingoSync Hand Icon.png").ToSprite();
            Sprite nonHandModeSprite = Loader.GetTexture("BingoSync Eye Icon.png").ToSprite();

            handModeToggleButton = new(layoutRoot, handModeSprite, nonHandModeSprite, Controller.HandModeButtonClicked, "Hand Mode Toggle");
            Button handModeButton = new(layoutRoot, "handModeToggleButton")
            {
                MinWidth = MenuUI.handModeButtonWidth,
                MinHeight = MenuUI.handModeButtonWidth,
            };
            handModeToggleButton.SetButton(handModeButton);

            joinRoomButton.Click += JoinRoomButtonClicked;

            StackLayout bottomRow = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
            };

            bottomRow.Children.Add(joinRoomButton);
            bottomRow.Children.Add(handModeToggleButton);

            connectionMenu.Children.Add(bottomRow);
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
                MinWidth = MenuUI.colorButtonWidth,
            };
            button.Click += SelectColor;
            return button;
        }

        private static void SelectColor(Button sender)
        {
            Button previousSelectedColor = layoutRoot.GetElement<Button>(Controller.RoomColor);
            previousSelectedColor.BorderColor = previousSelectedColor.ContentColor;
            Controller.RoomColor = sender.Name;
            sender.BorderColor = Color.white;
        }

        private static string SanitizeRoomCode(string input)
        {
            return new string(input.ToCharArray()
            .Where(c => !char.IsWhiteSpace(c)).ToArray())
            .Split('/').Last();
        }

        public static void ReadCurrentConnectionInfo()
        {
            Controller.RoomCode = SanitizeRoomCode(roomCodeInput.Text);
            Controller.RoomNickname = nicknameInput.Text;
            Controller.RoomPassword = passwordInput.Text;
        }

        private static void JoinRoomButtonClicked(Button sender)
        {
            if (BingoSyncClient.GetState() != BingoSyncClient.State.Connected)
            {
                ReadCurrentConnectionInfo();
                BingoSyncClient.JoinRoom((ex) =>
                {
                    Update();
                });
                Update();
            }
            else
            {
                BingoSyncClient.ExitRoom(() =>
                {
                    Update();
                });
                Update();
            }
        }

        public static void Update()
        {
            if (Controller.ClientIsConnected())
            {
                joinRoomButton.Content = "Exit Room";
                joinRoomButton.Enabled = true;
                SetEnabled(false);
            }
            else if (Controller.ClientIsConnecting())
            {
                joinRoomButton.Content = "Loading...";
                joinRoomButton.Enabled = false;
                SetEnabled(false);
            }
            else
            {
                joinRoomButton.Content = "Join Room";
                joinRoomButton.Enabled = true;
                SetEnabled(true);
            }
        }

        private static void SetEnabled(bool enabled)
        {
            roomCodeInput.Enabled = enabled;
            nicknameInput.Enabled = enabled;
            passwordInput.Enabled = enabled;
            colorButtons.ForEach(button =>
            {
                button.Enabled = enabled;
            });
        }

        public static void LoadDefaults()
        {
            if (nicknameInput != null)
                nicknameInput.Text = BingoSync.modSettings.DefaultNickname;
            if (passwordInput != null)
                passwordInput.Text = BingoSync.modSettings.DefaultPassword;
            Controller.RoomColor = BingoSync.modSettings.DefaultColor;
            if (layoutRoot == null)
                return;
            Button selectedColorButton = layoutRoot.GetElement<Button>(Controller.RoomColor);
            if (selectedColorButton != null)
            {
                selectedColorButton.BorderColor = Color.white;
            }
        }

        public static bool HandModeToggleButtonIsOn()
        {
            return handModeToggleButton.IsOn;
        }
    }
}
