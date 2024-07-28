using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using GridLayout = MagicUI.Elements.GridLayout;

namespace BingoSync
{
    public static class BingoBoardUI
    {
        internal class SquareLayoutObjects
        {
            public TextObject Text;
            public Dictionary<string, Image> BackgroundColors;
        };
        private class Board
        {
            public int id;
            public LayoutRoot layoutRoot;
            public GridLayout gridLayout;
            public List<SquareLayoutObjects> bingoLayout;
        }

        private static readonly List<Board> boards = [];

        private static readonly LayoutRoot commonRoot = new(true, "Persistent layout")
        {
            VisibilityCondition = () => false,
        };
        private static readonly Button revealCardButton = new(commonRoot, "revealCard")
        {
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
        private static readonly TextObject loadingText = new(commonRoot)
        {
            Text = "Loading...",
            FontSize = 15,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 200,
            Padding = new Padding(20),
            ContentColor = Color.white,
            Visibility = Visibility.Hidden,
        };
        
        private static Action<string> Log;
        private static readonly TextureLoader Loader = new(Assembly.GetExecutingAssembly(), "BingoSync.Resources.Images");
        private static readonly Dictionary<string, Color> BingoColors = new()
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


        public static void Setup(Action<string> log)
        {
            Log = log;

            commonRoot.VisibilityCondition = () => true;

            revealCardButton.Click += Controller.RevealButtonClicked;

            Loader.Preload();

            boards.Add(CreateBoardWithSprite(Loader.GetTexture("BingoSync Transparent Background.png").ToSprite()));
            boards.Add(CreateBoardWithSprite(Loader.GetTexture("BingoSync Opaque Background.png").ToSprite()));
            boards.Add(CreateBoardWithSprite(Loader.GetTexture("BingoSync Solid Background.png").ToSprite()));

            commonRoot.ListenForPlayerAction(BingoSync.modSettings.Keybinds.ToggleBoard, Controller.ToggleBoardKeybindClicked);
            commonRoot.ListenForPlayerAction(BingoSync.modSettings.Keybinds.RevealCard, Controller.RevealKeybindClicked);
            commonRoot.ListenForPlayerAction(BingoSync.modSettings.Keybinds.CycleBoardOpacity, Controller.UpdateOpacity);

            BingoSyncClient.BoardUpdated.Add(UpdateGrid);
            BingoSyncClient.BoardUpdated.Add(Controller.ConfirmTopLeftOnReveal);
        }

        private static Board CreateBoardWithSprite(Sprite sprite)
        {
            Board board = new()
            {
                id = boards.Count,
                layoutRoot = new(true, "Persistent layout"),
            };

            board.gridLayout = new GridLayout(board.layoutRoot, "grid")
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

            CreateBaseLayout(board, sprite);

            board.layoutRoot.VisibilityCondition = () => {
                return (BingoSyncClient.GetState() != BingoSyncClient.State.Disconnected) && (Controller.CurrentBoardID == board.id) && (Controller.BoardIsVisible);
            };

            return board;
        }

        public static void UpdateGrid()
        {
            loadingText.Visibility = (!Controller.BoardIsAvailable()) ? Visibility.Visible : Visibility.Hidden;
            revealCardButton.Visibility = (BingoSyncClient.GetState() == BingoSyncClient.State.Connected && Controller.BoardIsAvailable() && !Controller.BoardIsRevealed) ? Visibility.Visible : Visibility.Hidden;
            boards.ForEach(board => board.gridLayout.Visibility = (!Controller.BoardIsAvailable() || !Controller.BoardIsRevealed) ? Visibility.Hidden : Visibility.Visible);

            if (!Controller.BoardIsAvailable())
            {
                return;
            }

            for (var position = 0; position < Controller.Board.Count; position++)
            {
                boards.ForEach(board => board.bingoLayout[position].Text.Text = Controller.Board[position].Name);
                var colors = Controller.Board[position].Colors.Split(' ').ToList();
                boards.ForEach(board => board.bingoLayout[position].BackgroundColors.Keys.ToList().ForEach(color =>
                {
                    board.bingoLayout[position].BackgroundColors[color].Height = 0;
                }));
                boards.ForEach(board => colors.ForEach(color =>
                {
                    board.bingoLayout[position].BackgroundColors[color].Height = 110 / colors.Count;
                }));
            }
        }

        private static void CreateBaseLayout(Board board, Sprite backgroundSprite) {
            board.bingoLayout = [];
            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < 5; column++)
                {
                    var (stack, images) = GenerateSquareBackgroundImage(board, row, column, backgroundSprite);
                    board.gridLayout.Children.Add(stack);

                    var textObject = new TextObject(board.layoutRoot, $"square_{row}_{column}")
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
                    board.gridLayout.Children.Add(textObject);

                    board.bingoLayout.Add(new SquareLayoutObjects
                    {
                        Text = textObject,
                        BackgroundColors = images,
                    });
                }
            }
        }

        private static (StackLayout, Dictionary<string, Image>) GenerateSquareBackgroundImage(Board board, int row, int column, Sprite backgroundSprite)
        {
            var stack = new StackLayout(board.layoutRoot, $"background_{row}_{column}")
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
                    var backgroundImage = new Image(board.layoutRoot, backgroundSprite, $"image_{brow}_{row}_{column}")
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

        public static int GetBoardCount()
        {
            return boards.Count;
        }
    }
}