﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        private Action<string> Log;

        private CookieContainer cookieContainer = null;
        private HttpClientHandler handler = null;
        private HttpClient client = null;
        private ClientWebSocket webSocketClient = null;

        private ClientState forcedState = ClientState.None;
        private WebSocketState lastSocketState = WebSocketState.None;

        private bool shouldConnect = false;



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

                            Cookie cookie = new Cookie(cookieName.Trim(), cookieValue.Trim(), "/", response.RequestMessage.RequestUri.Host);
                            cookieContainer.Add(response.RequestMessage.RequestUri, cookie);
                        }
                    }
                });
            }, maxRetries, nameof(LoadCookie));
        }

        private void UpdateBoardAndBroadcast(List<NetworkObjectBoardSquare> newBoard)
        {
            Controller.Board = newBoard;
            BingoTracker.ClearFinishedGoals();
            Controller.BoardUpdate();
        }

        public void JoinRoom(Action<Exception> callback)
        {
            if (GetState() == ClientState.Loading)
            {
                return;
            }
            string roomCode = Controller.RoomCode;
            string roomNickname = Controller.RoomNickname;
            string roomPassword = Controller.RoomPassword;

            if (roomCode == null || roomCode == string.Empty
                || roomNickname == null || roomNickname == string.Empty
                || roomPassword == null || roomPassword == string.Empty)
            {
                return;
            }

            forcedState = ClientState.Loading;
            shouldConnect = true;

            var joinRoomInput = new NetworkObjectJoinRoomInput
            {
                Room = roomCode,
                Nickname = roomNickname,
                Password = roomPassword,
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
                        var socketJoin = JsonConvert.DeserializeObject<NetworkObjectSocketJoin>(joinRoomResponse.Result);
                        ConnectToBroadcastSocket(socketJoin);
                        UpdateBoard(true); // TODO: check if card should be hidden
                        UpdateSettings();
                        SetColor(Controller.RoomColor);
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
            var setColorInput = new NetworkObjectSetColorInput
            {
                Room = Controller.RoomCode,
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
            var newCard = new NetworkObjectNewCard
            {
                Room = Controller.RoomCode,
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
            if (Controller.BoardIsRevealed) return;
            var revealInput = new NetworkObjectRevealInput
            {
                Room = Controller.RoomCode,
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
                    Controller.BoardIsRevealed = true;
                    Controller.BoardUpdate();
                });
            }, maxRetries, nameof(RevealCard));
        }

        public void SendChatMessage(string text)
        {
            if (GetState() != ClientState.Connected) return;
            var setColorInput = new NetworkObjectChatMessage
            {
                Room = Controller.RoomCode,
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

        public void SelectSquare(int square, Action errorCallback, bool clear = false)
        {
            if (GetState() != ClientState.Connected) return;
            var selectInput = new NetworkObjectSelectInput
            {
                Room = Controller.RoomCode,
                Slot = square,
                Color = Controller.RoomColor,
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

        private void ConnectToBroadcastSocket(NetworkObjectSocketJoin socketJoin)
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

        private async void ListenForBoardUpdates(NetworkObjectSocketJoin socketJoin)
        {
            var buffer = new byte[1024];
            while (webSocketClient.State == WebSocketState.Open)
            {
                try
                {
                    var response = await webSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (response.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, response.Count);
                        var obj = JsonConvert.DeserializeObject<NetworkObjectBroadcast>(json);
                        if (!Controller.BoardIsAvailable()) return;
                        if (obj.Type == "goal")
                        {
                            for (int i = 0; i < Controller.Board.Count; i++)
                            {
                                if (Controller.Board[i].Slot == obj.Square.Slot)
                                {
                                    Controller.Board[i] = obj.Square;
                                    BingoTracker.GoalUpdated(Controller.Board[i].Name, i);
                                    break;
                                }
                            }
                            Controller.BoardUpdate();
                        }
                        else if (obj.Type == "new-card")
                        {
                            UpdateBoardAndBroadcast(null);
                            UpdateBoard(obj.HideCard);
                            UpdateSettings();
                        }
                        else if (obj.Type == "revealed")
                        {
                            if (Controller.GlobalSettings.RevealCardWhenOthersReveal)
                            {
                                Controller.RevealCard();
                            }
                            Controller.BoardUpdate();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"error receiving data from socket: {ex.Message}");
                }
            }
            if (shouldConnect)
            {
                Log($"socket is closed, will try to connect again");
                ConnectToBroadcastSocket(socketJoin);
                return;
            }
        }

        private void UpdateBoard(bool hideCard)
        {
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var task = client.GetAsync($"room/{Controller.RoomCode}/board");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = null;
                    response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(boardResponse =>
                    {
                        var newBoard = JsonConvert.DeserializeObject<List<NetworkObjectBoardSquare>>(boardResponse.Result);
                        Controller.BoardIsRevealed = !hideCard;
                        UpdateBoardAndBroadcast(newBoard);
                    });
                });
            }, maxRetries, nameof(UpdateBoard));
        }

        private void UpdateSettings()
        {
            RetryHelper.RetryWithExponentialBackoff(() =>
            {
                var task = client.GetAsync($"room/{Controller.RoomCode}/room-settings");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = null;
                    response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(settingsResponse =>
                    {
                        var settings = JsonConvert.DeserializeObject<NetworkObjectRoomSettingsResponse>(settingsResponse.Result);
                        Controller.RoomIsLockout = settings.Settings.LockoutMode == LOCKOUT_MODE;
                    });
                });
            }, maxRetries, nameof(UpdateSettings));
        }

        public List<string> RoomHistory()
        {
            return [];
        }
    }

    [DataContract]
    class NetworkObjectSetColorInput
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("color")]
        public string Color;
    }

    [DataContract]
    class NetworkObjectRevealInput
    {
        [JsonProperty("room")]
        public string Room;
    }

    [DataContract]
    class NetworkObjectSelectInput
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
    class NetworkObjectJoinRoomInput
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("nickname")]
        public string Nickname;
        [JsonProperty("password")]
        public string Password;
    }

    [DataContract]
    class NetworkObjectBroadcast
    {
        [JsonProperty("type")]
        public string Type = string.Empty;
        [JsonProperty("square")]
        public NetworkObjectBoardSquare Square = new NetworkObjectBoardSquare();
        [JsonProperty("hide_card")]
        public bool HideCard = false;
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
        public NetworkObjectRoomSettings Settings = new NetworkObjectRoomSettings();
    }

    [DataContract]
    class NetworkObjectRoomSettings
    {
        [JsonProperty("lockout_mode")]
        public string LockoutMode = string.Empty;
    }

    [DataContract]
    class NetworkObjectSocketJoin
    {
        [JsonProperty("socket_key")]
        public string SocketKey = string.Empty;
    }

    [DataContract]
    class NetworkObjectNewCard
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
    class NetworkObjectChatMessage
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("text")]
        public string Text;
    }
}