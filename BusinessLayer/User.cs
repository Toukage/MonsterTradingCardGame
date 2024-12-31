using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class User
    {
        private readonly Parser _parser = new();
        private readonly Tokens _token = new();
        private readonly Response _response = new();
        private readonly UserManager _userMan = new();

        public int UserId { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }//passwords are not yet being hashed    
        public int Coins { get; set; } 
        public int Elo { get; set; } 
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }

        //----------------------LOGIN----------------------
        public void Login(string body, StreamWriter writer)
        {
            Console.WriteLine("** inside login function **");//debug

            var (username, password) = _parser.UserDataParse(body, writer);

            bool Valid = _userMan.GetUser(username, password);
            if (Valid)
            {
                int? userId = _userMan.GetUserId(username);
                Console.WriteLine($"** user id gotten : {userId} **");//debug
                string token = _token.GetToken(userId.Value, username, writer);
                Console.WriteLine($"** token gotten : {token} **");//debug
            }
            else
            {
                _response.HttpResponse(401, "Login Failed", writer);
            }
        }

        //----------------------REGISTRATION----------------------
        public void Register(string body, StreamWriter writer)
        {
            Console.WriteLine("** inside register **");//debug

            var (username, password) = _parser.UserDataParse(body, writer);

            Console.WriteLine($"** the credentioals inside the registr function name : {username}, pass : {password}  **");//debug

            bool Valid = _userMan.InsertUser(username, password, writer);
            if (Valid)
            {
                Console.WriteLine("** isnide valid if statement for register **");//debug
                _response.HttpResponse(201, "Successfully Registered", writer);
            }
        }
    }
}
