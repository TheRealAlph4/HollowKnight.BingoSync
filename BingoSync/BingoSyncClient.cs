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
        public static Action<string> _log;

        public static string room = "";
        public static string password = "";
        public static string nickname = "BingoSyncMod";
        public static string color = "";

        public static bool joined = false, joining = false;
        public static bool cookiesSet = false;

        public static List<BoardSquare> board = null;

        private static CookieContainer cookieContainer = null;
        private static HttpClientHandler handler = null;
        private static HttpClient client = null;
        private static ClientWebSocket webSocketClient = null;

        public static List<Action<Exception>> BoardUpdated;

        public static void Setup()
        {
            cookieContainer = new CookieContainer();
            handler = new HttpClientHandler();
            handler.CookieContainer = cookieContainer;
            client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://bingosync.com"),
            };
            RetryLoadCookie();

            webSocketClient = new ClientWebSocket();

            BoardUpdated = new List<Action<Exception>>();
        }

        private static void RetryLoadCookie(int maxAttempts = 10)
        {
            if (maxAttempts <= 0)
            {
                return;
            }

            LoadCookie((ex) =>
            {
                if (ex != null)
                {
                    RetryLoadCookie(maxAttempts - 1);
                }
            });
        }

        private static void LoadCookie(Action<Exception> errorCallback)
        {
            var task = client.GetAsync("");
            _ = task.ContinueWith(responseTask =>
            {
                HttpResponseMessage response = null;
                try
                {
                    response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                } catch (Exception ex)
                {
                    errorCallback(ex);
                    return;
                }

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
        }
        public static void ExitRoom()
        {
            joined = false;
            joining = false;
            BingoTracker.ClearFinishedGoals();
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
                        _log($"joined room, response is {socketJoin.SocketKey}");
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
            var payload = JsonConvert.SerializeObject(selectInput);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var task = client.PutAsync("api/select", content);
            _ = task.ContinueWith(responseTask =>
            {
                var response = responseTask.Result;
                response.EnsureSuccessStatusCode();
            });
        }

        private static void ConnectToBroadcastSocket(SocketJoin socketJoin)
        {
            var socketUri = new Uri("wss://sockets.bingosync.com/broadcast");
            var connectTask = webSocketClient.ConnectAsync(socketUri, CancellationToken.None);
            _ = connectTask.ContinueWith(connectResponse =>
            {
                if (connectResponse.Exception != null)
                {
                    Console.WriteLine($"Error connecting to websocket: {connectResponse.Exception}");
                    return;
                }

                _log($"connected to the socket, sending socketJoin object");
                var serializedSocketJoin = JsonConvert.SerializeObject(socketJoin);
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(serializedSocketJoin));
                var sendTask = webSocketClient.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                sendTask.ContinueWith(_ =>
                {
                    _log($"will listen for updates");
                    ListenForBoardUpdates();
                });
            });
        }

        private static void ListenForBoardUpdates()
        {
            if (webSocketClient.State != WebSocketState.Open)
            {
                //ConnectToBroadcastSocket();
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
                            BoardUpdated.ForEach(f =>
                            {
                                f(null);
                            });
                        } else if (obj.Type == "new-card") {
                            BingoTracker.ClearFinishedGoals();
                            UpdateBoard();
                        } else {
                            return;
                        }
                    }
                } finally
                {
                    ListenForBoardUpdates();
                }
            });
        }

        public static void UpdateBoard()
        {
            var task = client.GetAsync($"room/{room}/board");
            _ = task.ContinueWith(responseTask =>
            {
                HttpResponseMessage response = null;
                try
                {
                    response = responseTask.Result;
                    response.EnsureSuccessStatusCode();
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.ContinueWith(boardResponse =>
                    {
                        board = JsonConvert.DeserializeObject<List<BoardSquare>>(boardResponse.Result);
                        _log("updated board");
                        BoardUpdated.ForEach(f =>
                        {
                            f(null);
                        });
                    });
                } catch (Exception ex)
                {
                    BoardUpdated.ForEach(f =>
                    {
                        f(ex);
                    });
                }
            });
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
