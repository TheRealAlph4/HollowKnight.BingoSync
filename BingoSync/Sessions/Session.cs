using BingoSync.Clients;
using BingoSync.Clients.ColorManagement;
using BingoSync.Clients.EventInfoObjects;
using BingoSync.Clients.StateChangeInfoObjects;
using BingoSync.GameUI;
using BingoSync.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static BingoSync.Settings.ModSettings;

namespace BingoSync.Sessions
{
    public class Session
    {
        private readonly IBingoClient _client;
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
        public bool IsAutoMarking { get; set; }
        public bool IsAutoUnmarking { get; set; } = false;
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

        public RoomSettings RoomSettings { get; set; } = new();
        public bool RoomIsLockout => RoomSettings.IsLockout;
        public bool RoomHidBoardInitially => RoomSettings.HideBoard;
        public string RoomLink { get; set; } = string.Empty;
        public string RoomNickname { get; set; } = string.Empty;
        public string RoomPassword { get; set; } = string.Empty;
        public int RoomColor { get; set; } = 0;
        public string RoomPlayerUUID { get
            {
                return _client.PlayerUUID;
            } 
        }
        private SquareManager _squareManager;

        public SquareManager SquareManager
        {
            get
            {
                return _squareManager;
            }
            private set
            {
                _squareManager.OnSquareCanBeMarked -= OnSquareCanBeMarked;
                _squareManager = value;
                _squareManager.OnSquareCanBeMarked -= OnSquareCanBeMarked;
                _squareManager.OnSquareCanBeMarked += OnSquareCanBeMarked;
            }
        }
        public bool NonStandardBoardGeneration { get; set; } = false;
        public IColorManager ColorManager => _client.ColorManager;

        #region Events

        public event EventHandler<BoardRevealedEventInfo>? OnBoardRevealedEventReceived;
        public event EventHandler<ChatMessageEventInfo>? OnChatMessageEventReceived;
        public event EventHandler<GoalUpdateEventInfo>? OnGoalUpdateEventReceived;
        public event EventHandler<NewBoardEventInfo>? OnNewBoardEventReceived;
        public event EventHandler<PlayerColorChangeEventInfo>? OnPlayerColorChangeEventReceived;
        public event EventHandler<PlayerConnectionEventInfo>? OnPlayerConnectedEventReceived;

        public event EventHandler<RoomSettings>? OnRoomSettingsChanged;
        public event EventHandler<ClientStateChangedInfo>? OnClientStateChanged;
        public event EventHandler<BoardChangedInfo>? OnBoardChanged;
        public event EventHandler<SquareChangedInfo>? OnSquareChanged;
        public event EventHandler? OnBoardRevealed;

        private void RefireBoardRevealedBroadcast(object _, BoardRevealedEventInfo broadcast)
        {
            OnBoardRevealedEventReceived?.Invoke(this, broadcast);
        }

        private void RefireChatMessage(object _, ChatMessageEventInfo broadcast)
        {
            OnChatMessageEventReceived?.Invoke(this, broadcast);
        }

        private void RefireGoalUpdate(object _, GoalUpdateEventInfo broadcast)
        {
            OnGoalUpdateEventReceived?.Invoke(this, broadcast);
        }

        private void RefireNewBoard(object _, NewBoardEventInfo broadcast)
        {
            OnNewBoardEventReceived?.Invoke(this, broadcast);
        }

        private void RefirePlayerColorChange(object _, PlayerColorChangeEventInfo broadcast)
        {
            OnPlayerColorChangeEventReceived?.Invoke(this, broadcast);
        }

        private void RefirePlayerConnectedBroadcast(object _, PlayerConnectionEventInfo broadcast)
        {
            OnPlayerConnectedEventReceived?.Invoke(this, broadcast);
        }

        private void RefireRoomSettings(object _, RoomSettings broadcast)
        {
            OnRoomSettingsChanged?.Invoke(this, broadcast);
        }

        private void RefireClientState(object _, ClientStateChangedInfo broadcast)
        {
            OnClientStateChanged?.Invoke(this, broadcast);
        }

        private void RefireBoardChanged(object _, BoardChangedInfo broadcast)
        {
            OnBoardChanged?.Invoke(this, broadcast);
        }

        private void RefireSquareChanged(object _, SquareChangedInfo broadcast)
        {
            OnSquareChanged?.Invoke(this, broadcast);
        }

