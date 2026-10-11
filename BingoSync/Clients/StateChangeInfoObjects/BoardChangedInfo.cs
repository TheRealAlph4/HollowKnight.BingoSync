using BingoSync.Sessions;
using System.Collections.Generic;

namespace BingoSync.Clients.StateChangeInfoObjects
{
    public class BoardChangedInfo
    {
        public List<BoardSquare> Board { get; set; } = [];
        public bool HideBoard { get; set; } = false;
    }
}
