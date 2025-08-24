using System.Collections;
using System.Collections.Generic;

namespace BingoSync.Sessions
{
    public class BingoBoard : IEnumerable<Square>
    {
        private List<Square> _squares;
        public bool IsRevealed { get; set; } = false;
        public bool IsConfirmed { get; set; } = false;

        public void SetSquares(List<Square> squares)
        {
            _squares = squares;
        }

        public bool IsAvailable()
        {
            return _squares != null && _squares.Count == 25;
        }

        public void Clear()
        {
            foreach(Square square in _squares)
            {
                square.MarkedBy.Clear();
                square.MarkedBy.Add(Colors.Blank);
            }
        }

        public Square GetIndex(int index)
        {
            return _squares[index];
        }

        // bingosync.com uses "slot" as a 1-based index, hence the separation internally
        public Square GetSlot(int slot)
        {
            return GetIndex(slot - 1);
        }

        public IEnumerator<Square> GetEnumerator()
        {
            return _squares.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
