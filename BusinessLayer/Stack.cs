using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Stack
    {
        public int UserId { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();//list of cards in the stack
    }
}
