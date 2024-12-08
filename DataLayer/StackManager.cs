using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.DataLayer
{
    internal class StackManager
    {
        private readonly Response _response = new();

        public int? GetStackIdForUser(int? userId)//gets stackId from UserId
        {
            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("SELECT stack_id FROM Stacks WHERE user_id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    var result = command.ExecuteScalar();

                    if (result != null)
                    {
                        Console.WriteLine($"** Stack found !!!! **");
                        return Convert.ToInt32(result); 
                    }
                    else
                    {
                        Console.WriteLine($"** No stack found **");
                        return -1;
                    }
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetStackIdForUser! Error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetStackIdForUser! Error:{ex.Message}");
                return null; 
            }
        }

        public int? CreateStackForUser(int? userId)//creates stack and returns id
        {
            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("INSERT INTO Stacks (user_id) VALUES (@userId) RETURNING stack_id;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    var result = command.ExecuteScalar();

                    if (result != null)
                    {
                        Console.WriteLine($"** Stack created for user ID {userId}. Stack ID: {result} **");
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        Console.WriteLine("** Failed to create stack **");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in CreateStackForUser: {ex.Message}");
                return null; 
            }
        }

        //get all card IDs for a user from the Stacks table
        public List<string> GetCardIdsFromStack(int? userId)
        {
            Console.WriteLine("** Fetching card IDs from user's stack **");
            List<string> cardIds = new List<string>();

            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("SELECT card_id FROM StackCards WHERE stack_id = (SELECT stack_id FROM Stacks WHERE user_id = @userId);", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string cardId = reader.GetString(reader.GetOrdinal("card_id"));
                            cardIds.Add(cardId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching card IDs: {ex.Message}");
            }

            return cardIds;
        }

    }


}
