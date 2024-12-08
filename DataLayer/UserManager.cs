using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MonsterTradingCardGame.BusinessLayer;
using MonsterTradingCardGame.Routing;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace MonsterTradingCardGame.DataLayer
{
    public class UserManager
    {
        private readonly Response _response = new();

        //----------------------GET--DATA----------------------
        public bool GetUser(string username, string password)//looks for user in database
        {
            Console.WriteLine("** inside get user **");
            try
            {
                using (var connection = Database.Database.Connection())

                using (var command = new NpgsqlCommand("SELECT * FROM Users WHERE username=@username AND password=@password", connection))
                {
                    Console.WriteLine("** inside get user database injection **");//debug


                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);
                    Console.WriteLine("** after parameter added **");//debug
                    using (var reader = command.ExecuteReader())
                    {

                        if (reader.Read())
                        {
                            Console.WriteLine("** successfull read **");//debug
                            return true;
                        }
                    }

                }
                Console.WriteLine("** failed read, user not found **");
                return false;
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetUser: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetUser: {ex.Message}");
                return false;
            }
        }

        public int? GetUserId(string username)//gets user id using username
        {
            try
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
                Console.WriteLine("User not found.");
                return null;//returns null if the user doesn't exist
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetUserId: {ex.Message}");
                return null;
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Exception caught in GetUserId: {ex.Message}");
                return null;
            }
        }

        public bool CheckAdmin(int userId)//Checks if user is admin
        {
            Console.WriteLine($"** inside CheckAdmin **");//debug
            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("SELECT admin FROM Users WHERE id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool isAdmin = reader.GetBoolean(0);
                            Console.WriteLine($"** the users admin status : {isAdmin} **");//debug
                            return isAdmin;
                        }
                        else
                        {
                            Console.WriteLine("No user found with the specified userId.");//debug
                            return false;
                        }
                    }
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in CheckAdmin: {ex.Message}");
                return false;
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Exception caught in CheckAdmin: {ex.Message}");
                return false;
            }

            
        }

        public int? GetCoins(int userId)
        {
            Console.WriteLine("** inside CheckCoins methode **");
            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("SELECT coins FROM Users WHERE id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int coins = reader.GetInt32(reader.GetOrdinal("coins"));
                            Console.WriteLine($"** Success! User : {userId} has {coins} coins. **"); //debug
                            return coins;
                        }
                    }
                }
                Console.WriteLine("User not found.");//debug
                return null;
            }
            catch (PostgresException ex) 
            {
                Console.WriteLine($"PostgresException caught in CheckCoins: {ex.Message}");
                return null;
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Exception caught in CheckCoins: {ex.Message}");
                return null;
            }           
        }

        //----------------------WRITE--DATA----------------------
        public bool InsertUser(string username, string password, StreamWriter writer)//Creates a new user in the database
        {
            using (var connection = Database.Database.Connection())
            {
                // Check if the username is "admin"
                string sqlQuery;
                if (username != "admin")
                {
                    sqlQuery = "INSERT INTO Users (username, password) VALUES (@username, @password)";
                }
                else
                {
                    sqlQuery = "INSERT INTO Users (username, password, admin) VALUES (@username, @password, TRUE)";
                }

                using (var command = new NpgsqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);

                    try
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                    catch (PostgresException ex) when (ex.SqlState == "23505")
                    {
                        Console.WriteLine($"PostgresException caught in InsertUser: {ex.Message}");
                        _response.HttpResponse(409, "User already exists", writer);
                        
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception caught in InsertUser: {ex.Message}");
                        return false;
                    }
                }
            }
        }  
    }
}