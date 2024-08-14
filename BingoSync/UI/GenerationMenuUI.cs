using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BingoSync.CustomGoals;

namespace BingoSync.GameUI
{
    static class GenerationMenuUI
    {
        private static Action<string> Log;
        private static readonly TextureLoader Loader = new(Assembly.GetExecutingAssembly(), "BingoSync.Resources.Images");

        private static LayoutRoot layoutRoot;
        private static StackLayout generationMenu;

        private static TextInput profileNameInput;
        private static Button acceptProfileNameButton;

        private static TextInput generationSeedInput;
        private static Button generateBoardButton;
        private static readonly List<Button> gameModeButtons = [];
        private static ToggleButton lockoutToggleButton;

        public static bool TextBoxActive { get; private set; } = false;

        public static void Setup(Action<string> log, LayoutRoot layoutRoot)
        {
            Log = log;
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

            profileNameInput = new(layoutRoot, "profileName")
            {
                FontSize = MenuUI.fontSize,
                MinWidth = MenuUI.profileNameFieldWidth,
                Placeholder = "Profile Name",
            };
            profileNameInput.OnHover += HoverTextInput;
            profileNameInput.OnUnhover += UnhoverTextInput;
            acceptProfileNameButton = new(layoutRoot, "acceptProfileNameButton")
            {
                Content = "Set Name",
                FontSize = 15,
                Margin = 20,
                MinWidth = MenuUI.acceptProfileNameButtonWidth,
                MinHeight = 25,
            };

            generationSeedInput = new(layoutRoot, "Seed")
            {
                FontSize = MenuUI.fontSize,
                MinWidth = MenuUI.seedFieldWidth,
                Placeholder = "Seed",
            };
            generationSeedInput.OnHover += HoverTextInput;
            generationSeedInput.OnUnhover += UnhoverTextInput;
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

            SetupRenameProfileRow();
            SetupGenerateRow();
        }

        private static void HoverTextInput(TextInput _)
        {
            TextBoxActive = true;
        }
        private static void UnhoverTextInput(TextInput _)
        {
            // note: this also gets called if the textbox becomes inactive by hiding the menu
            TextBoxActive = false;
        }

        private static void SetupRenameProfileRow()
        {
            acceptProfileNameButton.Click += AcceptProfileNameButtonClicked;

            StackLayout profileRenameRow = new(layoutRoot)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
            };

            profileRenameRow.Children.Add(profileNameInput);
            profileRenameRow.Children.Add(acceptProfileNameButton);
            generationMenu.Children.Add(profileRenameRow);
        }

        private static void SetupGenerateRow()
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

        public static (int, bool) GetSeed()
        {
            string inputStr = generationSeedInput.Text;
            int seed = unchecked(DateTime.Now.Ticks.GetHashCode());
            bool isCustom = false;
            if (inputStr != string.Empty)
            {
                isCustom = true;
                bool isNumeric = int.TryParse(inputStr, out seed);
                if (!isNumeric)
                {
                    seed = inputStr.GetHashCode();
                }
            }
            return (seed, isCustom);
        }

        public static bool LockoutToggleButtonIsOn()
        {
            return lockoutToggleButton.IsOn;
        }

        private static void SelectGameMode(Button sender)
        {
            string gameModeName = sender.Content;
            Controller.ActiveGameMode = gameModeName;
            profileNameInput.Text = gameModeName;
            if (gameModeName.EndsWith("*"))
            {
                profileNameInput.Text = gameModeName.Remove(gameModeName.Count() - 1, 1);
            }
            foreach (Button gameMode in gameModeButtons)
            {
                gameMode.BorderColor = Color.white;
            }
            sender.BorderColor = Color.red;
            bool isCustom = Controller.IsCustomGameMode(gameModeName);
            profileNameInput.Enabled = isCustom;
            acceptProfileNameButton.Enabled = isCustom;
        }

        public static void SetGenerationButtonEnabled(bool enabled)
        {
            generateBoardButton.Enabled = enabled;
        }

        private static void AcceptProfileNameButtonClicked(Button _)
        {
            string rawName = profileNameInput.Text;
            string displayName = rawName + "*";
            if(rawName == string.Empty)
            {
                Log($"A name must be given to rename a gamemode");
                return;
            }
            if (gameModeButtons.FindIndex(gameMode => gameMode.Content == displayName) != -1)
            {
                Log($"Cannot rename gamemode to {displayName}, that name already exists");
                return;
            }
            string oldName = Controller.ActiveGameMode;
            bool success = Controller.RenameActiveGameModeTo(rawName);
            if (success)
            {
                gameModeButtons.Find(button => button.Content == oldName).Content = displayName;
                Controller.RefreshMenu();
            }
        }
    }
}