        private void RefireBoardRevealed(object _, EventArgs __)
        {
            OnBoardRevealed?.Invoke(this, EventArgs.Empty);
        }

        private void UnsubscribeEventRefires()
        {
            _client.BoardRevealedEventReceived -= RefireBoardRevealedBroadcast;
            _client.ChatMessageEventReceived -= RefireChatMessage;
            _client.GoalUpdateEventReceived -= RefireGoalUpdate;
            _client.NewBoardEventReceived -= RefireNewBoard;
            _client.PlayerColorChangeEventReceived -= RefirePlayerColorChange;
            _client.PlayerConnectionEventReceived -= RefirePlayerConnectedBroadcast;
            
            _client.OnConnectionStateChanged -= RefireClientState;
            _client.OnRoomSettingsChanged -= RefireRoomSettings;
            _client.OnBoardChanged -= RefireBoardChanged;
            _client.OnSquareChanged -= RefireSquareChanged;
            _client.OnBoardRevealed -= RefireBoardRevealed;
        }

        private void SubscribeEventRefires()
        {
            UnsubscribeEventRefires();
            _client.BoardRevealedEventReceived += RefireBoardRevealedBroadcast;
            _client.ChatMessageEventReceived += RefireChatMessage;
            _client.GoalUpdateEventReceived += RefireGoalUpdate;
            _client.NewBoardEventReceived += RefireNewBoard;
            _client.PlayerColorChangeEventReceived += RefirePlayerColorChange;
            _client.PlayerConnectionEventReceived += RefirePlayerConnectedBroadcast;

            _client.OnConnectionStateChanged += RefireClientState;
            _client.OnRoomSettingsChanged += RefireRoomSettings;
            _client.OnBoardChanged += RefireBoardChanged;
            _client.OnSquareChanged += RefireSquareChanged;
            _client.OnBoardRevealed += RefireBoardRevealed;
        }

        #endregion

        public Session(string name, IBingoClient client, bool isAutoMarking, bool isAutoUnmarking)
        {
            SessionName = name;
            _client = client;
            _squareManager = new([], false);
            SubscribeEventRefires();

            IsAutoMarking = isAutoMarking;
            IsAutoUnmarking = isAutoUnmarking;

            OnRoomSettingsChanged += ConsumeRoomSettings;
            OnBoardChanged += CreateSquareManagerOnBoardChanged;
            OnSquareChanged += ForwardSquareChanged;
            OnBoardRevealed += RevealInSquareManager;

            OnBoardRevealed += MarkCompletedGoalsOnReveal;
            OnBoardChanged += MarkCompletedGoalsOnNewBoard;

            OnBoardRevealedEventReceived += RevealOnOthersReveal;
            OnGoalUpdateEventReceived += DoAudioNotification;

            ItemSyncInterop.AddSession(this);
        }

        private void RevealInSquareManager(object sender, EventArgs e)
        {
            SquareManager.IsRevealed = true;
        }

        private void ConsumeRoomSettings(object sender, RoomSettings settings)
        {
            RoomSettings = settings;
        }

        private void MarkCompletedGoalsOnNewBoard(object _, BoardChangedInfo __)
        {
            if (!Controller.GlobalSettings.MarkCompletedGoalsOnNewBoardReceived)
            {
                return;
            }
            MarkAllCompleted();
        }

        private void MarkCompletedGoalsOnReveal(object _, EventArgs __)
        {
            if (!Controller.GlobalSettings.MarkCompletedGoalsOnNewBoardReceived)
            {
                return;
            }
            MarkAllCompleted();
        }

        private void MarkAllCompleted()
        {
            if (GameManager.instance.IsMenuScene())
            {
                return;
            }
            if (!IsPlayable())
            {
                return;
            }
            int index = 0;
            foreach (BoardSquare square in SquareManager.AllSquares)
            {
                if (GoalCompletionTracker.IsGoalMarkedByName(square.Name))
                {
                    SelectIndex(index);
                }
                ++index;
            }
        }

        private void ForwardSquareChanged(object _, SquareChangedInfo info)
        {
            SquareManager.SquareUpdateFromServer(info);
        }

