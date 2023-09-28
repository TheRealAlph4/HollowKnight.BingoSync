using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using GridLayout = MagicUI.Elements.GridLayout;

namespace BingoSync
{
    public static class BingoBoardUI
    {
        public static LayoutRoot layoutRoot;
        private static GridLayout gridLayout;
        private static Sprite sprite = null;
        private static Button revealCardButton = null;
        private static TextObject loadingText = null;

        public static bool isBingoBoardVisible = true;
        private static Action<string> Log;
        private static TextureLoader Loader;
        private static Dictionary<string, Color> BingoColors;

        internal class SquareLayoutObjects
        {
            public TextObject Text;
            public Dictionary<string, Image> BackgroundColors;
        };
        private static List<SquareLayoutObjects> bingoLayout = null;

        public static void Setup(Action<string> log)
        {
            Log = log;
            layoutRoot = new(true, "Persistent layout");

            gridLayout = new GridLayout(layoutRoot, "grid")
            {
                MinWidth = 600,
                MinHeight = 600,
                RowDefinitions =
                {
                    new GridDimension(1, GridUnit.Proportional),
                    new GridDimension(1, GridUnit.Proportional),
                    new GridDimension(1, GridUnit.Proportional),
                    new GridDimension(1, GridUnit.Proportional),
                    new GridDimension(1, GridUnit.Proportional),
                },
                ColumnDefinitions =
                {
                    new GridDimension(1, GridUnit.Proportional),
                    new GridDimension(1, GridUnit.Proportional),
                    new GridDimension(1, GridUnit.Proportional),
                    new GridDimension(1, GridUnit.Proportional),
                    new GridDimension(1, GridUnit.Proportional),
                },
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Visibility = Visibility.Hidden,
            };

            revealCardButton = new Button(layoutRoot, "revealCard") {
                Content = "Reveal Card",
                FontSize = 15,
                Margin = 20,
                BorderColor = Color.white,
                ContentColor = Color.white,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Padding(20),
                MinWidth = 200,
                Visibility = Visibility.Hidden,
            };
            revealCardButton.Click += (sender) => {
                BingoSyncClient.RevealCard();
            };

            loadingText = new TextObject(layoutRoot) {
                Text = "Loading...",
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 200,
                Padding = new Padding(20),
                ContentColor = Color.white,
                Visibility = Visibility.Hidden,
            };

            var assembly = Assembly.GetExecutingAssembly();
            Loader = new TextureLoader(assembly, "BingoSync.Resources.Images");
            Loader.Preload();

            BingoColors = new Dictionary<string, Color>
            {
                { "blank", Color.black },
                { "orange", Colors.Orange },
                { "red", Colors.Red },
                { "blue", Colors.Blue },
                { "green", Colors.Green },
                { "purple", Colors.Purple },
                { "navy", Colors.Navy },
                { "teal", Colors.Teal },
                { "brown", Colors.Brown },
                { "pink", Colors.Pink },
                { "yellow", Colors.Yellow },
            };
            
            sprite = Loader.GetTexture("BingoSync Base Background.png").ToSprite();

            CreateBaseLayout();

            layoutRoot.ListenForPlayerAction(BingoSync.modSettings.Keybinds.ToggleBoard, () =>
            {
                if (BingoSyncClient.board != null)
                {
                    isBingoBoardVisible = !isBingoBoardVisible;
                }
            });
            layoutRoot.ListenForPlayerAction(BingoSync.modSettings.Keybinds.RevealCard, () => {
                BingoSyncClient.RevealCard();
            });
            layoutRoot.VisibilityCondition = () => (BingoSyncClient.GetState() != BingoSyncClient.State.Disconnected && isBingoBoardVisible);
            BingoSyncClient.BoardUpdated.Add(UpdateGrid);
        }

        public static void UpdateGrid()
        {
            loadingText.Visibility = (BingoSyncClient.board == null) ? Visibility.Visible : Visibility.Hidden;
            revealCardButton.Visibility = (BingoSyncClient.board != null && BingoSyncClient.isHidden) ? Visibility.Visible : Visibility.Hidden;
            gridLayout.Visibility = (BingoSyncClient.board == null || BingoSyncClient.isHidden) ? Visibility.Hidden : Visibility.Visible;

            if (BingoSyncClient.board == null)
            {
                return;
            }

            for (var position = 0; position < BingoSyncClient.board.Count; position++)
            {
                bingoLayout[position].Text.Text = BingoSyncClient.board[position].Name;
                var colors = BingoSyncClient.board[position].Colors.Split(' ').ToList();
                bingoLayout[position].BackgroundColors.Keys.ToList().ForEach(color =>
                {
                    bingoLayout[position].BackgroundColors[color].Height = 0;
                });
                colors.ForEach(color =>
                {
                    bingoLayout[position].BackgroundColors[color].Height = 110 / colors.Count;
                });
            }
        }

        public static void CreateBaseLayout() {
            bingoLayout = new List<SquareLayoutObjects>();
            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < 5; column++)
                {
                    var (stack, images) = GenerateSquareBackgroundImage(row, column);
                    gridLayout.Children.Add(stack);

                    var textObject = new TextObject(layoutRoot, $"square_{row}_{column}")
                    {
                        FontSize = 12,
                        Text = "",
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        MaxWidth = 100,
                        MaxHeight = 100,
                        Padding = new Padding(10),
                        ContentColor = Color.white,
                    }.WithProp(GridLayout.Row, row).WithProp(GridLayout.Column, column);
                    gridLayout.Children.Add(textObject);

                    bingoLayout.Add(new SquareLayoutObjects
                    {
                        Text = textObject,
                        BackgroundColors = images,
                    });
                }
            }
        }

        private static (StackLayout, Dictionary<string, Image>) GenerateSquareBackgroundImage(int row, int column)
        {
            var stack = new StackLayout(layoutRoot, $"background_{row}_{column}")
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Orientation = Orientation.Vertical,
                Spacing = 0,
            }.WithProp(GridLayout.Row, row).WithProp(GridLayout.Column, column);

            var colors = BingoColors.Keys.ToList();
            var images = new Dictionary<string, Image>();
            for (int brow = 0; brow < colors.Count; brow++) {
                Color tint;
                if (BingoColors.TryGetValue(colors[brow], out tint))
                {
                    var backgroundImage = new Image(layoutRoot, sprite, $"image_{brow}_{row}_{column}")
                    {
                        Height = 0,
                        Width = 110,
                        Tint = tint,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    stack.Children.Add(backgroundImage);
                    images.Add(colors[brow], backgroundImage);
                }
            }

            return (stack, images);
        }
    }
}