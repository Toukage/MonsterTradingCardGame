using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Deck
    {
        public int UserId { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();//to save cards, might change it to dictionary? probably not will see
    }
}
