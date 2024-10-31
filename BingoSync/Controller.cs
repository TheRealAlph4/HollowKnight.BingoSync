using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MagicUI.Elements;
using System.Threading;
using BingoSync.ModMenu;
using BingoSync.Settings;
using BingoSync.CustomGoals;
using BingoSync.GameUI;
using System.Linq;

namespace BingoSync
{
    internal static class Controller
    {
        public static readonly string BLANK_COLOR = "blank";

        public static ModSettings GlobalSettings { get; set; } = new ModSettings();

        public static List<BoardSquare> Board { get; set; } = null;
        public static bool MenuIsVisible { get; set; } = true;
        public static bool BoardIsVisible { get; set; } = true;
        public static bool BoardIsConfirmed { get; set; } = false;
        public static bool BoardIsRevealed { get; set; } = false;
        public static string ActiveGameMode { get; set; } = string.Empty;
        public static bool MenuIsLockout
        {
            get
            {
                return MenuUI.IsLockout();
            }
            private set { }
        }
        public static bool HandMode
        {
            get
            {
                return MenuUI.IsHandMode();
            }
            private set { }
        }

        public static string RoomCode { get; set; } = string.Empty;
        public static string RoomPassword { get; set; } = string.Empty;
        public static string RoomNickname { get; set; } = string.Empty;
        public static string RoomColor { get; set; } = string.Empty;
        public static bool RoomIsLockout { get; set; } = false;

        public static bool IsDebugMode
        {
            get
            {
                return GlobalSettings.DebugMode;
            }
            private set { }
        }

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
            string message = $"Revealed my card in hand-mode, my top-left goal is \"{Board[0].Name}\"";
            BingoSyncClient.ChatMessage(message);
        }

        public static void RefreshDefaultsFromUI()
        {
            ConnectionMenuUI.ReadCurrentConnectionInfo();
        }

        public static void UpdateBoardOpacity()
        {
            if (!BoardIsAvailable())
            {
                return;
            }
            GlobalSettings.BoardID = (GlobalSettings.BoardID + 1) % BingoBoardUI.GetBoardCount();
        }

        public static void RevealButtonClicked(Button sender)
        {
            RevealCard();
        }

        public static void RevealKeybindClicked()
        {
            RevealCard();
        }

        public static void RevealCard()
        {
            if (BoardIsRevealed)
            {
                return;
            }
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

        public static (int, bool) GetCurrentSeed()
        {
            return MenuUI.GetSeed();
        }

        public static void RegenerateGameModeButtons()
        {
            GenerationMenuUI.SetupProfileSelection();
            GameModesManager.LoadCustomGameModes();
            GenerationMenuUI.CreateGenerationMenu();
            GenerationMenuUI.SetupGameModeButtons();
        }

        public static void AfterGoalPacksLoaded()
        {
            GameModesManager.LoadCustomGameModes();
            MenuUI.SetupGameModeButtons();
        }

        public static void DumpDebugInfo()
        {
            Log("----------------------------------------------------------------");
            Log("-------------------                          -------------------");
            Log("-------------------   BingoSync Debug Dump   -------------------");
            Log("-------------------                          -------------------");
            Log("----------------------------------------------------------------");

            Log("Controller");
            Log($"\tBoard");
            foreach (string goalname in Board.Select(square => square.Name))
            {
                Log($"\t\tGoal \"{goalname}\"");
            };
            Log($"\tMenuIsVisible = {MenuIsVisible}");
            Log($"\tBoardIsVisible = {BoardIsVisible}");
            Log($"\tBoardIsConfirmed = {BoardIsConfirmed}");
            Log($"\tBoardIsRevealed = {BoardIsRevealed}");
            Log($"\tActiveGameMode = {ActiveGameMode}");
            Log($"\tRoomCode = {RoomCode}");
            Log($"\tRoomPassword = {RoomPassword}");
            Log($"\tRoomNickname = {RoomNickname}");
            Log($"\tRoomColor = {RoomColor}");
            Log($"\tRoomIsLockout = {RoomIsLockout}");


            BingoSyncClient.DumpDebugInfo();

            GameModesManager.DumpDebugInfo();

            Log($"GlobalSettings");
            Log($"\tBoardID = {GlobalSettings.BoardID}");
            Log($"\tRevealCardOnGameStart = {GlobalSettings.RevealCardOnGameStart}");
            Log($"\tRevealCardWhenOthersReveal = {GlobalSettings.RevealCardWhenOthersReveal}");
            Log($"\tUnmarkGoals = {GlobalSettings.UnmarkGoals}");
            Log($"\tDefaultNickname = {GlobalSettings.DefaultNickname}");
            Log($"\tDefaultPassword = {GlobalSettings.DefaultPassword}");
            Log($"\tDefaultColor = {GlobalSettings.DefaultColor}");
            foreach (string gamemode in GlobalSettings.CustomGameModes.Select(gameMode => gameMode.GetDisplayName()))
            {
                Log($"\tCustomGameMode \"{gamemode}\"");
            };
    }

    public static bool RenameActiveGameModeTo(string newName)
        {
            GameMode gameMode = GameModesManager.FindGameModeByDisplayName(ActiveGameMode);
            if(gameMode == null || gameMode.GetType() != typeof(CustomGameMode))
            {
                Log($"Cannot rename non-custom gamemode {ActiveGameMode}");
                return false;
            }
            CustomGameMode customGameMode = (CustomGameMode)gameMode;
            customGameMode.InternalName = newName;
            ActiveGameMode = customGameMode.GetDisplayName();
            return true;
        }

        public static bool IsCustomGameMode(string name)
        {
            return GameModesManager.FindGameModeByDisplayName(name).GetType() == typeof(CustomGameMode);
        }

        public static void SetGenerationButtonEnabled(bool enabled)
        {
            GenerationMenuUI.SetGenerationButtonEnabled(enabled);
        }

        public static void RefreshMenu()
        {
            MainMenu.RefreshMenu();
        }
    }
}
