﻿using BingoSync.Clients.EventInfoObjects;
using BingoSync.Sessions;
using HutongGames.PlayMaker.Actions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace BingoSync.Clients
{
    internal class BingoSyncClient : IRemoteClient
    {
        private static readonly string LOCKOUT_MODE = "Lockout";
        private static readonly int maxRetries = 30;

        private readonly Action<string> Log;

        private string currentRoomID = string.Empty;

        private readonly CookieContainer cookieContainer = null;
        private readonly HttpClientHandler handler = null;
        private readonly HttpClient client = null;
        private ClientWebSocket webSocketClient = null;

        private ClientState forcedState = ClientState.None;
        private WebSocketState lastSocketState = WebSocketState.None;

        private bool shouldConnect = false;

        public event EventHandler<CardRevealedBroadcast> CardRevealedBroadcastReceived;
        public event EventHandler<ChatMessage> ChatMessageReceived;
        public event EventHandler<GoalUpdate> GoalUpdateReceived;
        public event EventHandler<NewCardBroadcast> NewCardReceived;
        public event EventHandler<PlayerColorChange> PlayerColorChangeReceived;
        public event EventHandler<PlayerConnectedBroadcast> PlayerConnectedBroadcastReceived;
        public event EventHandler<RoomSettings> RoomSettingsReceived;

        private BingoBoard Board;

        public void DumpDebugInfo()
        {
            Log($"Client");
            Log($"\tActualClientState = {webSocketClient?.State}");
            Log($"\tForcedClientState = {forcedState}");
            Log($"\tClientShouldConnect = {shouldConnect}");
        }

        public BingoSyncClient(Action<string> log)
        {
            Log = log;

            cookieContainer = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            };
            client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://bingosync.com"),
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"HollowKnight.BingoSync/{BingoSync.version}");
            LoadCookie();

            webSocketClient = new ClientWebSocket();
        }

        public void SetBoard(BingoBoard board)
        {
            Board = board;
        }

        public void Update()
        {
            if (webSocketClient.State == lastSocketState)
                return;
            Controller.BoardUpdate();
            forcedState = ClientState.None;
            lastSocketState = webSocketClient.State;
        }

        public ClientState GetState()
        {
            if (forcedState != ClientState.None)
                return forcedState;
            if (webSocketClient.State == WebSocketState.Open)
                return ClientState.Connected;
            else if (webSocketClient.State == WebSocketState.Connecting)
                return ClientState.Loading;
            return ClientState.Disconnected;
        }

        private void LoadCookie()
        {
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var task = client.GetAsync("");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = null;
                    response = responseTask.Result;
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
            }, maxRetries, nameof(LoadCookie));
        }

        private void UpdateBoardAndBroadcast(List<NetworkObjectBoardSquare> newBoard)
        {
            if (newBoard != null)
            {
                List<Square> squares = [];
                foreach (NetworkObjectBoardSquare networkSquare in newBoard)
                {
                    HashSet<Colors> colors = [];
                    foreach (string color in networkSquare.Colors.Split(' '))
                    {
                        colors.Add(ColorExtensions.FromName(color));
                    }
                    squares.Add(new Square() {
                        Name = networkSquare.Name,
                        MarkedBy = colors,
                        Highlighted = false,
                        GoalNr = int.Parse(networkSquare.Slot.Substring(4)) - 1,
                    });
                }
                Board.SetSquares(squares);
            }
            BingoTracker.ClearFinishedGoals();
            Controller.BoardUpdate();
        }

        public void JoinRoom(string roomID, string nickname, string password, Colors color, Action<Exception> callback)
        {
            if (GetState() == ClientState.Loading)
            {
                return;
            }
            forcedState = ClientState.Loading;
            shouldConnect = true;
            currentRoomID = roomID;

            var joinRoomInput = new NetworkObjectJoinRoomRequest
            {
                Room = roomID,
                Nickname = nickname,
                Password = password,
            };
            var payload = JsonConvert.SerializeObject(joinRoomInput);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var task = client.PostAsync("api/join-room", content);
            _ = task.ContinueWith(responseTask =>
            {
                Exception ex = null;
                try
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(joinRoomResponse =>
                    {
                        var socketJoin = JsonConvert.DeserializeObject<NetworkObjectSocketJoinRequest>(joinRoomResponse.Result);
                        ConnectToBroadcastSocket(socketJoin);
                        UpdateBoard(true); // TODO: check if card should be hidden
                        UpdateSettings();
                        SetColor(color.GetName());
                    });
                }
                catch (Exception _ex)
                {
                    ex = _ex;
                    Log($"could not join room: {ex.Message}");
                }
                finally
                {
                    callback(ex);
                    forcedState = ClientState.None;
                }
            });
        }

        private void SetColor(string color)
        {
            var setColorInput = new NetworkObjectSetColorRequest
            {
                Room = currentRoomID,
                Color = color,
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(setColorInput);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = client.PutAsync("api/color", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                });
            }, maxRetries, nameof(SetColor));
        }

        public void NewCard(string customJSON, bool lockout = true, bool hideCard = true)
        {
            if (GetState() != ClientState.Connected) return;
            var newCard = new NetworkObjectNewCardRequest
            {
                Room = currentRoomID,
                Game = 18, // this is supposed to be custom alread
                Variant = 18, // but this is also required for custom ???
                CustomJSON = customJSON,
                Lockout = !lockout, // false is lockout here for some godforsaken reason
                Seed = "",
                HideCard = hideCard,
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(newCard);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = client.PostAsync("api/new-card", content);
                return task.ContinueWith(responseTask => { });
            }, maxRetries, nameof(SendChatMessage));
        }

        public void RevealCard()
        {
            if (GetState() != ClientState.Connected) return;
            if (Board.IsRevealed) return;
            var revealInput = new NetworkObjectRevealRequest
            {
                Room = currentRoomID,
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(revealInput);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = client.PutAsync("api/revealed", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    Board.IsRevealed = true;
                    Controller.BoardUpdate();
                });
            }, maxRetries, nameof(RevealCard));
        }

        public void SendChatMessage(string text)
        {
            if (GetState() != ClientState.Connected) return;
            var setColorInput = new NetworkObjectChatMessageRequest
            {
                Room = currentRoomID,
                Text = text,
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(setColorInput);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = client.PutAsync("api/chat", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                });
            }, maxRetries, nameof(SendChatMessage));
        }

        public void SelectSquare(int square, Colors color, Action errorCallback, bool clear = false)
        {
            if (GetState() != ClientState.Connected) return;
            var selectInput = new NetworkObjectSelectRequest
            {
                Room = currentRoomID,
                Slot = square,
                Color = color.GetName(),
                RemoveColor = clear,
            };
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var payload = JsonConvert.SerializeObject(selectInput);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = client.PutAsync("api/select", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                });
            }, maxRetries, nameof(SelectSquare), errorCallback);
        }

        public void ExitRoom(Action callback)
        {
            if (GetState() != ClientState.Connected) return;
            shouldConnect = false;
            forcedState = ClientState.Loading;
            currentRoomID = string.Empty;
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                return webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "exiting room", CancellationToken.None).ContinueWith(result =>
                {
                    if (result.Exception != null)
                    {
                        throw result.Exception;
                    }
                    UpdateBoardAndBroadcast(null);
                    webSocketClient = new ClientWebSocket();
                    forcedState = ClientState.None;
                    callback();
                });
            }, maxRetries, nameof(ExitRoom), () =>
            {
                UpdateBoardAndBroadcast(null);
                webSocketClient = new ClientWebSocket();
                forcedState = ClientState.None;
            });
        }

        private void ConnectToBroadcastSocket(NetworkObjectSocketJoinRequest socketJoin)
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
                        Log($"error connecting to websocket: {connectResponse.Exception}");
                        throw connectResponse.Exception;
                    }

                    // Log($"connected to the socket, sending socketJoin object");
                    var serializedSocketJoin = JsonConvert.SerializeObject(socketJoin);
                    var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(serializedSocketJoin));
                    var sendTask = webSocketClient.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    sendTask.ContinueWith(_ =>
                    {
                        ListenForBoardUpdates(socketJoin);
                    });
                });
            }, maxRetries, nameof(ConnectToBroadcastSocket));
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
                    if (!Board.IsAvailable()) return;
                    string json = Encoding.UTF8.GetString(buffer, 0, response.Count);
                    NetworkObjectBroadcast broadcast = JsonConvert.DeserializeObject<NetworkObjectBroadcast>(json);
                    switch(broadcast.Type)
                    {
                        case "chat": HandleChatBroadcast(json); break;
                        case "new-card": HandleNewCardBroadcast(json); break;
                        case "goal": HandleGoalBroadcast(json); break;
                        case "color": HandleColorBroadcast(json); break;
                        case "revealed": HandleRevealedBroadcast(json); break;
                        case "connection": HandleConnectionBroadcast(json); break;
                        default: Log($"Received unknown broadcast type \"{broadcast.Type}\""); break;
                    }
                }
                catch (Exception ex)
                {
                    Log($"'{ex.GetType().FullName}' error with message '{ex.Message}' while handling socket broadcast.\nStacktrace: \n{ex.StackTrace}");
                }
            }
            if (shouldConnect)
            {
                Log($"socket is closed, will try to connect again");
                ConnectToBroadcastSocket(socketJoin);
                return;
            }
        }

        private void HandleChatBroadcast(string json)
        {
            NetworkObjectChatBroadcast chatBroadcast = JsonConvert.DeserializeObject<NetworkObjectChatBroadcast>(json);
            ChatMessageReceived?.Invoke(this, new ChatMessage()
            {
                Text = chatBroadcast.Text,
                Sender = chatBroadcast.Player.Name,
                Timestamp = chatBroadcast.Timestamp,
            });
        }

        private void HandleNewCardBroadcast(string json)
        {
            NetworkObjectNewCardBroadcast newCardBroadcast = JsonConvert.DeserializeObject<NetworkObjectNewCardBroadcast>(json);
            UpdateBoardAndBroadcast(null);
            UpdateBoard(newCardBroadcast.HideCard);
            UpdateSettings();
            NewCardReceived?.Invoke(this, new NewCardBroadcast()
            {
                Player = newCardBroadcast.Player.Name,
            });
        }

        private void HandleGoalBroadcast(string json)
        {
            NetworkObjectGoalBroadcast goalBroadcast = JsonConvert.DeserializeObject<NetworkObjectGoalBroadcast>(json);
            foreach (Square square in Board)
            {
                if ("slot" + (square.GoalNr + 1) == goalBroadcast.Square.Slot)
                {
                    square.MarkedBy.Clear();
                    foreach (string color in goalBroadcast.Square.Colors.Split(' '))
                    {
//                        if (color == "blank") continue;
                        square.MarkedBy.Add(ColorExtensions.FromName(color));
                    }
                    GoalUpdateReceived?.Invoke(this, new GoalUpdate()
                    {
                        Player = goalBroadcast.Player.Name,
                        Color = ColorExtensions.FromName(goalBroadcast.Color),
                        Goal = goalBroadcast.Square.Name,
                        Slot = square.GoalNr,
                        Unmarking = goalBroadcast.Remove,
                    });
                    break;
                }
            }
            Controller.BoardUpdate();
        }

        private void HandleColorBroadcast(string json)
        {
            NetworkObjectColorBroadcast colorBroadcast = JsonConvert.DeserializeObject<NetworkObjectColorBroadcast>(json);
            PlayerColorChangeReceived?.Invoke(this, new PlayerColorChange()
            {
                Player = colorBroadcast.Player.Name,
                Color = ColorExtensions.FromName(colorBroadcast.Color),
            });
        }

        private void HandleRevealedBroadcast(string json)
        {
            NetworkObjectRevealedBroadcast revealedBroadcast = JsonConvert.DeserializeObject<NetworkObjectRevealedBroadcast>(json);
            if (Controller.GlobalSettings.RevealCardWhenOthersReveal)
            {
                Controller.RevealCard();
            }
            Controller.BoardUpdate();
            CardRevealedBroadcastReceived?.Invoke(this, new CardRevealedBroadcast()
            {
                Player = revealedBroadcast.Player.Name,
            });
        }

        private void HandleConnectionBroadcast(string json)
        {
            NetworkObjectConnectionBroadcast connectionBroadcast = JsonConvert.DeserializeObject<NetworkObjectConnectionBroadcast>(json);
            PlayerConnectedBroadcastReceived?.Invoke(this, new PlayerConnectedBroadcast()
            {
                Player = connectionBroadcast.Player.Name,
            });
        }

        private void UpdateBoard(bool hideCard)
        {
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var task = client.GetAsync($"room/{currentRoomID}/board");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = null;
                    response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(boardResponse =>
                    {
                        var newBoard = JsonConvert.DeserializeObject<List<NetworkObjectBoardSquare>>(boardResponse.Result);
                        Board.IsRevealed = !hideCard;
                        UpdateBoardAndBroadcast(newBoard);
                    });
                });
            }, maxRetries, nameof(UpdateBoard));
        }

        private void UpdateSettings()
        {
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var task = client.GetAsync($"room/{currentRoomID}/room-settings");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = null;
                    response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(settingsResponse =>
                    {
                        var settings = JsonConvert.DeserializeObject<NetworkObjectRoomSettingsResponse>(settingsResponse.Result);
                        RoomSettingsReceived(this, new RoomSettings()
                        {
                            IsLockout = settings.Settings.LockoutMode == LOCKOUT_MODE
                        });
                    });
                });
            }, maxRetries, nameof(UpdateSettings));
        }

        public List<string> RoomHistory()
        {
            return [];
        }
    }

    #region Request objects

    [DataContract]
    class NetworkObjectSetColorRequest
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("color")]
        public string Color;
    }

    [DataContract]
    class NetworkObjectRevealRequest
    {
        [JsonProperty("room")]
        public string Room;
    }

    [DataContract]
    class NetworkObjectSelectRequest
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("slot")]
        public int Slot;
        [JsonProperty("color")]
        public string Color;
        [JsonProperty("remove_color")]
        public bool RemoveColor;
    }

    [DataContract]
    class NetworkObjectJoinRoomRequest
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("nickname")]
        public string Nickname;
        [JsonProperty("password")]
        public string Password;
    }

    [DataContract]
    class NetworkObjectSocketJoinRequest
    {
        [JsonProperty("socket_key")]
        public string SocketKey = string.Empty;
    }

    [DataContract]
    class NetworkObjectNewCardRequest
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("game_type")]
        public int Game;
        [JsonProperty("variant_type")]
        public int Variant;
        [JsonProperty("custom_json")]
        public string CustomJSON;
        [JsonProperty("lockout_mode")]
        public bool Lockout;
        [JsonProperty("seed")]
        public string Seed;
        [JsonProperty("hide_card")]
        public bool HideCard;
    }

    [DataContract]
    class NetworkObjectChatMessageRequest
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("text")]
        public string Text;
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
    class NetworkObjectNewCardBroadcast
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
        public bool HideCard = false;
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
        [JsonProperty("lockout_mode")]
        public string LockoutMode = string.Empty;
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
