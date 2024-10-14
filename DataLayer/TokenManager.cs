using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.DataLayer
{
    public class TokenManager
    {
        public string GetToken(int userId)//looks if user already has an existing token
        {
            using (var connection = Database.Database.Connection())//connects to database
            using (var command = new NpgsqlCommand("SELECT token FROM tokens WHERE user_id = @userId;", connection))//sql query for getting token
            {
                command.Parameters.AddWithValue("@userId", userId);//replaces username with the correct value in sql query

                using (var reader = command.ExecuteReader())//reads the answer
                {
                    if (reader.Read())
                    {
                        return reader.GetString(reader.GetOrdinal("token"));//if token exists it gets sent back
                    }
                }
            }

            return null;//if the user doesnt have a token it returns null
        }

        public string GenerateToken(string username)//makes a new token string with the username
        {
            return $"{username}-mtcgToken";//token in the same format as in the Project Spezifications
        }

        public void SaveToken(int userId, string token)//speichert token in datenbank
        {
            using (var connection = Database.Database.Connection())//connection to db
            using (var command = new NpgsqlCommand("INSERT INTO tokens (user_id, token) VALUES (@userId, @token) ON CONFLICT (user_id) DO UPDATE SET token = @token;", connection))//sql query die token zu den usernams speichert
            {
                command.Parameters.AddWithValue("@userId", userId);//replaces username with the correct value in sql query
                command.Parameters.AddWithValue("@token", token);//replaces token with the correct value in sql query
                command.ExecuteNonQuery();//executes the query
            }
        }

        public int? ValidateToken(string token)//validates token , to see if the already provided token exists for a user , if yes it returns the username
        {
            using (var connection = Database.Database.Connection())//connects to db
            using (var command = new NpgsqlCommand("SELECT user_id FROM tokens WHERE token = @token;", connection))//sql query for getting the username based on token
            {
                command.Parameters.AddWithValue("@token", token);//replaces the token placeholder with the correct value

                using (var reader = command.ExecuteReader())//reads the result
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(reader.GetOrdinal("user_id"));
                    }
                }
            }

            return null;//if token doesnt exist it returns null
        }

        public string CheckToken(int userId, string username, StreamWriter writer)//methode that handels token generation on login
        {
            string token = GetToken(userId);//checks for existing token based on username

            if (token != null)//if the user has a token it returns said token
            {
                writer.WriteLine("HTTP/1.1 200 Ok");
                writer.WriteLine();
                writer.WriteLine($"here : {username}: {token}");
                return token;
            }
            else //if user doesnt have a token , it generates one and returns the new token
            {
                token = GenerateToken(username);//methode to generate token
                SaveToken(userId, token);//methode to save token to database

                writer.WriteLine("HTTP/1.1 200 Ok");
                writer.WriteLine();
                writer.WriteLine($"here : {username}: {token}");
                return token;
            }
        }
    }

}