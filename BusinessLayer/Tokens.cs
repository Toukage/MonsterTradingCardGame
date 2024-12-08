using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Tokens
    {
        private readonly TokenManager _tokenMan = new ();
        private readonly Response _response = new ();
        private readonly Parser _parser = new();
        private readonly UserManager _userMan = new ();

        
        public int UserId { get; }
        public string Token { get; } 

        //-----------Token--Validation-----------
        public string GetToken(int userId, string username, StreamWriter writer)//gets token for login (generates on login)
        {
            Console.WriteLine("** inside GetToken **");//debug
            string token = _tokenMan.GetTokenFromId(userId);
            
            if (token != null)//if the user has a token
            {
                Console.WriteLine($"** token from user id gotten :{token} **");//debug
                _response.HttpResponse(200, $"here : {username}: {token}", writer);
                return token;
            }
            else//token gen for when the user doesnt have one
            {
                Console.WriteLine("** no token found , starting generation **");//debug
                token = GenerateToken(username);
                Console.WriteLine($"** token generated: {token} **");//debug
                _tokenMan.InsertToken(userId, token);
                _response.HttpResponse(200, $"here : {username}: {token}", writer);
                return token;
            }
        }

        public (bool isValid, bool isAdmin, int? userID) CheckToken(string headers, StreamWriter writer)//checks token and if admin
        {
            Console.WriteLine("** inside CheckToken **");//debug
            string token = _parser.TokenParse(headers, writer);
            Console.WriteLine($"** inside CheckToken token is -> {token} **");//debug
            if (token != null)//if the user has a token
            {
                Console.WriteLine($"** inside token !null **");//debug
                bool isValid = _tokenMan.ValidateToken(token);
                
                if (isValid)
                {
                    Console.WriteLine($"** inside token isValid **");//debug
                    int? userID = _tokenMan.GetUserIdFromToken(token);
                    Console.WriteLine($"** userid from token gotten  : {userID}  **");//debug
                    bool isAdmin = _userMan.CheckAdmin(userID!.Value);
                    Console.WriteLine($"** checked if admin : {isAdmin}  **");//debug
                    return (isValid, isAdmin, userID);
                }
                else
                {
                    Console.WriteLine("** token validation failed **");
                    _response.HttpResponse(401, "Token invalid", writer);
                }
                return(false, false, null);
            }
            else
            {
                _response.HttpResponse(401, "Token is missing", writer);
                return (false, false, null);
            }
        }

        public string GenerateToken(string username)//generates token
        {
            return $"{username}-mtcgToken";//token in Project Spezifications format
        }
    }
}
