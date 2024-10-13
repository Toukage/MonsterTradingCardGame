using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Card
    {
        public string CardId { get; set; }
        public string Name { get; set; }
        public string CardType { get; set; }//monster or spell
        public int Damage { get; set; }
        public string ElementType { get; set; }//fire, water, earth, air etc
    }
}
