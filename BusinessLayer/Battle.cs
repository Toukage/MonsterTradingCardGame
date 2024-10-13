using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Battle
    { 
        public int BattleId { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public string BattleLog { get; set; } 
        public string Result { get; set; }
    }
}
