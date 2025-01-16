using System;

namespace BingoSync.Clients.EventInfoObjects
{
    public class ChatMessage : EventArgs
    {
        public string Text { get; set; }
        public string Sender { get; set; }
        public string Timestamp { get; set; }
    }
}
