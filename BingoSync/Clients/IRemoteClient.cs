using BingoSync.Clients.EventInfoObjects;
using BingoSync.Sessions;
using System;
using System.Collections.Generic;

namespace BingoSync.Clients
{
    public interface IRemoteClient
    {
        public event EventHandler<CardRevealedEventInfo> CardRevealedBroadcastReceived;
        public event EventHandler<ChatMessageEventInfo> ChatMessageReceived;
        public event EventHandler<GoalUpdateEventInfo> GoalUpdateReceived;
        public event EventHandler<NewCardEventInfo> NewCardReceived;
        public event EventHandler<PlayerColorChangeEventInfo> PlayerColorChangeReceived;
        public event EventHandler<PlayerConnectionEventInfo> PlayerConnectedBroadcastReceived;
        public event EventHandler<RoomSettings> RoomSettingsReceived;
        public void SetBoard(BingoBoard board);
        public void DumpDebugInfo();
        public void Update();
        public ClientState GetState();
        public void JoinRoom(string roomID, string nickname, string password, Colors color, Action<Exception> callback);
        public void NewCard(string customJSON, bool lockout = true, bool hideCard = true);
        public void RevealCard();
        public void SendChatMessage(string text);
        public void SelectSquare(int square, Colors color, Action errorCallback, bool clear = false);
        public void ExitRoom(Action callback);
        public void ProcessRoomHistory(Action<List<RoomEventInfo>> callback, Action errorCallback);
    }
}
