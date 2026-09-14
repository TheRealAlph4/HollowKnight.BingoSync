namespace BingoSync.Clients.EventInfoObjects
{
    public class RoomEventInfo
    {
        public PlayerInfo Player { get; set; } = new();
        public string Timestamp { get; set; } = string.Empty;
    }
}
