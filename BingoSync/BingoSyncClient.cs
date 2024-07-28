using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace BingoSync
{
    internal class BingoSyncClient
    {
        public enum State
        {
            None, Disconnected, Connected, Loading
        };

        private static readonly string LOCKOUT_MODE = "Lockout";

        private static Action<string> Log;

        private static CookieContainer cookieContainer = null;
        private static HttpClientHandler handler = null;
        private static HttpClient client = null;
        private static ClientWebSocket webSocketClient = null;

        private static State forcedState = State.None;
        private static WebSocketState lastSocketState = WebSocketState.None;

        private static bool shouldConnect = false;

        private static readonly int maxRetries = 30;

        public static void Setup(Action<string> log)
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

        public static void Update()
        {
            if (webSocketClient.State == lastSocketState)
                return;
            Controller.BoardUpdate();
            forcedState = State.None;
            lastSocketState = webSocketClient.State;
        }

        public static State GetState()
        {
            if (forcedState != State.None)
                return forcedState;
            if (webSocketClient.State == WebSocketState.Open)
                return State.Connected;
            else if (webSocketClient.State == WebSocketState.Connecting)
                return State.Loading;
            return State.Disconnected;
        }

        private static void LoadCookie()
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

        private static void UpdateBoardAndBroadcast(List<BoardSquare> newBoard)
        {
            Controller.Board = newBoard;
            BingoTracker.ClearFinishedGoals();
            Controller.BoardUpdate();
        }

        public static void JoinRoom(Action<Exception> callback)
        {
            if (GetState() == State.Loading)
            {
                return;
            }
            forcedState = State.Loading;
            shouldConnect = true;

            var joinRoomInput = new JoinRoomInput
            {
                Room = Controller.RoomCode,
                Nickname = Controller.RoomNickname,
                Password = Controller.RoomPassword,
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
                        var socketJoin = JsonConvert.DeserializeObject<SocketJoin>(joinRoomResponse.Result);
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
                    forcedState = State.None;
                }
            });
        }

        public static void SetColor(string color)
        {
            var setColorInput = new SetColorInput
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

        public static void NewCard(string customJSON, bool lockout = true, bool hideCard = true)
        {
            var newCard = new NewCard
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
            }, maxRetries, nameof(ChatMessage));
        }

        public static void RevealCard()
        {
            if (Controller.BoardIsRevealed) return;
            var revealInput = new RevealInput
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

        public static void ChatMessage(string text)
        {
            var setColorInput = new ChatMessage
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
            }, maxRetries, nameof(ChatMessage));
        }

        public static void SelectSquare(int square, Action errorCallback, bool clear = false)
        {
            var selectInput = new SelectInput
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

        public static void ExitRoom(Action callback)
        {
            shouldConnect = false;
            forcedState = State.Loading;
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
                    forcedState = State.None;
                    callback();
                });
            }, maxRetries, nameof(ExitRoom), () =>
            {
                UpdateBoardAndBroadcast(null);
                webSocketClient = new ClientWebSocket();
                forcedState = State.None;
            });
        }

        private static void ConnectToBroadcastSocket(SocketJoin socketJoin)
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

                    Log($"connected to the socket, sending socketJoin object");
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

        private static async void ListenForBoardUpdates(SocketJoin socketJoin)
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
                        var obj = JsonConvert.DeserializeObject<Broadcast>(json);
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
                            Controller.BoardIsRevealed = true;
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

        public static void UpdateBoard(bool hideCard)
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
                        var newBoard = JsonConvert.DeserializeObject<List<BoardSquare>>(boardResponse.Result);
                        Controller.BoardIsRevealed = !hideCard;
                        UpdateBoardAndBroadcast(newBoard);
                        Log("updated board");
                    });
                });
            }, maxRetries, nameof(UpdateBoard));
        }

        public static void UpdateSettings()
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
                        var settings = JsonConvert.DeserializeObject<RoomSettingsResponse>(settingsResponse.Result);
                        Controller.RoomIsLockout = settings.Settings.LockoutMode == LOCKOUT_MODE;
                    });
                });
            }, maxRetries, nameof(UpdateSettings));
        }
    }

    [DataContract]
    internal class SetColorInput
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("color")]
        public string Color;
    }

    [DataContract]
    internal class RevealInput
    {
        [JsonProperty("room")]
        public string Room;
    }

    [DataContract]
    internal class SelectInput
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
    internal class JoinRoomInput
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("nickname")]
        public string Nickname;
        [JsonProperty("password")]
        public string Password;
    }

    [DataContract]
    internal class Broadcast
    {
        [JsonProperty("type")]
        public string Type = string.Empty;
        [JsonProperty("square")]
        public BoardSquare Square = new BoardSquare();
        [JsonProperty("hide_card")]
        public bool HideCard = false;
    }

    [DataContract]
    internal class BoardSquare
    {
        [JsonProperty("name")]
        public string Name = string.Empty;
        [JsonProperty("colors")]
        public string Colors = string.Empty;
        [JsonProperty("slot")]
        public string Slot = string.Empty;
    }

    [DataContract]
    internal class RoomSettingsResponse
    {
        [JsonProperty("settings")]
        public RoomSettings Settings = new RoomSettings();
    }

    [DataContract]
    internal class RoomSettings
    {
        [JsonProperty("lockout_mode")]
        public string LockoutMode = string.Empty;
    }

    [DataContract]
    internal class SocketJoin
    {
        [JsonProperty("socket_key")]
        public string SocketKey = string.Empty;
    }

    [DataContract]
    internal class NewCard
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
    public class ChatMessage
    {
        [JsonProperty("room")]
        public string Room;
        [JsonProperty("text")]
        public string Text;
    }
}
