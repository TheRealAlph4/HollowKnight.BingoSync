using BingoSync.Clients;
using BingoSync.Clients.EventInfoObjects;
using System;
using System.Collections.Generic;

namespace BingoSync.Sessions
{
    public class Session
    {
        private readonly IRemoteClient _client;
        public bool IsMarking { get; set; }
        public bool RoomIsLockout { get; set; } = false;
        public string Nickname { get; private set; } = string.Empty;
        public Colors Color { get; private set; } = Colors.Orange;
        public BingoBoard Board { get; set; } = new();

        #region Events

        public event EventHandler<CardRevealedBroadcast> OnCardRevealedBroadcastReceived;

        private void RefireCardRevealedBroadcast(object _, CardRevealedBroadcast broadcast)
        {
            OnCardRevealedBroadcastReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<ChatMessage> OnChatMessageReceived;

        private void RefireChatMessage(object _, ChatMessage broadcast)
        {
            OnChatMessageReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<GoalUpdate> OnGoalUpdateReceived;

        private void RefireGoalUpdate(object _, GoalUpdate broadcast)
        {
            OnGoalUpdateReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<NewCardBroadcast> OnNewCardReceived;

        private void RefireNewCard(object _, NewCardBroadcast broadcast)
        {
            OnNewCardReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<PlayerColorChange> OnPlayerColorChangeReceived;

        private void RefirePlayerColorChange(object _, PlayerColorChange broadcast)
        {
            OnPlayerColorChangeReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<PlayerConnectedBroadcast> OnPlayerConnectedBroadcastReceived;

        private void RefirePlayerConnectedBroadcast(object _, PlayerConnectedBroadcast broadcast)
        {
            OnPlayerConnectedBroadcastReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<RoomSettings> OnRoomSettingsReceived;

        private void RefireRoomSettings(object _, RoomSettings broadcast)
        {
            OnRoomSettingsReceived?.Invoke(this, broadcast);
        }

        private void UnsubscribeEventRefires()
        {
            _client.CardRevealedBroadcastReceived -= RefireCardRevealedBroadcast;
            _client.ChatMessageReceived -= RefireChatMessage;
            _client.GoalUpdateReceived -= RefireGoalUpdate;
            _client.NewCardReceived -= RefireNewCard;
            _client.PlayerColorChangeReceived -= RefirePlayerColorChange;
            _client.PlayerConnectedBroadcastReceived -= RefirePlayerConnectedBroadcast;
            _client.RoomSettingsReceived -= RefireRoomSettings;
        }

        private void SubscribeEventRefires()
        {
            _client.CardRevealedBroadcastReceived += RefireCardRevealedBroadcast;
            _client.ChatMessageReceived += RefireChatMessage;
            _client.GoalUpdateReceived += RefireGoalUpdate;
            _client.NewCardReceived += RefireNewCard;
            _client.PlayerColorChangeReceived += RefirePlayerColorChange;
            _client.PlayerConnectedBroadcastReceived += RefirePlayerConnectedBroadcast;
            _client.RoomSettingsReceived += RefireRoomSettings;
        }

    #endregion

        public Session(IRemoteClient client, bool markingClient)
        {
            _client = client;
            SubscribeEventRefires();
            IsMarking = markingClient;
            _client.SetBoard(Board);
            _client.GoalUpdateReceived += GoalUpdateFromServer;
            _client.RoomSettingsReceived += ConsumeRoomSettings;
        }

        public void LocalUpdate()
        {
            Controller.BoardUpdate();
        }

        private void ConsumeRoomSettings(object sender, RoomSettings settings)
        {
            RoomIsLockout = settings.IsLockout;
        }

        private void GoalUpdateFromServer(object sender, GoalUpdate update)
        {
            BingoTracker.GoalUpdated(this, update.Goal, update.Slot);
        }

        public bool IsPlayable()
        {
            Update();
            if (!Board.IsAvailable() || !Board.IsRevealed)
                return false;
            if (!ClientIsConnected())
                return false;
            return true;
        }

        public bool ClientIsConnected()
        {
            return GetClientState() == ClientState.Connected;
        }

        public bool ClientIsConnecting()
        {
            return GetClientState() == ClientState.Loading;
        }

        public void JoinRoom(string roomID, string nickname, string password, Action<Exception> callback)
        {
            if (roomID == null || roomID == string.Empty
                || nickname == null || nickname == string.Empty
                || password == null || password == string.Empty)
            {
                return;
            }

            _client.JoinRoom(roomID, nickname, password, ColorExtensions.FromName(Controller.RoomColor), callback);
            Nickname = nickname;
            Color = ColorExtensions.FromName(Controller.RoomColor);
        }

        public void ExitRoom(Action callback)
        {
            _client.ExitRoom(callback);
        }

        public void Update()
        {
            _client.Update();
        }

        public ClientState GetClientState()
        {
            return _client.GetState();
        }

        public void NewCard(string customJSON, bool lockout = true, bool hideCard = true)
        {
            _client.NewCard(customJSON, lockout, hideCard);
        }

        public void RevealCard()
        {
            _client.RevealCard();
        }

        public void SendChatMessage(string text)
        {
            _client.SendChatMessage(text);
        }

        public void SelectSquare(int square, Action errorCallback, bool clear = false)
        {
            if(IsMarking)
            {
                _client.SelectSquare(square, errorCallback, clear);
            }
        }

        public List<string> RoomHistory()
        {
            return _client.RoomHistory();
        }

        public void DumpDebugInfo()
        {
            _client.DumpDebugInfo();
        }
    }
}
