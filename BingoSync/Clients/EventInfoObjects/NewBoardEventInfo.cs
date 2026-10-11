namespace BingoSync.Clients.EventInfoObjects
{
    public class NewBoardEventInfo : RoomEventInfo
    {
        public string Game {  get; set; } = string.Empty;
        public string Seed { get; set; } = string.Empty;
        public bool HideBoard { get; set; }
    }
}
