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
        public BingoBoard Board { get; set; } = new();

        #region Events

        public event EventHandler<CardRevealedBroadcast> OnCardRevealedBroadcastReceived
        {
            add
            {
                _client.CardRevealedBroadcastReceived += value;
            }
            remove
            {
                _client.CardRevealedBroadcastReceived -= value;
            }
        }

        public event EventHandler<ChatMessage> OnChatMessageReceived
        {
            add
            {
                _client.ChatMessageReceived += value;
            }
            remove
            {
                _client.ChatMessageReceived -= value;
            }
        }

        public event EventHandler<GoalUpdate> OnGoalUpdateReceived
        {
            add
            {
                _client.GoalUpdateReceived += value;
            }
            remove
            {
                _client.GoalUpdateReceived -= value;
            }
        }

        public event EventHandler<NewCardBroadcast> OnNewCardReceived
        {
            add
            {
                _client.NewCardReceived += value;
            }
            remove
            {
                _client.NewCardReceived -= value;
            }
        }

        public event EventHandler<PlayerColorChange> OnPlayerColorChangeReceived
        {
            add
            {
                _client.PlayerColorChangeReceived += value;
            }
            remove
            {
                _client.PlayerColorChangeReceived -= value;
            }
        }

        public event EventHandler<PlayerConnectedBroadcast> OnPlayerConnectedBroadcastReceived
        {
            add
            {
                _client.PlayerConnectedBroadcastReceived += value;
            }
            remove
            {
                _client.PlayerConnectedBroadcastReceived -= value;
            }
        }

        public event EventHandler<RoomSettings> OnRoomSettingsReceived
        {
            add
            {
                _client.RoomSettingsReceived += value;
            }
            remove
            {
                _client.RoomSettingsReceived -= value;
            }
        }

        #endregion

        public Session(IRemoteClient client, bool markingClient)
        {
            _client = client;
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

            _client.JoinRoom(roomID, nickname, password, callback);
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