        private void OnSquareCanBeMarked(object _, SquareCanBeMarkedInfo info)
        {
            if (!info.Unmark && !IsAutoMarking) return;
            if (info.Unmark && !IsAutoUnmarking) return;

            Task.Run(() =>
            {
                ItemSyncMarkDelay setting = Controller.GlobalSettings.ItemSyncMarkSetting;
                if (setting == ItemSyncMarkDelay.NoMark && info.IsItemSyncUpdate)
                {
                    return;
                }
                if (setting == ItemSyncMarkDelay.Delay && info.IsItemSyncUpdate)
                {
                    Thread.Sleep(ItemSyncInterop.MarkDelay);
                }
                SelectIndex(info.Index, info.Unmark);
            });
        }

        private void CreateSquareManagerOnBoardChanged(object _, BoardChangedInfo info)
        {
            SquareManager = new SquareManager(info.Board);
            SquareManager.IsRevealed = !info.HideBoard;
        }

        private bool CanMarkSquare(BoardSquare square, int color, bool unmark)
        {
            if (!SquareManager.IsRevealed) return false;
            if (unmark)
            {
                if (!square.MarkedBy.Contains(color)) return false;
            }
            else
            {
                if (RoomSettings.IsLockout)
                {
                    if (square.MarkedBy.Count > 0) return false;
                }
                else
                {
                    if (square.MarkedBy.Contains(color)) return false;
                }
            }
            return true;
        }

        public bool IsPlayable()
        {
            Update();
            if (!SquareManager.IsValid || !SquareManager.IsRevealed)
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
            return GetClientState() == ClientState.Connecting;
        }

        public void JoinRoom(string roomID, string nickname, string password, int color, Action? callback = null)
        {
            if (roomID == null || roomID == string.Empty
                || nickname == null || nickname == string.Empty
                || password == null || password == string.Empty)
            {
                return;
            }

            _client.JoinRoom(roomID, nickname, password, () =>
            {
                SetColor(color, () =>
                {
                    RoomNickname = nickname;
                    callback?.Invoke();
                });
            });
        }

        public void ExitRoom(Action? callback = null)
        {
            _client.ExitRoom(callback);
        }

        public void Update()
        {
            Controller.BoardUpdate();
        }

        public ClientState GetClientState()
        {
            return _client.GetState();
        }

        public void SetColor(int color, Action? callback = null)
        {
            _client.SetColor(color, () => {
                RoomColor = color;
                callback?.Invoke();
            });
        }

        public void NewBoard(List<string> board, bool lockout = true, bool hideCard = true, int seed = 0, Action? callback = null)
        {
            _client.NewBoard(board, lockout, hideCard, seed, callback);
        }

        public void RevealBoard(Action? callback = null)
        {
            _client.RevealBoard(callback);
        }

        public void SendChatMessage(string text, Action? callback = null)
        {
            _client.SendChatMessage(text, callback);
        }

        public void SelectIndex(int index, bool unmark = false, Action? callback = null)
        {
            SelectIndex(index, RoomColor, unmark, callback);
        }

        public void SelectIndex(int index, int color, bool unmark = false, Action? callback = null)
        {
            if (CanMarkSquare(SquareManager.GetSquareByIndex(index), color, unmark))
            {
                _client.MarkGoal(index, color, unmark, callback);
            }
        }

        public void ProcessRoomHistory(Action<List<RoomEventInfo>> callback)
        {
           _client.ProcessRoomHistory(callback);
        }

        private void DoAudioNotification(object sender, GoalUpdateEventInfo goalUpdate)
        {
            Session session = (Session) sender;
            if (goalUpdate.Unmark || !SquareManager.IsValid || !SquareManager.IsRevealed || session.HandMode)
            {
                return;
            }
            switch(AudioNotificationOn)
            {
                case AudioNotificationCondition.None:
                    break;

                case AudioNotificationCondition.OtherPlayers:
                    if(goalUpdate.Player.UUID != RoomPlayerUUID)
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

        private void RevealOnOthersReveal(object sender, BoardRevealedEventInfo revealedInfo)
        {
            if (Controller.GlobalSettings.RevealBoardWhenOthersReveal)
            {
                Controller.RevealBoard();
            }
        }

        public void SetDisplaySquaresSelector(Func<List<BoardSquare>, List<BoardSquare>> selector)
        {
            SquareManager.SetDisplaySquaresSelector(selector);
        }

        public void SetDefaultDisplaySquaresSelector()
        {
            SquareManager.SetDefaultDisplaySquaresSelector();
        }
    }
}
