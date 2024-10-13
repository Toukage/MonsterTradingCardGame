using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Trade
    {
        public int TradeId { get; set; }
        public Card OfferedCard { get; set; }
        public string RequestedCardType { get; set; }
        public int RequestedMinimumDamage { get; set; }
        public string Status { get; set; }
    }
}
