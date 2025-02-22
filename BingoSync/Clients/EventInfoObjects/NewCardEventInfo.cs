namespace BingoSync.Clients.EventInfoObjects
{
    public class NewCardEventInfo : RoomEventInfo
    {
        public string Game {  get; set; }
        public string Seed { get; set; }
        public bool HideCard { get; set; }
    }
}
