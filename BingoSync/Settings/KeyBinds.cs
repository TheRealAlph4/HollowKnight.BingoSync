using InControl;

namespace Settings
{
    public class KeyBinds: PlayerActionSet {
        public PlayerAction ToggleBoard;
        public PlayerAction HideMenu;
        public PlayerAction RevealCard;
        public KeyBinds() {
            ToggleBoard = CreatePlayerAction("ToggleBingoSyncBoard");
            ToggleBoard.AddDefaultBinding(Key.B);
            HideMenu = CreatePlayerAction("HideBingoSyncMenu");
            HideMenu.AddDefaultBinding(Key.H);
            RevealCard = CreatePlayerAction("RevealBingoSyncCard");
            RevealCard.AddDefaultBinding(Key.R);
        }
    };
}