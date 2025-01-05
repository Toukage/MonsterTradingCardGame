using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static MonsterTradingCardGame.BusinessLayer.User;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Tokens
    {
        private readonly TokenRepo _tokenMan = new ();
        private readonly Response _response = new ();
        private readonly Parser _parser = new();
        private readonly UserRepo _userMan = new ();

        
        public int UserId { get; }
        public string Token { get; } 

        //-----------Token--Validation-----------
        public async Task <string> GetToken(int userId, string username, StreamWriter writer)//gets token for login (generates on login)
        {
            Console.WriteLine("** inside GetToken **");//debug
            string token = await _tokenMan.GetTokenFromId(userId);
            
            if (token != null)//if the user has a token
            {
                Console.WriteLine($"** token from user id gotten :{token} **");//debug
                await _response.HttpResponse(200, $"here : {username}: {token}", writer);
                return token;
            }
            else//token gen for when the user doesnt have one
            {
                Console.WriteLine("** no token found , starting generation **");//debug
                token = GenerateToken(username);
                Console.WriteLine($"** token generated: {token} **");//debug
                await _tokenMan.InsertToken(userId, token);
                await _response.HttpResponse(200, $"here : {username}: {token}", writer);
                return token;
            }
        }

        public async Task<(bool isValid, int userID)> CheckToken(string headers, User user, StreamWriter writer)//checks token
        {
            Console.WriteLine("** inside CheckToken **");//debug
            string token = _parser.TokenParse(headers, writer);
            Console.WriteLine($"** inside CheckToken token is -> {token} **");//debug
            if (token != null)//if the user has a token
            {
                Console.WriteLine($"** inside token !null **");//debug
                bool isValid = await _tokenMan.ValidateToken(token);
                
                if (isValid)
                {
                    Console.WriteLine($"** inside token isValid **");//debug
                    int userID = await _tokenMan.GetUserIdFromToken(token);
                    Console.WriteLine($"** userid from token gotten  : {userID}  **");//debug
                    if (userID == -1)
                    {
                        return (false, -1);
                    }
                    else 
                    {
                        user.UserId = userID; 
                        user.Token = token;//saving id and token for later use
                        Console.WriteLine($"Current User ID: {user.UserId}");
                        Console.WriteLine($"Current Token: {user.Token}");

                        return (isValid, userID);
                    }
                }
                else
                {
                    Console.WriteLine("** token validation failed **");
                    await _response.HttpResponse(401, "Token invalid or missing", writer);
                    return(false, -1);
                }
                
            }
            else
            {
                await _response.HttpResponse(401, "Token is missing", writer);
                return (false, -1);
            }
        }

        public string GenerateToken(string username)//generates token
        {
            return $"{username}-mtcgToken";//token in Project Spezifications format
        }

        public bool ValidateUser(string path, User user,StreamWriter writer)
        {
            string name = path.Substring("/users/".Length).Trim();
            string token = user.Token.Split(new[] { "-mtcgToken" }, StringSplitOptions.None)[0];
            Console.WriteLine($"** name : {name} , token name : {token} **");//debug
            if (name == token)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
