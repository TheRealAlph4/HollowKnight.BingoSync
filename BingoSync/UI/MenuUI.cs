using MagicUI.Core;

namespace BingoSync
{
    public static class MenuUI
    {
        public static readonly int colorButtonWidth = 100;
        public static readonly int lockoutButtonWidth = 50;
        public static readonly int handModeButtonWidth = 43;
        public static readonly int textFieldWidth = colorButtonWidth * 5 + 40;
        public static readonly int joinRoomButtonWidth = textFieldWidth - handModeButtonWidth - 10;
        public static readonly int gameModeButtonWidth = (textFieldWidth - 20) / 3;
        public static readonly int generateButtonWidth = (int)(1.5 * gameModeButtonWidth) + 10;
        public static readonly int seedFieldWidth = textFieldWidth - generateButtonWidth - lockoutButtonWidth - 20;
        public static readonly int profileNameFieldWidth = (int)(0.75 * textFieldWidth);
        public static readonly int acceptProfileNameButtonWidth = textFieldWidth - profileNameFieldWidth - 10;
        public static readonly int fontSize = 22;


        private static readonly LayoutRoot layoutRoot = new(true, "Persistent layout")
        {
            // this is needed to hide the menu while the game is loading
            VisibilityCondition = () => false,
        };

        public static void Setup()
        {
            ConnectionMenuUI.Setup(layoutRoot);
            GenerationMenuUI.Setup(layoutRoot);


            layoutRoot.VisibilityCondition = () => {
                BingoSyncClient.Update();
                ConnectionMenuUI.Update();
                return Controller.MenuIsVisible;
            };

            layoutRoot.ListenForPlayerAction(BingoSync.modSettings.Keybinds.HideMenu, () => {
                Controller.MenuIsVisible = !Controller.MenuIsVisible;
            });
        }

        public static void LoadDefaults()
        {
            ConnectionMenuUI.LoadDefaults();
        }

        public static bool IsLockout()
        {
            return GenerationMenuUI.LockoutToggleButtonIsOn();
        }

        public static bool IsHandMode()
        {
            return ConnectionMenuUI.HandModeToggleButtonIsOn();
        }

        public static int GetSeed()
        { 
            return GenerationMenuUI.GetSeed();
        }
        public static void SetupGameModeButtons()
        {
            GenerationMenuUI.SetupGameModeButtons();
        }
    }
}