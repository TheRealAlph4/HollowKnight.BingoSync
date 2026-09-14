using System.Collections.Generic;

namespace BingoSync.Clients.EventInfoObjects
{
    public class PlayerInfo
    {
        public string UUID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Color { get; set; }
        public bool IsSpectator { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PlayerInfo broadcast &&
                   UUID == broadcast.UUID;
        }

        public override int GetHashCode()
        {
            return 2006673922 + EqualityComparer<string>.Default.GetHashCode(UUID);
        }
    }
}
