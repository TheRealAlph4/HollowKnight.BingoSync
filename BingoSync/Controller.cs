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
using BingoSync.Clients;
using BingoSync.Sessions;

namespace BingoSync
{
    internal static class Controller
    {
        public static ModSettings GlobalSettings { get; set; } = new ModSettings();

        public static ConnectionSession DefaultSession { get; set; }
        public static ConnectionSession ActiveSession { get; set; }
        public static bool IsOnMainMenu { get; set; } = true;
        public static bool MenuIsVisible { get; set; } = true;
        public static bool BoardIsVisible { get; set; } = true;
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
            DefaultSession = new ConnectionSession(new BingoSyncClient(log), true);
            ActiveSession = DefaultSession;
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

        public static void ToggleBoardKeybindClicked()
        {
            if (!ActiveSession.Board.IsAvailable())
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

        public static void GenerateButtonClicked(Button _)
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
            if (!ActiveSession.Board.IsAvailable() || !ActiveSession.Board.IsRevealed || ActiveSession.Board.IsConfirmed)
            {
                return;
            }
            ActiveSession.Board.IsConfirmed = true;
            string message = $"Revealed my card in hand-mode, my top-left goal is \"{ActiveSession.Board.GetSlot(0).Name}\"";
            ActiveSession.SendChatMessage(message);
        }

        public static void RefreshDefaultsFromUI()
        {
            ConnectionMenuUI.ReadCurrentConnectionInfo();
        }

        public static void UpdateBoardOpacity()
        {
            if (!ActiveSession.Board.IsAvailable())
            {
                return;
            }
            GlobalSettings.BoardID = (GlobalSettings.BoardID + 1) % BingoBoardUI.GetBoardCount();
        }

        public static void RevealButtonClicked(Button _)
        {
            RevealCard();
        }

        public static void RevealKeybindClicked()
        {
            RevealCard();
        }

        public static void JoinRoomButtonClicked(Button _)
        {
            if (!ActiveSession.ClientIsConnected())
            {
                ActiveSession.JoinRoom(RoomCode, RoomNickname, RoomPassword, (ex) => {
                    ConnectionMenuUI.Update();
                    RefreshGenerationButtonEnabled();
                });
            }
            else
            {
                ActiveSession.ExitRoom(() => {
                    ConnectionMenuUI.Update();
                    RefreshGenerationButtonEnabled();
                });
            }
        }

        public static void RevealCard()
        {
            if (ActiveSession.Board.IsRevealed)
            {
                return;
            }
            ActiveSession.Board.IsConfirmed = false;
            ActiveSession.RevealCard();
            if (HandMode)
            {
                BoardIsVisible = false;
            }
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
            foreach (string goalname in ActiveSession.Board.Select(square => square.Name))
            {
                Log($"\t\tGoal \"{goalname}\"");
            };
            Log($"\tMenuIsVisible = {MenuIsVisible}");
            Log($"\tBoardIsVisible = {BoardIsVisible}");
            Log($"\tBoardIsConfirmed = {ActiveSession.Board.IsConfirmed}");
            Log($"\tBoardIsRevealed = {ActiveSession.Board.IsRevealed}");
            Log($"\tActiveGameMode = {ActiveGameMode}");
            Log($"\tRoomCode = {RoomCode}");
            Log($"\tRoomPassword = {RoomPassword}");
            Log($"\tRoomNickname = {RoomNickname}");
            Log($"\tRoomColor = {RoomColor}");
            Log($"\tRoomIsLockout = {ActiveSession.RoomIsLockout}");


            ActiveSession.DumpDebugInfo();

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

        public static void RefreshGenerationButtonEnabled()
        {
            SetGenerationButtonEnabled((ActiveSession.ClientIsConnected() || ActiveSession.ClientIsConnecting()) && IsOnMainMenu);
        }

        public static void RefreshMenu()
        {
            MainMenu.RefreshMenu();
        }
    }
}
