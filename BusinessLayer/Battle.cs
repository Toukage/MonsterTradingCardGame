using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Battle(string BattleLog, string Result)
    { 
        public int BattleId { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public string BattleLog { get; } = BattleLog;
        public string Result { get; } = Result;
    }
}
