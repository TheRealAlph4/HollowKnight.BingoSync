using BingoSync.Clients.ColorManagement;
using BingoSync.Clients.EventInfoObjects;
using BingoSync.Clients.StateChangeInfoObjects;
using BingoSync.Helpers;
using BingoSync.Sessions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BingoSync.Clients
{
    internal class BingoSyncClient : IBingoClient
    {
        private static readonly BingoSyncColorManager _colorManager = new();
        public IColorManager ColorManager => _colorManager;

        private const int MAX_RETRIES = 30;

        private readonly HttpClient httpClient;
        private ClientWebSocket webSocketClient;
        private string socketKey = string.Empty;

        private ClientState stateOverride = ClientState.None;

        private string currentRoomID = string.Empty;
        public string PlayerUUID { get; private set; } = string.Empty;
        private RoomSettings roomSettings = new();

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

        public BingoSyncClient()
        {
            CookieContainer cookieContainer = new();
            HttpClientHandler clientHandler = new()
            {
                CookieContainer = cookieContainer
            };
            httpClient = new HttpClient(clientHandler)
            {
                BaseAddress = new Uri("https://bingosync.com"),
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"HollowKnight.BingoSync/{BingoSync.version}");
            LoadCookie(cookieContainer);

            webSocketClient = new ClientWebSocket();
        }

        private void LoadCookie(CookieContainer cookieContainer)
        {
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var task = httpClient.GetAsync("");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> values))
                    {
                        foreach (string cookieHeader in values)
                        {
                            string[] cookieParts = cookieHeader.Split(';');
                            string cookieName = cookieParts[0].Split('=')[0];
                            string cookieValue = cookieParts[0].Split('=')[1];

                            Cookie cookie = new(cookieName.Trim(), cookieValue.Trim(), "/", response.RequestMessage.RequestUri.Host);
                            cookieContainer.Add(response.RequestMessage.RequestUri, cookie);
                        }
                    }
                });
            }, MAX_RETRIES, nameof(LoadCookie), () => { });
        }

        public ClientState GetState()
        {
            if (stateOverride != ClientState.None)
                return stateOverride;
            if (webSocketClient.State == WebSocketState.Open)
                return ClientState.Connected;
            else if (webSocketClient.State == WebSocketState.Connecting)
                return ClientState.Connecting;
            return ClientState.Disconnected;
        }

        public void JoinRoom(string roomID, string nickname, string password, Action? callback = null)
        {
            if (GetState() == ClientState.Connecting || GetState() == ClientState.Connected)
            {
                return;
            }
            stateOverride = ClientState.Connecting;
            currentRoomID = roomID;

            NetworkObjectJoinRoomRequest joinRoomInput = new()
            {
                Room = roomID,
                Nickname = nickname,
                Password = password,
            };
            string payload = JsonConvert.SerializeObject(joinRoomInput);
            StringContent content = new(payload, Encoding.UTF8, "application/json");
            Task<HttpResponseMessage> task = httpClient.PostAsync("api/join-room", content);
            _ = task.ContinueWith(responseTask =>
            {
                try
                {
                    HttpResponseMessage response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    Task<string> readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(joinRoomResponse =>
                    {
                        NetworkObjectSocketJoinRequest socketJoin = JsonConvert.DeserializeObject<NetworkObjectSocketJoinRequest>(joinRoomResponse.Result) ?? throw new Exception("SocketJoin request is null");
                        socketKey = socketJoin.SocketKey;
                        RequestPlayerUUID();
                        ConnectToBroadcastSocket(socketJoin, () =>
                        {
                            UpdateSettings(() =>
                            {
                                GetNewBoard(roomSettings.HideBoard);
                                callback?.Invoke();
                            });
                        });
                    });
                }
                catch (Exception ex)
                {
                    Log.Error($"could not join room: {ex.Message}");
                    stateOverride = ClientState.None;
                }
            });
        }

        private void ConnectToBroadcastSocket(NetworkObjectSocketJoinRequest socketJoin, Action? callback = null)
        {
            var socketUri = new Uri("wss://sockets.bingosync.com/broadcast");
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                webSocketClient = new ClientWebSocket();
                var connectTask = webSocketClient.ConnectAsync(socketUri, CancellationToken.None);
                return connectTask.ContinueWith(connectResponse =>
                {
                    if (connectResponse.Exception != null)
                    {
                        Log.Error($"Error connecting to websocket: {connectResponse.Exception}");
                        throw connectResponse.Exception;
                    }
                    var serializedSocketJoin = JsonConvert.SerializeObject(socketJoin);
                    var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(serializedSocketJoin));
                    var sendTask = webSocketClient.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    sendTask.ContinueWith(_ =>
                    {
                        stateOverride = ClientState.None;
                        callback?.Invoke();
                        OnConnectionStateChanged?.Invoke(this, new ClientStateChangedInfo() { NewClientState = GetState() });
                        ListenForBoardUpdates(socketJoin);
                    });
                });
            }, MAX_RETRIES, nameof(ConnectToBroadcastSocket));
        }

        private async void ListenForBoardUpdates(NetworkObjectSocketJoinRequest socketJoin)
        {
            var buffer = new byte[1024];
            while (webSocketClient.State == WebSocketState.Open)
            {
                try
                {
                    var response = await webSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (response.MessageType != WebSocketMessageType.Text)
                    {
                        continue;
                    }
                    string json = Encoding.UTF8.GetString(buffer, 0, response.Count);
                    NetworkObjectBroadcast broadcast = JsonConvert.DeserializeObject<NetworkObjectBroadcast>(json) ?? throw new Exception("Broadcast object is null");
                    switch (broadcast.Type)
                    {
                        case "chat": HandleChatBroadcast(json); break;
                        case "new-card": HandleNewBoardBroadcast(json); break;
                        case "goal": HandleGoalBroadcast(json); break;
                        case "color": HandleColorBroadcast(json); break;
                        case "revealed": HandleRevealedBroadcast(json); break;
                        case "connection": HandleConnectionBroadcast(json); break;
                        default: Log.Warn($"Received unknown broadcast type \"{broadcast.Type}\""); break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"'{ex.GetType().FullName}' error with message '{ex.Message}' while handling socket broadcast.\nStacktrace: \n{ex.StackTrace}");
                }
            }
            if (GetState() == ClientState.Connecting || GetState() == ClientState.Connected)
            {
                Log.Warn($"Socket is closed, reconnecting...");
                ConnectToBroadcastSocket(socketJoin);
                return;
            }
        }

        private void HandleChatBroadcast(string json)
        {
            NetworkObjectChatBroadcast chatBroadcast = JsonConvert.DeserializeObject<NetworkObjectChatBroadcast>(json) ?? throw new Exception("Chat broadcast object is null");
            ChatMessageEventReceived?.Invoke(this, NetworkChatBroadcastToLocal(chatBroadcast));
        }

        private void HandleNewBoardBroadcast(string json)
        {
            NetworkObjectNewBoardBroadcast newBoardBroadcast = JsonConvert.DeserializeObject<NetworkObjectNewBoardBroadcast>(json) ?? throw new Exception("NewBoard broadcast object is null");
            UpdateSettings(delegate { GetNewBoard(newBoardBroadcast.HideBoard); });
            NewBoardEventReceived?.Invoke(this, NetworkNewBoardBroadcastToLocal(newBoardBroadcast));
        }

        private void HandleGoalBroadcast(string json)
        {
            NetworkObjectGoalBroadcast goalBroadcast = JsonConvert.DeserializeObject<NetworkObjectGoalBroadcast>(json) ?? throw new Exception("Goal broadcast object is null");
            GoalUpdateEventInfo info = NetworkGoalBroadcastToLocal(goalBroadcast);
            OnSquareChanged?.Invoke(this, new SquareChangedInfo()
            {
                Index = info.Index,
                Color = info.Color,
                Goal = info.Goal,
                Unmark = info.Unmark,
            });
            GoalUpdateEventReceived?.Invoke(this, info);
        }

        private void HandleColorBroadcast(string json)
        {
            NetworkObjectColorBroadcast colorBroadcast = JsonConvert.DeserializeObject<NetworkObjectColorBroadcast>(json) ?? throw new Exception("Color broadcast object is null");
            PlayerColorChangeEventReceived?.Invoke(this, NetworkColorBroadcastToLocal(colorBroadcast));
        }

        private void HandleRevealedBroadcast(string json)
        {
            NetworkObjectRevealedBroadcast revealedBroadcast = JsonConvert.DeserializeObject<NetworkObjectRevealedBroadcast>(json) ?? throw new Exception("Revealed broadcast object is null");
            BoardRevealedEventReceived?.Invoke(this, NetworkRevealedBroadcastToLocal(revealedBroadcast));
        }

        private void HandleConnectionBroadcast(string json)
        {
            NetworkObjectConnectionBroadcast connectionBroadcast = JsonConvert.DeserializeObject<NetworkObjectConnectionBroadcast>(json) ?? throw new Exception("Connection broadcast object is null");
            PlayerConnectionEventReceived?.Invoke(this, NetworkConnectionBroadcastToLocal(connectionBroadcast));
        }

        private void RequestPlayerUUID(Action? callback = null)
        {
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var requestTask = httpClient.GetAsync($"https://bingosync.com/api/socket/{socketKey}");
                return requestTask.ContinueWith(response =>
                {
                    HttpResponseMessage result = response.Result;
                    result.EnsureSuccessStatusCode();
                    result.Content.ReadAsStringAsync().ContinueWith(networkSocketCheck =>
                    {
                        NetworkObjectSocketCheck socketInfo = JsonConvert.DeserializeObject<NetworkObjectSocketCheck>(networkSocketCheck.Result) ?? throw new Exception("SocketInfo response is null");
                        PlayerUUID = socketInfo.PlayerUUID;
                        callback?.Invoke();
                    });

                });
            }, MAX_RETRIES, nameof(RequestPlayerUUID));
        }

        private void GetNewBoard(bool hideBoard, Action? callback = null)
        {
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var task = httpClient.GetAsync($"room/{currentRoomID}/board");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(boardResponse =>
                    {
                        List<NetworkObjectBoardSquare> newBoard = JsonConvert.DeserializeObject<List<NetworkObjectBoardSquare>>(boardResponse.Result) ?? throw new Exception("Board response is null");
                        newBoard.Sort((left, right) => int.Parse(left.Slot.Substring(4)).CompareTo(int.Parse(right.Slot.Substring(4))));
                        callback?.Invoke();
                        OnBoardChanged?.Invoke(this, new BoardChangedInfo()
                        {
                            Board = [.. newBoard.Select(networkSquare => NetworkBoardSquareToLocal(networkSquare))],
                            HideBoard = hideBoard,
                        });
                    });
                });
            }, MAX_RETRIES, nameof(GetNewBoard));
        }

        private void UpdateSettings(Action? callback = null)
        {
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var task = httpClient.GetAsync($"room/{currentRoomID}/room-settings");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(settingsResponse =>
                    {
                        var settings = JsonConvert.DeserializeObject<NetworkObjectRoomSettingsResponse>(settingsResponse.Result) ?? throw new Exception("Settings response is null");
                        RoomSettings localSettings = NetworkRoomSettingsToLocal(settings);
                        roomSettings = localSettings;
                        callback?.Invoke();
                        OnRoomSettingsChanged?.Invoke(this, localSettings);
                    });
                });
            }, MAX_RETRIES, nameof(UpdateSettings));
        }

        public void SetColor(int color, Action? callback = null)
        {
            if (GetState() != ClientState.Connected) return;
            var setColorInput = new NetworkObjectSetColorRequest
            {
                Room = currentRoomID,
                Color = ColorManager.NameOf(color),
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(setColorInput);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = httpClient.PutAsync("api/color", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    callback?.Invoke();
                });
            }, MAX_RETRIES, nameof(SetColor));
        }

        public void NewBoard(List<string> board, bool lockout = true, bool hideBoard = true, int seed = 0, Action? callback = null)
        {
            if (GetState() != ClientState.Connected) return;
            var newBoard = new NetworkObjectNewBoardRequest
            {
                Room = currentRoomID,
                Game = 18, // this is supposed to be custom already
                Variant = 18, // but this is also required for custom ???
                CustomJSON = JsonifyBoard(board),
                Lockout = !lockout, // false is lockout here for some godforsaken reason
                Seed = $"{seed}",
                HideBoard = hideBoard,
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(newBoard);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = httpClient.PostAsync("api/new-card", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    callback?.Invoke();
                });
            }, MAX_RETRIES, nameof(NewBoard));
        }

        private static string JsonifyBoard(List<string> board)
        {
            string output = "[";
            for (int i = 0; i < board.Count; i++)
            {
                output += "{\"name\": \"" + board.ElementAt(i) + "\"}" + (i < 24 ? "," : "");
            }
            output += "]";
            return output;
        }

        public void RevealBoard(Action? callback = null)
        {
            if (GetState() != ClientState.Connected) return;
            var revealInput = new NetworkObjectRevealRequest
            {
                Room = currentRoomID,
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(revealInput);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = httpClient.PutAsync("api/revealed", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    callback?.Invoke();
                    OnBoardRevealed?.Invoke(this, EventArgs.Empty);
                });
            }, MAX_RETRIES, nameof(RevealBoard));
        }

        public void SendChatMessage(string text, Action? callback = null)
        {
            if (GetState() != ClientState.Connected) return;
            var chatMessageInput = new NetworkObjectChatMessageRequest
            {
                Room = currentRoomID,
                Text = text,
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(chatMessageInput);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = httpClient.PutAsync("api/chat", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    callback?.Invoke();
                });
            }, MAX_RETRIES, nameof(SendChatMessage));
        }

        public void MarkGoal(int index, int color, bool unmark = false, Action? callback = null)
        {
            if (GetState() != ClientState.Connected) return;
            var selectInput = new NetworkObjectSelectRequest
            {
                Room = currentRoomID,
                Slot = index + 1,
                Color = ColorManager.NameOf(color),
                RemoveColor = unmark,
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(selectInput);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = httpClient.PutAsync("api/select", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    callback?.Invoke();
                });
            }, MAX_RETRIES, nameof(MarkGoal));
        }

        public void ExitRoom(Action? callback = null)
        {
            if (GetState() != ClientState.Connected) return;
            stateOverride = ClientState.Disconnecting;
            OnConnectionStateChanged?.Invoke(this, new ClientStateChangedInfo() { NewClientState = GetState() });
            currentRoomID = string.Empty;
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                return webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "exiting room", CancellationToken.None).ContinueWith(result =>
                {
                    if (result.Exception != null)
                    {
                        throw result.Exception;
                    }
                    AfterExitRoom();
                    callback?.Invoke();
                });
            }, MAX_RETRIES, nameof(ExitRoom), () =>
            {
                AfterExitRoom();
            });
        }

        private void AfterExitRoom()
        {
            webSocketClient = new ClientWebSocket();
            stateOverride = ClientState.None;
            PlayerUUID = string.Empty;
            OnConnectionStateChanged?.Invoke(this, new ClientStateChangedInfo() { NewClientState = GetState() });
            OnBoardChanged?.Invoke(this, new BoardChangedInfo()
            {
                Board = []
            });
        }

        public void ProcessRoomHistory(Action<List<RoomEventInfo>> callback)
        {
            if (GetState() != ClientState.Connected) return;
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var task = httpClient.GetAsync($"room/{currentRoomID}/feed");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    Task<string> readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(stringResponse =>
                    {
                        List<RoomEventInfo> events = ParseRoomHistory(stringResponse.Result);
                        callback(events);
                    });
                });
            }, MAX_RETRIES, nameof(ProcessRoomHistory));
        }

        private static List<RoomEventInfo> ParseRoomHistory(string json)
        {
            UnparsedRoomFeed unparsedFeed = JsonConvert.DeserializeObject<UnparsedRoomFeed>(json) ?? throw new Exception("Room feed is null");
            List<RoomEventInfo> events = [];
            foreach (JObject unparsedEvent in unparsedFeed.Events)
            {
                string type = unparsedEvent?.Property("type")?.Value.ToString() ?? throw new Exception("Event type is null");
                RoomEventInfo? parsedEvent = type switch
                {
                    "chat" => NetworkChatBroadcastToLocal(JsonConvert.DeserializeObject<NetworkObjectChatBroadcast>(unparsedEvent.ToString()) ?? throw new Exception("Chat event is null")),
                    "new-card" => NetworkNewBoardBroadcastToLocal(JsonConvert.DeserializeObject<NetworkObjectNewBoardBroadcast>(unparsedEvent.ToString()) ?? throw new Exception("New board event is null")),
                    "goal" => NetworkGoalBroadcastToLocal(JsonConvert.DeserializeObject<NetworkObjectGoalBroadcast>(unparsedEvent.ToString()) ?? throw new Exception("Goal event is null")),
                    "color" => NetworkColorBroadcastToLocal(JsonConvert.DeserializeObject<NetworkObjectColorBroadcast>(unparsedEvent.ToString()) ?? throw new Exception("Color event is null")),
                    "revealed" => NetworkRevealedBroadcastToLocal(JsonConvert.DeserializeObject<NetworkObjectRevealedBroadcast>(unparsedEvent.ToString()) ?? throw new Exception("Revealed event is null")),
                    "connection" => NetworkConnectionBroadcastToLocal(JsonConvert.DeserializeObject<NetworkObjectConnectionBroadcast>(unparsedEvent.ToString()) ?? throw new Exception("Connection event is null")),
                    _ => null,
                };
                if (parsedEvent != null)
                {
                    events.Add(parsedEvent);
                }
            }
            return events;
        }

        [DataContract]
        private class UnparsedRoomFeed
        {
            [JsonProperty("events")]
            public List<JObject> Events = [];
            [JsonProperty("allIncluded")]
            public bool FullFeed = false;
        }

        #region Network objects to internal broadcast objects

        private static RoomSettings NetworkRoomSettingsToLocal(NetworkObjectRoomSettingsResponse network)
        {
            return new RoomSettings()
            {
                HideBoard = network.Settings.HideBoard,
                IsLockout = network.Settings.LockoutMode == "Lockout",
                GameName = network.Settings.GameName,
                VariantName = network.Settings.VariantName,
                Seed = network.Settings.Seed,
            };
        }

        private static PlayerInfo NetworkPlayerBroadcastToLocal(NetworkObjectPlayer network)
        {
            return new PlayerInfo()
            {
                UUID = network.UUID,
                Name = network.Name,
                Color = _colorManager.NumberOf(network.Color),
                IsSpectator = network.IsSpectator,
            };
        }

        private static ChatMessageEventInfo NetworkChatBroadcastToLocal(NetworkObjectChatBroadcast network)
        {
            return new ChatMessageEventInfo()
            {
                Player = NetworkPlayerBroadcastToLocal(network.Player),
                Timestamp = network.Timestamp,
                Text = network.Text,
            };
        }

        private static NewBoardEventInfo NetworkNewBoardBroadcastToLocal(NetworkObjectNewBoardBroadcast network)
        {
            return new NewBoardEventInfo()
            {
                Player = NetworkPlayerBroadcastToLocal(network.Player),
                Timestamp = network.Timestamp,
                Game = network.Game,
                Seed = network.Seed,
                HideBoard = network.HideBoard,
            };
        }

        private static BoardSquare NetworkBoardSquareToLocal(NetworkObjectBoardSquare network)
        {
            HashSet<int> markedBy = [];
            foreach (string colorStr in network.Colors.Split(' '))
            {
                int color = _colorManager.NumberOf(colorStr);
                if (color >= 0)
                {
                    markedBy.Add(color);
                }
            }
            return new BoardSquare()
            {
                Name = network.Name,
                MarkedBy = markedBy,
            };
        }

        private static GoalUpdateEventInfo NetworkGoalBroadcastToLocal(NetworkObjectGoalBroadcast network)
        {
            return new GoalUpdateEventInfo()
            {
                Player = NetworkPlayerBroadcastToLocal(network.Player),
                Timestamp = network.Timestamp,
                Color = _colorManager.NumberOf(network.Color),
                Goal = network.Square.Name,
                Index = int.Parse(network.Square.Slot.Substring(4)) - 1,
                Unmark = network.Remove,
            };
        }

        private static PlayerColorChangeEventInfo NetworkColorBroadcastToLocal(NetworkObjectColorBroadcast network)
        {
            return new PlayerColorChangeEventInfo()
            {
                Player = NetworkPlayerBroadcastToLocal(network.Player),
                Timestamp = network.Timestamp,
                Color = _colorManager.NumberOf(network.Color),
            };
        }

        private static BoardRevealedEventInfo NetworkRevealedBroadcastToLocal(NetworkObjectRevealedBroadcast network)
        {
            return new BoardRevealedEventInfo()
            {
                Player = NetworkPlayerBroadcastToLocal(network.Player),
                Timestamp = network.Timestamp,
            };
        }

        private static PlayerConnectionEventInfo NetworkConnectionBroadcastToLocal(NetworkObjectConnectionBroadcast network)
        {
            return new PlayerConnectionEventInfo()
            {
                Player = NetworkPlayerBroadcastToLocal(network.Player),
                Timestamp = network.Timestamp,
                IsDisconnect = network.EventType == "disconnected",
            };
        }

        #endregion

        #region Request objects

        [DataContract]
        class NetworkObjectSetColorRequest
        {
            [JsonProperty("room")]
            public string Room = string.Empty;
            [JsonProperty("color")]
            public string Color = string.Empty;
        }

        [DataContract]
        class NetworkObjectRevealRequest
        {
            [JsonProperty("room")]
            public string Room = string.Empty;
        }

        [DataContract]
        class NetworkObjectSelectRequest
        {
            [JsonProperty("room")]
            public string Room = string.Empty;
            [JsonProperty("slot")]
            public int Slot;
            [JsonProperty("color")]
            public string Color = string.Empty;
            [JsonProperty("remove_color")]
            public bool RemoveColor;
        }

        [DataContract]
        class NetworkObjectJoinRoomRequest
        {
            [JsonProperty("room")]
            public string Room = string.Empty;
            [JsonProperty("nickname")]
            public string Nickname = string.Empty;
            [JsonProperty("password")]
            public string Password = string.Empty;
        }

        [DataContract]
        class NetworkObjectSocketJoinRequest
        {
            [JsonProperty("socket_key")]
            public string SocketKey = string.Empty;
        }

        [DataContract]
        class NetworkObjectNewBoardRequest
        {
            [JsonProperty("room")]
            public string Room = string.Empty;
            [JsonProperty("game_type")]
            public int Game;
            [JsonProperty("variant_type")]
            public int Variant;
            [JsonProperty("custom_json")]
            public string CustomJSON = string.Empty;
            [JsonProperty("lockout_mode")]
            public bool Lockout;
            [JsonProperty("seed")]
            public string Seed = string.Empty;
            [JsonProperty("hide_card")]
            public bool HideBoard;
        }

        [DataContract]
        class NetworkObjectChatMessageRequest
        {
            [JsonProperty("room")]
            public string Room = string.Empty;
            [JsonProperty("text")]
            public string Text = string.Empty;
        }

        #endregion

        #region Broadcast objects

        [DataContract]
        class NetworkObjectBroadcast
        {
            [JsonProperty("type")]
            public string Type = string.Empty;
        }

        [DataContract]
        class NetworkObjectChatBroadcast
        {
            [JsonProperty("type")]
            public string Type = string.Empty;
            [JsonProperty("player")]
            public NetworkObjectPlayer Player = new();
            [JsonProperty("player_color")]
            public string Color = string.Empty;
            [JsonProperty("text")]
            public string Text = string.Empty;
            [JsonProperty("timestamp")]
            public string Timestamp = string.Empty;
        }

        [DataContract]
        class NetworkObjectNewBoardBroadcast
        {
            [JsonProperty("type")]
            public string Type = string.Empty;
            [JsonProperty("player")]
            public NetworkObjectPlayer Player = new();
            [JsonProperty("player_color")]
            public string PlayerColor = string.Empty;
            [JsonProperty("game")]
            public string Game = string.Empty;
            [JsonProperty("seed")]
            public string Seed = string.Empty;
            [JsonProperty("hide_card")]
            public bool HideBoard = false;
            [JsonProperty("is_current")]
            public bool IsCurrent = false;
            [JsonProperty("timestamp")]
            public string Timestamp = string.Empty;
        }

        [DataContract]
        class NetworkObjectGoalBroadcast
        {
            [JsonProperty("type")]
            public string Type = string.Empty;
            [JsonProperty("player")]
            public NetworkObjectPlayer Player = new();
            [JsonProperty("square")]
            public NetworkObjectBoardSquare Square = new();
            [JsonProperty("player_color")]
            public string PlayerColor = string.Empty;
            [JsonProperty("color")]
            public string Color = string.Empty;
            [JsonProperty("remove")]
            public bool Remove = false;
            [JsonProperty("timestamp")]
            public string Timestamp = string.Empty;
        }

        [DataContract]
        class NetworkObjectColorBroadcast
        {
            [JsonProperty("type")]
            public string Type = string.Empty;
            [JsonProperty("player")]
            public NetworkObjectPlayer Player = new();
            [JsonProperty("player_color")]
            public string PlayerColor = string.Empty;
            [JsonProperty("color")]
            public string Color = string.Empty;
            [JsonProperty("timestamp")]
            public string Timestamp = string.Empty;
        }

        [DataContract]
        class NetworkObjectRevealedBroadcast
        {
            [JsonProperty("type")]
            public string Type = string.Empty;
            [JsonProperty("player")]
            public NetworkObjectPlayer Player = new();
            [JsonProperty("player_color")]
            public string PlayerColor = string.Empty;
            [JsonProperty("timestamp")]
            public string Timestamp = string.Empty;
        }

        [DataContract]
        class NetworkObjectConnectionBroadcast
        {
            [JsonProperty("type")]
            public string Type = string.Empty;
            [JsonProperty("event_type")]
            public string EventType = string.Empty;
            [JsonProperty("player")]
            public NetworkObjectPlayer Player = new();
            [JsonProperty("player_color")]
            public string PlayerColor = string.Empty;
            [JsonProperty("timestamp")]
            public string Timestamp = string.Empty;
        }

        #endregion

        #region Common network objects

        [DataContract]
        class NetworkObjectSocketCheck
        {
            [JsonProperty("room")]
            public string RoomCode = string.Empty;
            [JsonProperty("player")]
            public string PlayerUUID = string.Empty;
        }

        [DataContract]
        class NetworkObjectBoardSquare
        {
            [JsonProperty("name")]
            public string Name = string.Empty;
            [JsonProperty("colors")]
            public string Colors = string.Empty;
            [JsonProperty("slot")]
            public string Slot = string.Empty;
        }

        [DataContract]
        class NetworkObjectRoomSettingsResponse
        {
            [JsonProperty("settings")]
            public NetworkObjectRoomSettings Settings = new();
        }

        [DataContract]
        class NetworkObjectRoomSettings
        {
            [JsonProperty("hide_card")]
            public bool HideBoard = true;
            [JsonProperty("lockout_mode")]
            public string LockoutMode = string.Empty;
            [JsonProperty("game")]
            public string GameName = string.Empty;
            [JsonProperty("game_id")]
            public int GameId = 0;
            [JsonProperty("variant")]
            public string VariantName = string.Empty;
            [JsonProperty("variant_id")]
            public int VariantId = 0;
            [JsonProperty("seed")]
            public int Seed = 0;
        }

        [DataContract]
        class NetworkObjectPlayer
        {
            [JsonProperty("uuid")]
            public string UUID = string.Empty;
            [JsonProperty("name")]
            public string Name = string.Empty;
            [JsonProperty("color")]
            public string Color = string.Empty;
            [JsonProperty("is_spectator")]
            public bool IsSpectator = false;
        }

        #endregion

    }
}
