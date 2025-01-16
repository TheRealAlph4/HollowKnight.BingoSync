using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingoSync.Clients.EventInfoObjects
{
    public class PlayerColorChange
    {
        public string Player { get; set; }
        public Colors Color { get; set; }
    }
}
