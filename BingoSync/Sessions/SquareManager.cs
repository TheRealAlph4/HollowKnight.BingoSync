using BingoSync.Clients.StateChangeInfoObjects;
using System;
using System.Collections.Generic;

namespace BingoSync.Sessions
{
    public class SquareManager
    {
        private readonly List<BoardSquare> _squares;
        public bool IsRevealed { get; set; } = false;
        public bool IsConfirmed { get; set; } = false;
        public bool IsValid { get; private set; }

        public event EventHandler<SquareCanBeMarkedInfo>? OnSquareCanBeMarked;

        public List<BoardSquare> AllSquares => _squares;
        public List<BoardSquare> SquaresToDisplay => DisplaySquaresSelector(_squares);

        private Func<List<BoardSquare>, List<BoardSquare>> DisplaySquaresSelector = DefaultDisplaySquaresSelector;

        public SquareManager(List<BoardSquare> board, bool valid = true)
        {
            _squares = board;
            IsValid = valid;
            GoalCompletionTracker.OnGoalCompletionChanged += OnLocalGoalCompleted;
        }

        private void OnLocalGoalCompleted(object _, GoalCompletionTracker.InternalGoalUpdate goalUpdate)
        {
            if (!IsValid) return;

            int index = _squares.FindIndex(square => square.Name == goalUpdate.Name);
            if (index < 0) return;

            OnSquareCanBeMarked?.Invoke(this, new SquareCanBeMarkedInfo()
            {
                Square = _squares[index],
                Index = index,
                Unmark = goalUpdate.Unmark,
                IsItemSyncUpdate = goalUpdate.IsItemSyncUpdate,
            });
        }

        public void SquareUpdateFromServer(SquareChangedInfo info)
        {
            if (!IsValid) return;

            BoardSquare square = _squares[info.Index];
            if (info.Unmark)
            {
                square.MarkedBy.Remove(info.Color);
            }
            else
            {
                square.MarkedBy.Add(info.Color);
            }
        }

        private static List<BoardSquare> DefaultDisplaySquaresSelector(List<BoardSquare> allSquares)
        {
            return allSquares;
        }

        public void SetDisplaySquaresSelector(Func<List<BoardSquare>, List<BoardSquare>> selector)
        {
            DisplaySquaresSelector = selector ?? DefaultDisplaySquaresSelector;
        }

        public void SetDefaultDisplaySquaresSelector()
        {
            DisplaySquaresSelector = DefaultDisplaySquaresSelector;
        }

        public void Clear()
        {
            foreach(BoardSquare square in _squares)
            {
                square.MarkedBy.Clear();
            }
        }

        public BoardSquare GetSquareByIndex(int index)
        {
            if (!IsValid) throw new InvalidOperationException($"Attempting to access square {index} on empty SquareManager");
            return _squares[index];
        }

        public BoardSquare GetSquareByName(string name)
        {
            if (!IsValid) throw new InvalidOperationException($"Attempting to access square '{name}' on empty SquareManager");
            return _squares.Find(square => square.Name == name);
        }
    }
}
