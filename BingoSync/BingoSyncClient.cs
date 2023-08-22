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

        private static Action<string> Log;

        public static string room = "";
        public static string password = "";
        public static string nickname = "";
        public static string color = "";

        public static List<BoardSquare> board = null;
        public static bool isHidden = true;

        private static CookieContainer cookieContainer = null;
        private static HttpClientHandler handler = null;
        private static HttpClient client = null;
        private static ClientWebSocket webSocketClient = null;

        private static State forcedState = State.None;
        private static WebSocketState lastSocketState = WebSocketState.None;

        private static bool shouldConnect = false;

        public static List<Action> BoardUpdated;

        private static int maxRetries = 10;

        public static void Setup(Action<string> log)
        {
            Log = log;

            cookieContainer = new CookieContainer();
            handler = new HttpClientHandler();
            handler.CookieContainer = cookieContainer;
            client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://bingosync.com"),
            };
            LoadCookie();

            webSocketClient = new ClientWebSocket();

            BoardUpdated = new List<Action>();
        }

        public static void Update()
        {
            if (webSocketClient.State == lastSocketState)
                return;
            BoardUpdated.ForEach(f => f());
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
            }, 4, nameof(ExitRoom), () =>
            {
                forcedState = State.None;
            });
        }

        private static void UpdateBoardAndBroadcast(List<BoardSquare> newBoard)
        {
            board = newBoard;
            BingoTracker.ClearFinishedGoals();
            BoardUpdated.ForEach(f => f());
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
                Room = room,
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
                        var socketJoin = JsonConvert.DeserializeObject<SocketJoin>(joinRoomResponse.Result);
                        ConnectToBroadcastSocket(socketJoin);
                        UpdateBoard(true); // TODO: check if card should be hidden
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

        public static void SelectSquare(int square, Action errorCallback, bool clear = false)
        {
            var selectInput = new SelectInput
            {
                Room = room,
                Slot = square,
                Color = color,
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

        public static void RevealCard()
        {
            if (!isHidden) return;
            var revealInput = new RevealInput
            {
                Room = room,
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
                    isHidden = false;
                    BoardUpdated.ForEach(f => f());
                });
            }, maxRetries, nameof(RevealCard));
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
                        if (board == null) return;
                        if (obj.Type == "goal")
                        {
                            for (int i = 0; i < board.Count; i++)
                            {
                                if (board[i].Slot == obj.Square.Slot)
                                {
                                    board[i] = obj.Square;
                                    BingoTracker.GoalUpdated(board[i].Name, i);
                                    break;
                                }
                            }
                            BoardUpdated.ForEach(f => f());
                        }
                        else if (obj.Type == "new-card")
                        {
                            UpdateBoardAndBroadcast(null);
                            UpdateBoard(obj.HideCard);
                        }
                        else if (obj.Type == "revealed")
                        {
                            isHidden = false;
                            BoardUpdated.ForEach(f => f());
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
                var task = client.GetAsync($"room/{room}/board");
                return task.ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = null;
                    response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(boardResponse =>
                    {
                        var newBoard = JsonConvert.DeserializeObject<List<BoardSquare>>(boardResponse.Result);
                        isHidden = hideCard;
                        UpdateBoardAndBroadcast(newBoard);
                        Log("updated board");
                    });
                });
            }, maxRetries, nameof(UpdateBoard));
        }
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
    internal class SocketJoin
    {
        [JsonProperty("socket_key")]
        public string SocketKey = string.Empty;
    }
}
