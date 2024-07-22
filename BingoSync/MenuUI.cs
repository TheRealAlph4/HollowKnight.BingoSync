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
        private static readonly int colorButtonWidth = 100;
        private static readonly int textFieldWidth = colorButtonWidth * 5 + 40;
        private static readonly int gameModeButtonSize = (textFieldWidth - 30) / 3;
        private static readonly int generateButtonSize = 350;
        private static readonly int lockoutButtonSize = textFieldWidth - generateButtonSize - 20;
        private static readonly int fontSize = 22;

        public static readonly LayoutRoot layoutRoot = new(true, "Persistent layout")
        {
            VisibilityCondition = () => false,
        };

        private static readonly StackLayout connectionMenu = new(layoutRoot)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10,
            Orientation = Orientation.Vertical,
            Padding = new Padding(0, 50, 20, 0),
        };

        private static readonly TextInput roomCode = new (layoutRoot, "RoomCode")
        {
            FontSize = fontSize,
            MinWidth = textFieldWidth,
            Placeholder = "Room Link",
        };
        public static readonly TextInput nickname = new (layoutRoot, "NickName")
        {
            FontSize = fontSize,
            MinWidth = textFieldWidth,
            Placeholder = "Nickname",
        };
        public static readonly TextInput password = new(layoutRoot, "Password")
        {
            FontSize = fontSize,
            MinWidth = textFieldWidth,
            Placeholder = "Password",
        };
        private static List<Button> colorButtons = 
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
        private static readonly Button joinRoomButton = new(layoutRoot, "roomButton")
        {
            Content = "Join Room",
            FontSize = fontSize,
            Margin = 20,
            MinWidth = textFieldWidth,
        };

        public static StackLayout generationMenu = new(layoutRoot)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Spacing = 15,
            Orientation = Orientation.Vertical,
            Padding = new Padding(0, 50, 20, 15),
        };
        private static readonly Button GenerateBoardButton = new(layoutRoot, "generateBoardButton")
        {
            Content = "Generate Board",
            FontSize = 22,
            Margin = 20,
            MinWidth = generateButtonSize,
            MinHeight = 50,
        };
        private static readonly Button LockoutToggleButton = new(layoutRoot, "lockoutToggleButton")
        {
            Content = "Lockout",
            FontSize = 15,
            Margin = 20,
            MinWidth = lockoutButtonSize,
            MinHeight = 50,
        };
        private static readonly List<Button> GameModeButtons = [];


        public static string selectedColor = "";
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
            connectionMenu.Children.Add(roomCode);
            connectionMenu.Children.Add(nickname);
            connectionMenu.Children.Add(password);

            SetupColorButtons(connectionMenu);

            joinRoomButton.Click += RoomButtonClicked;
            connectionMenu.Children.Add(joinRoomButton);

            LoadDefaults();

            SetupGenerationMenu();

            layoutRoot.VisibilityCondition = () => {
                BingoSyncClient.Update();
                Update();
                return isBingoMenuUIVisible;
            };

            layoutRoot.ListenForPlayerAction(BingoSync.modSettings.Keybinds.HideMenu, () => {
                isBingoMenuUIVisible = !isBingoMenuUIVisible;
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
                MinWidth = colorButtonWidth,
            };
            button.Click += SelectColor;
            return button;
        }

        private static void SetupColorButtons(StackLayout connectionMenu)
        {
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

            for(int i = 0; i < 5; ++i)
            {
                row1.Children.Add(colorButtons.ElementAt(i));
                row2.Children.Add(colorButtons.ElementAt(5 + i));
            }

            colorButtonsLayout.Children.Add(row1);
            colorButtonsLayout.Children.Add(row2);

            connectionMenu.Children.Add(colorButtonsLayout);
        }

        public static Button CreateGameModeButton(string name)
        {
            Button button = new(layoutRoot, name)
            {
                Content = name,
                FontSize = 15,
                Margin = 20,
                MinWidth = gameModeButtonSize,
            };
            button.Click += SelectGameMode;
            return button;
        }

        public static void SetupGameModeButtons()
        {
            StackLayout buttonLayout = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Spacing = 10,
                Orientation = Orientation.Vertical,
            };

            StackLayout row = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
            };

            foreach (string gameMode in GameModesManager.GameModeNames())
            {
                if (row.Children.Count >= 3)
                {
                    buttonLayout.Children.Insert(0, row);
                    row = new(layoutRoot)
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                    };
                }
                Button gameModeButton = CreateGameModeButton(gameMode);
                GameModeButtons.Add(gameModeButton);
                row.Children.Add(gameModeButton);
            }
            buttonLayout.Children.Insert(0, row);
            generationMenu.Children.Insert(0, buttonLayout);
            SelectGameMode(GameModeButtons[0]);
        }

        private static void SetupGenerationMenu()
        {
            GenerateBoardButton.Click += GameModesManager.Generate;
            LockoutToggleButton.Click += ToggleLockout;

            StackLayout bottomRow = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
            };

            bottomRow.Children.Add(GenerateBoardButton);
            bottomRow.Children.Add(LockoutToggleButton);

            generationMenu.Children.Add(bottomRow);
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

        private static void Update()
        {
            if (BingoSyncClient.GetState() == BingoSyncClient.State.Connected)
            {
                joinRoomButton.Content = "Exit Room";
                joinRoomButton.Enabled = true;
                SetEnabled(false);
            } else if (BingoSyncClient.GetState() == BingoSyncClient.State.Loading)
            {
                joinRoomButton.Content = "Loading...";
                joinRoomButton.Enabled = false;
                SetEnabled(false);
            } else
            {
                joinRoomButton.Content = "Join Room";
                joinRoomButton.Enabled = true;
                SetEnabled(true);
            }
        }

        private static string SanitizeRoomCode(string input)
        {
            return new string(input.ToCharArray()
            .Where(c => !Char.IsWhiteSpace(c)).ToArray())
            .Split('/').Last();
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

        private static void SelectColor(Button sender)
        {
            Button previousSelectedColor = layoutRoot.GetElement<Button>(selectedColor);
            previousSelectedColor.BorderColor = previousSelectedColor.ContentColor;
            selectedColor = sender.Name;
            sender.BorderColor = Color.white;
        }

        public static void ToggleLockout(Button sender)
        {
            bool lockout = !GameModesManager.GetLockout();
            GameModesManager.SetLockout(lockout);
            string text = "Lockout";
            if (!lockout)
            {
                text = "Non-Lockout";
            }
            sender.Content = text;
        }

        public static void SelectGameMode(Button sender)
        {
            GameModesManager.SetActiveGameMode(sender.Name);
            foreach (Button gameMode in GameModeButtons)
            {
                gameMode.BorderColor = Color.white;
            }
            sender.BorderColor = Color.red;
        }
    }
}