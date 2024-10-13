using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class User//not in use yet
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }//passwords are not yet being hashed    
        public int Coins { get; set; } = 20;//default 20 coins, but in db also specified to add to every generated user, so might change
        public int Elo { get; set; } = 100;//default 100 elo, but in db also specified to add to every generated user, so might change
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }
}
