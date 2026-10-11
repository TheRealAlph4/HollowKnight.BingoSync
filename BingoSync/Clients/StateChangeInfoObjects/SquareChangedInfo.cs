namespace BingoSync.Clients.StateChangeInfoObjects
{
    public class SquareChangedInfo
    {
        public int Color { get; set; }
        public string Goal { get; set; } = string.Empty;
        public int Index { get; set; }
        public bool Unmark { get; set; }
    }
}
