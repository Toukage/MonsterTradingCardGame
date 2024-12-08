using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.DataLayer
{
    internal class DeckManager
    {
        private readonly Response _response;

        public DeckManager()
        {
            _response = new Response();
        }
        public void GetDeck(StreamWriter writer)//get user deck
        {
            writer.WriteLine("GetDeck not yet Implemented");
        }
    }

    
}
