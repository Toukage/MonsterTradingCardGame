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

        //----------------------GET--USER--DATA----------------------

        public bool GetUser(string username, string password)//looks for user in database
        {
            Console.WriteLine("** inside get user **");

            using (var connection = Database.Database.Connection())//opens new connection
            using (var command = new NpgsqlCommand("SELECT * FROM Users WHERE username=@username AND password=@password", connection))//sql query for getting the user with the same credentials
            {
                Console.WriteLine("** inside get user database injection **");


                command.Parameters.AddWithValue("@username", username);//replaces the username/password place holder with the username
                command.Parameters.AddWithValue("@password", password);
                using (var reader = command.ExecuteReader())//reads and executes result
                {
                    
                    if (reader.Read())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public int? GetUserId(string username)//gets user id using username
            {
            using (var connection = Database.Database.Connection())//connects to db
            using (var command = new NpgsqlCommand("SELECT id FROM Users WHERE username = @username;", connection))//Sql querry to get the id from the user with the username
            {
                command.Parameters.AddWithValue("@username", username);//replaces the username place holder with the username

                using (var reader = command.ExecuteReader())//reads the result and executes the query
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(reader.GetOrdinal("id"));//returns the result, which in this case is the id
                    }
                }
            }

            return null;//returns null if the user doesn't exist
        }

        //----------------------GET--CARD--DATA----------------------(move to business layer)
        public Dictionary<string, Dictionary<string, object>> GetUserCards(int userId)//gets all owned cards for a user
        {
            Console.WriteLine("** inside get user cards **");

            Dictionary<string, Dictionary<string, object>> userCards = new Dictionary<string, Dictionary<string, object>>();//initialize dictionary to hold the card details

            using (var connection = Database.Database.Connection())//connects to db
            using (var command = new NpgsqlCommand("SELECT card_id, name, card_type, damage FROM Cards WHERE user_id=@userId", connection))//sql query that gets all cards info from userid
            {
                Console.WriteLine("** inside get user cards sql query **");//debug

                command.Parameters.AddWithValue("@userId", userId);//replaces the userid place holder with the users id

                using (var reader = command.ExecuteReader())//reads the results
                {
                    while (reader.Read())//as long as there are cardsit reads and saves them all
                    {
                        string cardId = reader.GetInt32(reader.GetOrdinal("card_id")).ToString();//converts id to string to be saved in dictionary
                        string cardName = reader.GetString(reader.GetOrdinal("name"));//gets name
                        string cardType = reader.GetString(reader.GetOrdinal("card_type"));//gets type
                        int damage = reader.GetInt32(reader.GetOrdinal("damage"));//gets dmg

                        Dictionary<string, object> cardDetails = new Dictionary<string, object>//new dicitionary to store card details
                        {
                            { "card_name", cardName },
                            { "card_type", cardType },
                            { "damage", damage }
                        };

                        userCards.Add(cardId, cardDetails);
                    }
                }
            }
            return userCards;
        }



        //----------------------WRITE--DATA----------------------
        public bool InsertUser(string username, string password, StreamWriter writer)//creates a new user in database
        {

            using (var connection = Database.Database.Connection())//connects to db
            using (var command = new NpgsqlCommand("INSERT INTO Users (username, password) VALUES (@username, @password)", connection))//sql query to insert new user with given credentials
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);
                try
                {
                    int rowsAffected = command.ExecuteNonQuery();//returns the number of rows that changed
                    return rowsAffected > 0;//if at least one was changed then true
                }
                catch (PostgresException ex) when (ex.SqlState == "23505")//409 status code
                {
                    writer.WriteLine("HTTP/1.1 409 Conflict");
                    writer.WriteLine("Content-Type: text/plain");
                    writer.WriteLine();
                    writer.WriteLine("User already exists");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Problem making new user : {ex.Message}");
                    return false;
                }

            }

            return false;
        }


        //----------------------REGISTRATION--UND--LOGIN----------------------(move to business layer)
        public void Login(string body, string headers, StreamWriter writer)//login handler
        {
            Console.WriteLine("** inside login function **");

            var (username, password) = _parser.UserDataParse(body, writer);//gets the username and password from the body

            bool Valid = GetUser(username, password);//checks if user exists
            if (Valid)//if yes check tokens and validate
            {
                int? userId = GetUserId(username);
                TokenManager tokenManager = new TokenManager();
                string token = tokenManager.CheckToken(userId.Value, username, writer);
            }
            else // 401 status code
            {
                writer.WriteLine("HTTP/1.1 401 Unauthorized");
                writer.WriteLine();
                writer.WriteLine("Login Failed");
            }

        }

        public void Register(string body, StreamWriter writer)//register Handler
        {
            Console.WriteLine("** inside register **");//debug

            var (username, password) = _parser.UserDataParse(body, writer);//gets the username and password from the body

            Console.WriteLine($"** the credentioals inside the registr function name : {username}, pass : {password}  **");//debug

            bool Valid = InsertUser(username, password, writer);//adds user to db
            if (Valid)
            {
                Console.WriteLine("** isnide valid if statement for register **");//debug

                writer.WriteLine("HTTP/1.1 201 Created"); // 201 status code
                writer.WriteLine();
            }
        }


        //---------------------User-data-Get---------------------(move to business layer)

        public void GetCards(string body, string headers, StreamWriter writer)//gets users cards and displays them
        {
            Console.WriteLine("** inside GetCards **");

            string? token = _parser.GetToken(headers, writer);//gets the token out of the header

            if (token == null)//401 status code
            {
                writer.WriteLine("HTTP/1.1 401 Unauthorized");
                writer.WriteLine();
                writer.WriteLine("No token provided or invalid format.");
                return;
            }

            TokenManager tokenManager = new TokenManager();
            int? userId = tokenManager.ValidateToken(token);//checks if token is valid

            if (userId != null)
            {
                Dictionary<string, Dictionary<string, object>> userCards = GetUserCards(userId.Value);

                if (userCards.Count > 0)//if user has cards , send them back
                {
                    Console.WriteLine("** isnide valid if statement for Cards **"); //debug
                    writer.WriteLine("HTTP/1.1 200 OK");// 200 status code
                    writer.WriteLine("Content-Type: application/json");
                    writer.WriteLine();
                    writer.WriteLine("Here are your Cards!");

                    foreach (var card in userCards)//displays cards
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
                    Console.WriteLine("** inside else statement for valid in Cards **"); //debug
                    writer.WriteLine("HTTP/1.1 404 Not Found"); // 404 status code
                    writer.WriteLine();
                    writer.WriteLine("No cards found for user.");
                }
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