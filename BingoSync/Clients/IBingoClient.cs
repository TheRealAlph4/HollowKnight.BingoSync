using BingoSync.Clients.ColorManagement;
using System;
using System.Collections.Generic;
using BingoSync.Clients.EventInfoObjects;
using BingoSync.Clients.StateChangeInfoObjects;

namespace BingoSync.Clients
{
    public interface IBingoClient
    {
        public event EventHandler<BoardRevealedEventInfo>? BoardRevealedEventReceived;
        public event EventHandler<ChatMessageEventInfo>? ChatMessageEventReceived;
        public event EventHandler<GoalUpdateEventInfo>? GoalUpdateEventReceived;
        public event EventHandler<NewBoardEventInfo>? NewBoardEventReceived;
        public event EventHandler<PlayerColorChangeEventInfo>? PlayerColorChangeEventReceived;
        public event EventHandler<PlayerConnectionEventInfo>? PlayerConnectionEventReceived;

        public event EventHandler<ClientStateChangedInfo>? OnConnectionStateChanged;
        public event EventHandler<RoomSettings>? OnRoomSettingsChanged;
        public event EventHandler<BoardChangedInfo>? OnBoardChanged;
        public event EventHandler<SquareChangedInfo>? OnSquareChanged;
        public event EventHandler? OnBoardRevealed;

        public string PlayerUUID { get; }
        public IColorManager ColorManager { get; }

        public ClientState GetState();
        public void JoinRoom(string roomID, string nickname, string password, Action? callback = null);
        public void ExitRoom(Action? callback = null);
        public void SetColor(int color, Action? callback = null);
        public void NewBoard(List<string> board, bool lockout = true, bool hideBoard = true, int seed = 0, Action? callback = null);
        public void RevealBoard(Action? callback = null);
        public void SendChatMessage(string text, Action? callback = null);
        public void MarkGoal(int index, int color, bool unmark = false, Action? callback = null);
        public void ProcessRoomHistory(Action<List<RoomEventInfo>> callback);
    }
}
