using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingoSync.Clients.EventInfoObjects
{
    public class GoalUpdate
    {
        public string Player {  get; set; }
        public Colors Color { get; set; }
        public string Goal { get; set; }
        public int Slot {  get; set; } 
        public bool Unmarking { get; set; }
    }
}
