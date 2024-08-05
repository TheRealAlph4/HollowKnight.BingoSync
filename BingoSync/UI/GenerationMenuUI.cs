using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BingoSync
{
    internal static class GenerationMenuUI
    {
        private static readonly TextureLoader Loader = new(Assembly.GetExecutingAssembly(), "BingoSync.Resources.Images");

        private static LayoutRoot layoutRoot;
        private static StackLayout generationMenu;

        private static TextInput generationSeedInput;
        private static Button generateBoardButton;
        private static readonly List<Button> gameModeButtons = [];
        private static ToggleButton lockoutToggleButton;

        public static void Setup(LayoutRoot layoutRoot)
        {
            Loader.Preload();
            GenerationMenuUI.layoutRoot = layoutRoot;
            CreateGenerationMenu();
        }

        public static void CreateGenerationMenu()
        {
            generationMenu?.Children.Clear();

            generationMenu = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Spacing = 15,
                Orientation = Orientation.Vertical,
                Padding = new Padding(0, 50, 20, 15),
            };
            generationSeedInput = new(layoutRoot, "Seed")
            {
                FontSize = MenuUI.fontSize,
                MinWidth = MenuUI.seedFieldWidth,
                Placeholder = "Seed",
            };
            generateBoardButton = new(layoutRoot, "generateBoardButton")
            {
                Content = "Generate Board",
                FontSize = MenuUI.fontSize,
                Margin = 20,
                MinWidth = MenuUI.generateButtonWidth,
                MinHeight = 50,
            };

            Sprite lockoutSprite = Loader.GetTexture("BingoSync Lockout Icon.png").ToSprite();
            Sprite nonLockoutSprite = Loader.GetTexture("BingoSync Non-Lockout Icon.png").ToSprite();

            lockoutToggleButton = new(layoutRoot, lockoutSprite, nonLockoutSprite, Controller.LockoutButtonClicked, "Lockout Toggle");
            Button lockoutButton = new(layoutRoot, "lockoutToggleButton")
            {
                MinWidth = MenuUI.lockoutButtonWidth,
                MinHeight = MenuUI.lockoutButtonWidth,
            };
            lockoutToggleButton.SetButton(lockoutButton);
            lockoutToggleButton.Toggle(null);

            SetupGenerationMenu();
        }

        private static void SetupGenerationMenu()
        {
            generateBoardButton.Click += Controller.GenerateButtonClicked;

            StackLayout bottomRow = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
            };

            bottomRow.Children.Add(generateBoardButton);
            bottomRow.Children.Add(generationSeedInput);
            bottomRow.Children.Add(lockoutToggleButton);

            generationMenu.Children.Add(bottomRow);
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

            foreach (var button in gameModeButtons)
            {
                button.Destroy();
            }
            gameModeButtons.Clear();

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
                gameModeButtons.Add(gameModeButton);
                row.Children.Add(gameModeButton);
            }
            buttonLayout.Children.Insert(0, row);
            generationMenu.Children.Insert(0, buttonLayout);
            SelectGameMode(gameModeButtons[0]);
        }

        public static Button CreateGameModeButton(string name)
        {
            Button button = new(layoutRoot, name)
            {
                Content = name,
                FontSize = 15,
                Margin = 20,
                MinWidth = MenuUI.gameModeButtonWidth,
            };
            button.Click += SelectGameMode;
            return button;
        }

        public static int GetSeed()
        {
            string inputStr = generationSeedInput.Text;
            int seed = unchecked(DateTime.Now.Ticks.GetHashCode());
            if (inputStr != string.Empty)
            {
                bool isNumeric = int.TryParse(inputStr, out seed);
                if (!isNumeric)
                {
                    seed = inputStr.GetHashCode();
                }
            }
            return seed;
        }

        public static bool LockoutToggleButtonIsOn()
        {
            return lockoutToggleButton.IsOn;
        }

        public static void SelectGameMode(Button sender)
        {
            Controller.ActiveGameMode = sender.Name;
            foreach (Button gameMode in gameModeButtons)
            {
                gameMode.BorderColor = Color.white;
            }
            sender.BorderColor = Color.red;
        }
    }
}
