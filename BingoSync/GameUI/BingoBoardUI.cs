using BingoSync.Sessions;
using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BingoSync.GameUI
{
    internal static class BingoBoardUI
    {
        private static DisplayBoard board;

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

        public static void Setup(Action<string> log)
        {
            Log = log;

            commonRoot.VisibilityCondition = () => true;

            revealCardButton.Click += Controller.RevealButtonClicked;

            Loader.Preload();

            Sprite backgroundSprite = Loader.GetTexture("BingoSync Background.png").ToSprite();
            Sprite highlightSprite = Loader.GetTexture("BingoSync Background Highlight.png").ToSprite();

            board = new DisplayBoard(backgroundSprite, highlightSprite);

            commonRoot.ListenForPlayerAction(Controller.GlobalSettings.Keybinds.ToggleBoard, Controller.ToggleBoardKeybindClicked);
            commonRoot.ListenForPlayerAction(Controller.GlobalSettings.Keybinds.RevealCard, Controller.RevealKeybindClicked);
            commonRoot.ListenForPlayerAction(Controller.GlobalSettings.Keybinds.CycleBoardOpacity, Controller.CycleBoardOpacity);
        }

        public static void UpdateGrid()
        {
            loadingText.Visibility = (!Controller.ActiveSession.Board.IsAvailable() && Controller.ActiveSession.ClientIsConnecting()) ? Visibility.Visible : Visibility.Hidden;
            revealCardButton.Visibility = (Controller.ActiveSession.ClientIsConnected() && Controller.ActiveSession.Board.IsAvailable() && !Controller.ActiveSession.Board.IsRevealed) ? Visibility.Visible : Visibility.Hidden;

            if (!Controller.ActiveSession.Board.IsAvailable())
            {
                return;
            }

            foreach (Square square in Controller.ActiveSession.Board)
            {
                board.bingoLayout[square.GoalNr].Text.Text = square.Name;
                board.bingoLayout[square.GoalNr].BackgroundColors.Values.ToList().ForEach(img => img.Height = 0);
                foreach (Colors color in square.MarkedBy)
                {
                    board.bingoLayout[square.GoalNr].BackgroundColors[color.GetName()].Height = 110 / square.MarkedBy.Count;
                }
                board.bingoLayout[square.GoalNr].Highlight.Height = square.Highlighted ? 110 : 0;
            }
        }

        public static void SetBoardAlpha(float alpha)
        {
            board.SetAlpha(alpha);
        }
    }
}