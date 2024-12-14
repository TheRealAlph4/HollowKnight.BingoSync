using System;

namespace BingoSync.Clients
{
    public class ChatMessage : EventArgs
    {
        public string Text { get; set; }
        public string Sender { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
    }
}
