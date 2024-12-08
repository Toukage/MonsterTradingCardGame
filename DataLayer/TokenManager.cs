using Newtonsoft.Json.Linq;
using Npgsql;
using Npgsql.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.DataLayer
{
    public class TokenManager
    {
        private readonly Response _response = new();

        //----------------------GET--DATA----------------------
        public string GetTokenFromId(int userId)//gets token from userid
        {
            Console.WriteLine("** inside GetTokenFromId **");//debug
            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("SELECT token FROM Tokens WHERE user_id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = command.ExecuteReader())//reads the answer
                    {
                        if (reader.Read())
                        {
                            return reader.GetString(reader.GetOrdinal("token"));
                        }
                    }
                }
                Console.WriteLine("No token found for the specified user.");
                return null;
            }
            catch(NpgsqlException ex)
            {
                Console.WriteLine($"PostgresException caught in GetTokenFromId: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetTokenFromId: {ex.Message}");
                return null;
            }

        }

        public int? GetUserIdFromToken(string token)//gets id from token
        {
            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("SELECT user_id FROM Tokens WHERE token = @token;", connection))
                {
                    command.Parameters.AddWithValue("@token", token);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetInt32(reader.GetOrdinal("user_id"));
                        }
                    }
                }
                Console.WriteLine("Token not found.");//debug
                return null;
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetUserIdFromToken: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PostgresException caught in GetUserIdFromToken: {ex.Message}");
                return null;
            }
        }

        public bool ValidateToken(string token)//validates token, if valid returns userID
        {
            try
            {
                using (var connection = Database.Database.Connection())//connects to db
                using (var command = new NpgsqlCommand("SELECT 1 FROM Tokens WHERE token = @token;", connection))
                {
                    command.Parameters.AddWithValue("@token", token);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Console.WriteLine($"** token found !!!! **");
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"PostgresException caught in ValidateToken: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in ValidateToken: {ex.Message}");
                return false;
            }           
        }

        //----------------------WRITE--DATA----------------------
        public void InsertToken(int userId, string token)//Saves token into DB
        {
            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("INSERT INTO tokens (user_id, token) VALUES (@userId, @token) ON CONFLICT (user_id) DO UPDATE SET token = @token;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@token", token);
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Debug: Token for user {userId} inserted/updated successfully.");
                }
            }
            catch(NpgsqlException ex)
            {
                Console.WriteLine($"PostgresException caught in InsertToken! Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in InsertToken! Error: {ex.Message}");
            }
        }
    }
}