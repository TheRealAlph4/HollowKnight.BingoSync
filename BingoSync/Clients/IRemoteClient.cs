using System;
using System.Collections.Generic;

namespace BingoSync.Clients
{
    public interface IRemoteClient
    {
        public void DumpDebugInfo();
        public void Update();
        public ClientState GetState();
        public void JoinRoom(Action<Exception> callback);
        public void NewCard(string customJSON, bool lockout = true, bool hideCard = true);
        public void RevealCard();
        public void SendChatMessage(string text);
        public void SelectSquare(int square, Action errorCallback, bool clear = false);
        public void ExitRoom(Action callback);
        public List<string> RoomHistory();
    }
}
