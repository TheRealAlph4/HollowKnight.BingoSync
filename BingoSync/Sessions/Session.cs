using BingoSync.Clients;
using BingoSync.Clients.EventInfoObjects;
using BingoSync.GameUI;
using System;
using System.Collections.Generic;

namespace BingoSync.Sessions
{
    public class Session
    {
        private readonly IRemoteClient _client;
        private string _sessionName = "Default";
        public string SessionName {
            get
            {
                return _sessionName;
            }
            set
            {
                _sessionName = value;
                Controller.BoardUpdate();
            }
        }
        public bool IsMarking { get; set; }
        public bool BoardIsVisible { get; set; } = true;
        private bool _handMode = false;
        public bool HandMode
        {
            get
            {
                return _handMode;
            }
            set
            {
                _handMode = value;
                if (Controller.ActiveSession == this)
                {
                    Controller.SetHandModeButtonState(value);
                }
            }
        }
        public AudioNotificationCondition AudioNotificationOn { get; set; } = AudioNotificationCondition.None;
        public bool HasCustomAudio { get; set; } = false;
        private int _customAudioClipId = 0;
        public int ActiveAudioId { 
            get
            {
                if (HasCustomAudio)
                {
                    return _customAudioClipId;
                }
                return Controller.GlobalSettings.AudioClipId;
            }
            set
            {
                HasCustomAudio = true;
                _customAudioClipId = value;
            }
        }
        public bool RoomIsLockout { get; set; } = false;
        public string RoomLink { get; set; } = string.Empty;
        public string RoomNickname { get; set; } = string.Empty;
        public string RoomPassword { get; set; } = string.Empty;
        public Colors RoomColor { get; set; } = Colors.Orange;
        public BingoBoard Board { get; set; } = new();

        #region Events

        public event EventHandler<CardRevealedEventInfo> OnCardRevealedBroadcastReceived;

        private void RefireCardRevealedBroadcast(object _, CardRevealedEventInfo broadcast)
        {
            OnCardRevealedBroadcastReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<ChatMessageEventInfo> OnChatMessageReceived;

        private void RefireChatMessage(object _, ChatMessageEventInfo broadcast)
        {
            OnChatMessageReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<GoalUpdateEventInfo> OnGoalUpdateReceived;

        private void RefireGoalUpdate(object _, GoalUpdateEventInfo broadcast)
        {
            OnGoalUpdateReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<NewCardEventInfo> OnNewCardReceived;

        private void RefireNewCard(object _, NewCardEventInfo broadcast)
        {
            OnNewCardReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<PlayerColorChangeEventInfo> OnPlayerColorChangeReceived;

        private void RefirePlayerColorChange(object _, PlayerColorChangeEventInfo broadcast)
        {
            OnPlayerColorChangeReceived?.Invoke(this, broadcast);
        }

        public event EventHandler<PlayerConnectionEventInfo> OnPlayerConnectedBroadcastReceived;

        private void RefirePlayerConnectedBroadcast(object _, PlayerConnectionEventInfo broadcast)
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

        public Session(string name, IRemoteClient client, bool markingClient)
        {
            SessionName = name;
            _client = client;
            SubscribeEventRefires();
            IsMarking = markingClient;
            _client.SetBoard(Board);
            _client.GoalUpdateReceived += GoalUpdateFromServer;
            _client.RoomSettingsReceived += ConsumeRoomSettings;
            OnGoalUpdateReceived += DoAudioNotification;
        }

        public void LocalUpdate()
        {
            Controller.BoardUpdate();
        }

        private void ConsumeRoomSettings(object sender, RoomSettings settings)
        {
            RoomIsLockout = settings.IsLockout;
        }

        private void GoalUpdateFromServer(object sender, GoalUpdateEventInfo update)
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
            RoomNickname = nickname;
            RoomColor = ColorExtensions.FromName(Controller.RoomColor);
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
            SelectSquare(square, RoomColor, errorCallback, clear);
        }

        public void SelectSquare(int square, Colors color, Action errorCallback, bool clear = false)
        {
            if(IsMarking)
            {
                _client.SelectSquare(square, color, errorCallback, clear);
            }
        }

        public void ProcessRoomHistory(Action<List<RoomEventInfo>> callback, Action errorCallback)
        {
            _client.ProcessRoomHistory(callback, errorCallback);
        }

        public void DumpDebugInfo()
        {
            _client.DumpDebugInfo();
        }

        public void DoAudioNotification(object sender, GoalUpdateEventInfo goalUpdate)
        {
            if (goalUpdate.Unmarking || !Board.IsAvailable() || !Board.IsRevealed)
            {
                return;
            }
            switch(AudioNotificationOn)
            {
                case AudioNotificationCondition.None:
                    break;

                case AudioNotificationCondition.OtherPlayers:
                    if(goalUpdate.Player.Name != RoomNickname)
                    {
                        Controller.Audio.Play(ActiveAudioId);
                    }
                    break;

                case AudioNotificationCondition.OtherColors:
                    if (goalUpdate.Player.Color != RoomColor)
                    {
                        Controller.Audio.Play(ActiveAudioId);
                    }
                    break;

                case AudioNotificationCondition.AllGoals:
                    Controller.Audio.Play(ActiveAudioId);
                    break;
            }
        }
    }
}
