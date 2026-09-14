namespace BingoSync.Sessions
{
    public class SquareCanBeMarkedInfo
    {
        public BoardSquare Square { get; set; } = new BoardSquare();
        public int Index { get; set; } = -1;
        public bool Unmark { get; set; } = false;
        public bool IsItemSyncUpdate { get; set; } = false;
    }
}
