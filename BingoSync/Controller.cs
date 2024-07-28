using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MagicUI.Elements;
using System.Threading;

namespace BingoSync
{
    internal static class Controller
    {
        public static readonly string BLANK_COLOR = "blank";

        public static List<BoardSquare> Board { get; set; } = null;
        public static bool MenuIsVisible { get; set; } = true;
        public static bool BoardIsVisible { get; set; } = true;
        public static bool BoardIsConfirmed { get; set; } = false;
        public static bool BoardIsRevealed { get; set; } = false;
        public static int CurrentBoardID { get; set; } = BingoSync.modSettings.BoardID;
        public static bool MenuIsLockout { get; set; } = true;
        public static string ActiveGameMode { get; set; } = string.Empty;
        public static bool HandMode { get; set; } = false;

        public static string RoomCode { get; set; } = string.Empty;
        public static string RoomPassword { get; set; } = string.Empty;
        public static string RoomNickname { get; set; } = string.Empty;
        public static string RoomColor { get; set; } = string.Empty;
        public static bool RoomIsLockout { get; set; } = false;

        private static readonly List<Action> OnBoardUpdateList = [];

        private static Action<string> Log;
        private static readonly Stopwatch timer = new();
        private static readonly TimeSpan showBoardButtonTimeout = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 300);
        private static int showBoardClickCount = 0;

        public static void Setup(Action<string> log)
        {
            Log = log;
            OnBoardUpdate(BingoBoardUI.UpdateGrid);
            OnBoardUpdate(ConfirmTopLeftOnReveal);
        }

        public static void BoardUpdate()
        {
            OnBoardUpdateList.ForEach(f => f());
        }

        public static void OnBoardUpdate(Action func)
        {
            OnBoardUpdateList.Add(func);
        }

        public static bool BoardIsAvailable()
        {
            return !(Board == null);
        }

        public static void ToggleBoardKeybindClicked()
        {
            if (!BoardIsAvailable())
            {
                return;
            }
            if (!HandMode)
            {
                BoardIsVisible = !BoardIsVisible;
                return;
            }
            if (timer.Elapsed < showBoardButtonTimeout)
            {
                ++showBoardClickCount;
            }
            else
            {
                showBoardClickCount = 1;
            }
            if (showBoardClickCount > 2 || BoardIsVisible)
            {
                showBoardClickCount = 0;
                BoardIsVisible = !BoardIsVisible;
            }
            timer.Restart();
        }

        public static void LockoutButtonClicked(Button sender)
        {
            MenuIsLockout = MenuUI.IsLockout();
        }

        public static void HandModeButtonClicked(Button sender)
        {
            HandMode = MenuUI.IsHandMode();
        }

        public static void GenerateButtonClicked(Button sender)
        {
            GameModesManager.Generate();
            Task resetBoardVisibility = new(() =>
            {
                Thread.Sleep(300);
                BoardIsVisible = true;
            });
            resetBoardVisibility.Start();
        }

        public static void ConfirmTopLeftOnReveal()
        {
            if (!HandMode)
            {
                return;
            }
            if (!BoardIsAvailable() || !BoardIsRevealed || BoardIsConfirmed)
            {
                return;
            }
            BoardIsConfirmed = true;
            string message = $"{RoomNickname} revealed their card in hand-mode, their top-left goal is \"{Board[0].Name}\"";
            BingoSyncClient.ChatMessage(message);
        }

        public static void RefreshInfoFromUI()
        {
            ConnectionMenuUI.ReadCurrentConnectionInfo();

        }

        public static void UpdateBoardOpacity()
        {
            CurrentBoardID = (CurrentBoardID + 1) % BingoBoardUI.GetBoardCount();
            BingoSync.modSettings.BoardID = CurrentBoardID;
        }

        public static void RevealButtonClicked(Button sender)
        {
            RevealKeybindClicked();
        }

        public static void RevealKeybindClicked()
        {
            BoardIsConfirmed = false;
            BingoSyncClient.RevealCard();
            if (HandMode)
            {
                BoardIsVisible = false;
            }
        }

        public static bool ClientIsConnected()
        {
            return BingoSyncClient.GetState() == BingoSyncClient.State.Connected;
        }

        public static bool ClientIsConnecting()
        {
            return BingoSyncClient.GetState() == BingoSyncClient.State.Loading;
        }

        public static int GetCurrentSeed()
        {
            return MenuUI.GetSeed();
        }
    }
}
