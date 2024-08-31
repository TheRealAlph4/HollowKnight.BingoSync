using InControl;

namespace BingoSync.Settings
{
    public class KeyBinds: PlayerActionSet {
        public PlayerAction ToggleBoard;
        public PlayerAction CycleBoardOpacity;
        public PlayerAction HideMenu;
        public PlayerAction RevealCard;
        public PlayerAction DumpDebugInfo;

        public KeyBinds() {
            ToggleBoard = CreatePlayerAction("ToggleBingoSyncBoard");
            ToggleBoard.AddDefaultBinding(Key.B);
            CycleBoardOpacity = CreatePlayerAction("CycleBingoSyncBoard");
            CycleBoardOpacity.AddDefaultBinding(Key.O);
            HideMenu = CreatePlayerAction("HideBingoSyncMenu");
            HideMenu.AddDefaultBinding(Key.H);
            RevealCard = CreatePlayerAction("RevealBingoSyncCard");
            RevealCard.AddDefaultBinding(Key.R);
            DumpDebugInfo = CreatePlayerAction("DumpDebugInfo");
            DumpDebugInfo.AddDefaultBinding(Key.LeftControl);
        }
    };
}