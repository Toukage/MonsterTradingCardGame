using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MonsterTradingCardGame.Routing;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace MonsterTradingCardGame.DataLayer
{
    public class UserManagement
    {
        private readonly Parser _parser;

        public UserManagement(Parser parser)
            {
                _parser = parser;
            }

        //----------------------GET--DATA----------------------

        public bool GetUser(string username, string password)//looks for user in database
        {
            Console.WriteLine("** inside get user **");

            using (var connection = Database.Database.Connection())//opens new connection
            using (var command = new NpgsqlCommand("SELECT * FROM Users WHERE username=@username AND password=@password", connection))//sql query for getting the user with the same credentials
            {
                Console.WriteLine("** inside get user database injection **");
                

                command.Parameters.AddWithValue("@username", username);//replaces the username place holder with the username
                command.Parameters.AddWithValue("@password", password);//replaces the password place holder with the password

                using (var reader = command.ExecuteReader())//reads the entire result, doesnt save it 
                {
                    return reader.Read();
                }
            }
        }

        public int? GetUserId(string username)//gets user id using username
        {
            using (var connection = Database.Database.Connection())//opens new connection
            using (var command = new NpgsqlCommand("SELECT id FROM Users WHERE username = @username;", connection))//Sql querry to get the id from the user with the username
            {
                command.Parameters.AddWithValue("@username", username);//replaces the username place holder with the username

                using (var reader = command.ExecuteReader())//reads the entire result
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(reader.GetOrdinal("id"));//returns the result, which in this case is the id
                    }
                }
            }

            return null; // Return null if the user doesn't exist
        }

        public Dictionary<string, Dictionary<string, object>> GetUserCards(int userId)//gets all owned cards for a user
        {
            Console.WriteLine("** inside get user cards **");

            var userCards = new Dictionary<string, Dictionary<string, object>>();//initialize dictionary to hold the card details

            using (var connection = Database.Database.Connection())//opens new connection
            using (var command = new NpgsqlCommand("SELECT card_id, name, card_type, damage FROM Cards WHERE user_id=@userId", connection))//sql query that gets all cards info from userid
            {
                command.Parameters.AddWithValue("@userId", userId);//replaces the userid place holder with the users id

                using (var reader = command.ExecuteReader())//reads the results
                {
                    while (reader.Read())//as long as there are cards beind read, they r also being saved
                    {
                        string cardId = reader.GetInt32(reader.GetOrdinal("card_id")).ToString();//changes cards id to string to be saved in dictionary correctly
                        string cardName = reader.GetString(reader.GetOrdinal("name"));//gets card name
                        string cardType = reader.GetString(reader.GetOrdinal("card_type"));//get card type
                        int damage = reader.GetInt32(reader.GetOrdinal("damage"));//get card damage

                        userCards[cardId] = new Dictionary<string, object>//new dicitionary to store card details
                        {
                            { "card_name", cardName },
                            { "card_type", cardType },
                            { "damage", damage }
                        };
                    }                  
                }
            }
            return userCards;
        }
        


        //----------------------WRITE--DATA----------------------
        public bool InsertUser(string username, string password)//creates a new user in database
        {

            using (var connection = Database.Database.Connection())//opens new connection
            using (var command = new NpgsqlCommand("INSERT INTO Users (username, password) VALUES (@username, @password)", connection))//sql query to insert new user with given credentials
            {
                command.Parameters.AddWithValue("@username", username);//replaces the username place holder with the username
                command.Parameters.AddWithValue("@password", password);//replaces the password place holder with the password
                try
                {
                    int rowsAffected = command.ExecuteNonQuery();//returns the number of rows affected by the query
                    return rowsAffected > 0;//returns true if atlease 1 row has been affected (a user has been insereted)
                }
                catch (Exception ex)//error mssg
                {
                    Console.WriteLine($"Problem making new user : {ex.Message}");
                    return false;
                }

            }
        }

        //----------------------REGISTRATION--UND--LOGIN----------------------(move to business layer)
        public void Login(string body, string headers, StreamWriter writer)
        {
            Console.WriteLine("** Handling POST /sessions for login **");
            

            var (username, password) = _parser.UserDataParse(body, writer);//gets the username and password from the body

            bool Valid = GetUser(username, password);//checks if user exists
            if (!Valid)//if the user doesnt exist
            {
                string errorResponse = "{\"error\": \"Wrong Username or Password.\"}";
                writer.WriteLine("HTTP/1.1 401 Unauthorized");
                writer.WriteLine("Content-Type: application/json");
                writer.WriteLine($"Content-Length: {errorResponse.Length}");
                writer.WriteLine();
                writer.WriteLine(errorResponse);
                return;
            }

            int? userId = GetUserId(username);//gets the user id from the username
            TokenManager tokenManager = new TokenManager();//initializes tokenmanager so i can use it later on

            if (userId != null)
            {
                string token = tokenManager.CheckToken(userId.Value, username, writer);//checks if the user has token, if not it creates a new one
                string successResponse = $"{{\"message\": \"Login successful!\", \"token\": \"{token}\"}}";

                writer.WriteLine("HTTP/1.1 200 OK");
                writer.WriteLine("Content-Type: application/json");
                writer.WriteLine($"Content-Length: {successResponse.Length}");
                writer.WriteLine();
                writer.WriteLine(successResponse);
            }
            else
            {
                string errorResponse = "{\"error\": \"Error retrieving user ID.\"}";
                writer.WriteLine("HTTP/1.1 500 Internal Server Error");
                writer.WriteLine("Content-Type: application/json");
                writer.WriteLine($"Content-Length: {errorResponse.Length}");
                writer.WriteLine();
                writer.WriteLine(errorResponse);
            }
        }

        public void Register(string body, StreamWriter writer)//handels registration
        {
            Console.WriteLine("** Handling POST /users for registration **");
           
            var (username, password) = _parser.UserDataParse(body, writer);//gets the username and password from the body

            Console.WriteLine($"** Registering new user: {username}  **");

            bool Valid = InsertUser(username, password);//inserts new user into database
            if (Valid)
            {
                string responseBody = "{\"message\": \"User created successfully\"}";
                writer.WriteLine("HTTP/1.1 201 Created");
                writer.WriteLine("Content-Type: application/json");
                writer.WriteLine($"Content-Length: {responseBody.Length}");
                writer.WriteLine();
                writer.WriteLine(responseBody);
            }
            else
            {
                writer.WriteLine("HTTP/1.1 409 Conflict");
                writer.WriteLine("Content-Type: application/json");
                string errorBody = "{\"error\": \"Registration failed: Username may already exist.\"}";
                writer.WriteLine($"Content-Length: {errorBody.Length}");
                writer.WriteLine();
                writer.WriteLine(errorBody);
            }
        }


        //---------------------User-data-Get---------------------(move to business layer)

        public void GetCards(string body, string headers, StreamWriter writer)//gets users cards and displays them
        {
            Console.WriteLine("** handling GET /cards **");

            string? token = _parser.GetToken(headers, writer);//gets the token out of the header

            if(token == null)
            {
                writer.WriteLine("HTTP/1.1 401 Unauthorized");
                writer.WriteLine();
                writer.WriteLine("No token provided or invalid format.");
                return;
            }

            TokenManager tokenManager = new TokenManager();
            int? userId = tokenManager.ValidateToken(token);//validates token and returns userid

            if(userId != null)
            {
                Dictionary<string, Dictionary<string, object>> userCards = GetUserCards(userId.Value);//gets cards from Getusercards using userid

                if (userCards.Count > 0)//if the user has any cards they get displayed
                {
                    writer.WriteLine("HTTP/1.1 200 OK");
                    writer.WriteLine("Content-Type: application/json");
                    writer.WriteLine();
                    writer.WriteLine("Here are your Cards!");

                    foreach (var card in userCards)//iterates trough every card oject and displays it 
                    {
                        string cardId = card.Key;
                        var cardDetails = card.Value;

                        writer.WriteLine($"Card ID: {cardId}");
                        writer.WriteLine($"Card Name: {cardDetails["card_name"]}");
                        writer.WriteLine($"Card Type: {cardDetails["card_type"]}");
                        writer.WriteLine($"Damage: {cardDetails["damage"]}");
                        writer.WriteLine();
                    }
                }
                else
                {
                    writer.WriteLine("HTTP/1.1 404 Not Found");
                    writer.WriteLine();
                    writer.WriteLine("No cards found for user.");
                }

            }
            else
            {
                writer.WriteLine("HTTP/1.1 401 Unauthorized");
                writer.WriteLine();
                writer.WriteLine("Invalid or expired token.");
            }
            
            
        }
        public void GetStack(StreamWriter writer)//get user stack
        {
            writer.WriteLine("GetStack on yet Implemented");
        }

        public void GetDeck(StreamWriter writer)//get user deck
        {
            writer.WriteLine("GetDeck not yet Implemented");
        }
    }
}
