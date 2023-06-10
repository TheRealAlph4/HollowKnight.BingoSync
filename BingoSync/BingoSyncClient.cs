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

namespace BingoSync
{
    internal class BingoSyncClient
    {
        private static Action<string> Log;

        public static string room = "";
        public static string password = "";
        public static string nickname = "";
        public static string color = "";

        public static bool joined = false, joining = false;

        public static List<BoardSquare> board = null;

        private static CookieContainer cookieContainer = null;
        private static HttpClientHandler handler = null;
        private static HttpClient client = null;
        private static ClientWebSocket webSocketClient = null;

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

        private static void LoadCookie()
        {
            RetryHelper.RetryWithExponentialBackoff(() => {
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

        public static void ExitRoom()
        {
            joined = false;
            joining = false;
            UpdateBoardAndBroadcast(null);
        }

        private static void UpdateBoardAndBroadcast(List<BoardSquare> newBoard) {
            board = newBoard;
            BingoTracker.ClearFinishedGoals();
            BoardUpdated.ForEach(f => f());
        }

        public static void JoinRoom(Action<Exception> callback)
        {
            if (joining)
            {
                return;
            }
            joining = true;

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
                    });

                    joined = true;
                } catch (Exception _ex)
                {
                    ex = _ex;
                } finally
                {
                    joining = false;
                    callback(ex);
                }
            });
        }

        public static void SelectSquare(int square, Action<Exception> errorCallback, bool clear = false)
        {
            var selectInput = new SelectInput
            {
                Room = room,
                Slot = square,
                Color = color,
                RemoveColor = clear,
            };
            RetryHelper.RetryWithExponentialBackoff(() => {
                var payload = JsonConvert.SerializeObject(selectInput);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var task = client.PutAsync("api/select", content);
                return task.ContinueWith(responseTask =>
                {
                    var response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                });
            }, maxRetries, nameof(SelectSquare));
        }

        private static void ConnectToBroadcastSocket(SocketJoin socketJoin)
        {
            var socketUri = new Uri("wss://sockets.bingosync.com/broadcast");
            RetryHelper.RetryWithExponentialBackoff(() => {
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

        private static void ListenForBoardUpdates(SocketJoin socketJoin)
        {
            if (webSocketClient.State != WebSocketState.Open)
            {
                Log($"socket is closed, will try to connect again");
                ConnectToBroadcastSocket(socketJoin);
                return;
            }
            var buffer = new byte[1024];
            var result = webSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            _ = result.ContinueWith(responseTask =>
            {
                try
                {
                    var response = responseTask.Result;
                    if (response.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, response.Count);
                        var obj = JsonConvert.DeserializeObject<Broadcast>(json);
                        if (board == null) return;
                        if (obj.Type == "goal") {
                            for (int i = 0; i < board.Count; i++)
                            {
                                if (board[i].Slot == obj.Square.Slot)
                                {
                                    board[i] = obj.Square;
                                    break;
                                }
                            }
                            BoardUpdated.ForEach(f => f());
                        } else if (obj.Type == "new-card") {
                            UpdateBoardAndBroadcast(null);
                            UpdateBoard();
                        } else {
                            return;
                        }
                    }
                } finally
                {
                    ListenForBoardUpdates(socketJoin);
                }
            });
        }

        public static void UpdateBoard()
        {
            RetryHelper.RetryWithExponentialBackoff(() => {
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
                        UpdateBoardAndBroadcast(newBoard);
                        Log("updated board");
                    });
                });
            }, maxRetries, nameof(UpdateBoard));
        }
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
        [JsonProperty("square")]
        public BoardSquare Square = new BoardSquare();
        [JsonProperty("type")]
        public string Type = string.Empty;
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
