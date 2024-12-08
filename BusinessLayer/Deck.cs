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
        public List<Card> Cards { get; set; } = new List<Card>();
        public void CheckDeck(string body, string headers, StreamWriter writer)
        {
            //first check token
            //then check if user has a deck -> if yes print
            //if user doesnt have a deck, check if the body provides cards
            //if yes CreateDeck
            //if not 200 -> empty list
        }

    }
}
