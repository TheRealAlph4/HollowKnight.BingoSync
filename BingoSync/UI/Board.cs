using MagicUI.Core;
using MagicUI.Elements;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GridLayout = MagicUI.Elements.GridLayout;

namespace BingoSync.GameUI
{
    class Board
    {
        internal class SquareLayoutObjects
        {
            public TextObject Text;
            public Dictionary<string, Image> BackgroundColors;
        };
        public int id;
        public LayoutRoot layoutRoot;
        public GridLayout gridLayout;
        public List<SquareLayoutObjects> bingoLayout;

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

        public Board(Sprite sprite)
        {
            id = BingoBoardUI.GetBoardCount();
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

            CreateBaseLayout(sprite);

            layoutRoot.VisibilityCondition = () => {
                return (Controller.ClientIsConnected()) && (Controller.CurrentBoardID == id) && (Controller.BoardIsVisible);
            };

        }

        private void CreateBaseLayout(Sprite backgroundSprite)
        {
            bingoLayout = [];
            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < 5; column++)
                {
                    var (stack, images) = GenerateSquareBackgroundImage(row, column, backgroundSprite);
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

        private (StackLayout, Dictionary<string, Image>) GenerateSquareBackgroundImage(int row, int column, Sprite backgroundSprite)
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
            for (int brow = 0; brow < colors.Count; brow++)
            {
                Color tint;
                if (BingoColors.TryGetValue(colors[brow], out tint))
                {
                    var backgroundImage = new Image(layoutRoot, backgroundSprite, $"image_{brow}_{row}_{column}")
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
