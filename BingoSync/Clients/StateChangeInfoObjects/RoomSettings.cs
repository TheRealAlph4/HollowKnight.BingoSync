namespace BingoSync.Clients.StateChangeInfoObjects
{
    public class RoomSettings
    {
        public string GameName { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public bool HideBoard { get; set; }
        public bool IsLockout { get; set; }
        public int Seed { get; set; }
    }
}
