namespace BingoSync.Clients.EventInfoObjects
{
    public class GoalUpdateEventInfo : RoomEventInfo
    {
        public int Color { get; set; }
        public string Goal { get; set; } = string.Empty;
        public int Index {  get; set; } 
        public bool Unmark { get; set; }
    }
}
