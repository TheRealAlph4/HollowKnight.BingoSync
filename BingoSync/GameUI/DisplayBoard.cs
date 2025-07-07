﻿using MagicUI.Core;
using MagicUI.Elements;
using UnityEngine;
using System.Collections.Generic;
using GridLayout = MagicUI.Elements.GridLayout;
using Satchel;
using static BingoSync.Settings.ModSettings;

namespace BingoSync.GameUI
{
    class DisplayBoard
    {
        internal class SquareLayoutObjects
        {
            public TextObject Text;
            public Dictionary<HighlightType, Image> Highlights;
            public Dictionary<string, Image> BackgroundColors;
        };
        private readonly LayoutRoot layoutRoot;
        private readonly StackLayout boardAndName;
        private readonly GridLayout gridLayout;
        public List<SquareLayoutObjects> bingoLayout;
        public readonly TextObject boardName;
        private bool opacityInitialized = false;
        private readonly Dictionary<string, List<Image>> backgroundImagesByColor = new()
        {
            ["orange"] = [],
            ["red"] = [],
            ["blue"] = [],
            ["green"] = [],
            ["purple"] = [],
            ["navy"] = [],
            ["teal"] = [],
            ["brown"] = [],
            ["pink"] = [],
            ["yellow"] = [],
            ["blank"] = []
        };


        public DisplayBoard(Sprite backgroundSprite, Dictionary<HighlightType, Sprite> highlightSprites)
        {
            layoutRoot = new(true, "BingoSync_BoardDisplayRoot");

            boardAndName = new(layoutRoot, "BingoSync_BoardDisplayStack")
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Visibility = Visibility.Visible,
            };

            gridLayout = new GridLayout(layoutRoot, "BingoSync_BoardDisplayGrid")
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
                Visibility = Visibility.Visible,
            };

            boardName = new(layoutRoot, "BingoSync_BoardDisplayName")
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = HorizontalAlignment.Left,
                Text = "Hello there",
                Visibility = Visibility.Visible,
                FontSize = 26,
                Padding = new Padding(5, 3),
            };

            boardAndName.Children.Add(gridLayout);
            boardAndName.Children.Add(boardName);

            CreateBaseLayout(backgroundSprite, highlightSprites);

            layoutRoot.VisibilityCondition = BoardShouldBeVisible;
        }

        private bool BoardShouldBeVisible()
        {
            bool shouldBeVisible = Controller.ActiveSession.ClientIsConnected() && Controller.BoardIsVisible && Controller.ActiveSession.Board.IsAvailable() && Controller.ActiveSession.Board.IsRevealed;
            if (shouldBeVisible && !opacityInitialized)
            {
                opacityInitialized = true;
                SetAlpha(Controller.GlobalSettings.BoardAlpha);
            }
            return shouldBeVisible;
        }

        public void SetAlpha(float alpha)
        {
            GameObject[] objects = GameObject.FindObjectsOfType<GameObject>();
            if (objects == null)
            {
                return;
            }
            foreach (GameObject obj in objects)
            {
                if (obj == null)
                {
                    continue;
                }
                string name = obj.GetName();
                if(name.Contains("BingoSync_BoardDisplay") && !name.Contains("text"))
                {
                    obj.GetComponent<CanvasRenderer>()?.SetAlpha(alpha);
                }
            }
        }

        private void CreateBaseLayout(Sprite backgroundSprite, Dictionary<HighlightType, Sprite> highlightSprites)
        {
            bingoLayout = [];
            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < 5; column++)
                {
                    var (stack, images) = GenerateSquareBackgroundImage(row, column, backgroundSprite);
                    gridLayout.Children.Add(stack);

                    TextObject textObject = new TextObject(layoutRoot, $"BingoSync_BoardDisplay_square_{row}_{column}_text")
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

                    Dictionary<HighlightType, Image> highlightImages = [];
                    foreach(KeyValuePair<HighlightType, Sprite> entry in highlightSprites)
                    {
                        Image highlightImage = new Image(layoutRoot, entry.Value, $"BingoSync_BoardDisplay_square_{row}_{column}_highlight_{entry.Key}")
                        {
                            Height = 110,
                            Width = 110,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        }.WithProp(GridLayout.Row, row).WithProp(GridLayout.Column, column);
                        gridLayout.Children.Add(highlightImage);
                        highlightImages[entry.Key] = highlightImage;
                    }

                    bingoLayout.Add(new SquareLayoutObjects
                    {
                        Text = textObject,
                        Highlights = highlightImages,
                        BackgroundColors = images,
                    });
                }
            }
        }

        private (StackLayout, Dictionary<string, Image>) GenerateSquareBackgroundImage(int row, int column, Sprite backgroundSprite)
        {
            var stack = new StackLayout(layoutRoot, $"BingoSync_BoardDisplay_background_{row}_{column}")
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Orientation = Orientation.Vertical,
                Spacing = 0,
            }.WithProp(GridLayout.Row, row).WithProp(GridLayout.Column, column);

            var colors = ColorExtensions.GetAllColorNames();
            var images = new Dictionary<string, Image>();
            for (int brow = 0; brow < colors.Count; brow++)
            {
                Color tint = ColorExtensions.FromName(colors[brow]).GetColor();
                var backgroundImage = new Image(layoutRoot, backgroundSprite, $"BingoSync_BoardDisplay_color_{brow}_{row}_{column}")
                {
                    Height = 0,
                    Width = 110,
                    Tint = tint,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                stack.Children.Add(backgroundImage);
                images.Add(colors[brow], backgroundImage);
                backgroundImagesByColor[colors[brow]].Add(backgroundImage);
            }

            return (stack, images);
        }

        public void UpdateColorScheme()
        {
            foreach(string color in ColorExtensions.GetAllColorNames())
            {
                foreach(Image image in backgroundImagesByColor[color])
                {
                    image.Tint = ColorExtensions.FromName(color).GetColor();
                }
            }
        }
    }
}
