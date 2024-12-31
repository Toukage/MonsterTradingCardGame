using MonsterTradingCardGame.BusinessLayer;
using MonsterTradingCardGame.Routing;
using Npgsql;
using Npgsql.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.DataLayer
{
    internal class PackageManager
    {
        private readonly Response _response = new();

        //----------------------GET--DATA----------------------
        public int GetRandomPack(StreamWriter writer)//gets random pack for purchasing
        {
            Console.WriteLine("** Picking random pack **");
            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("SELECT package_id FROM Packages ORDER BY RANDOM() LIMIT 1;", connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        Console.WriteLine($"** Random pack selected with ID: {result} **");
                        return Convert.ToInt32(result);//returns random pack
                    }
                    else
                    {
                        Console.WriteLine("** No packs available to select from. **");
                        _response.HttpResponse(404, "No packages available", writer);
                        return -1; 
                    }
                }
            }
            catch(NpgsqlException ex)
            {
                Console.WriteLine($"PostgresException caught in InsertPack! Error: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in PickRandomPack: {ex.Message}");
                return -1;
            }
        }

        //----------------------WRITE--DATA----------------------
        public bool InsertPack(string cardID1, string cardID2, string cardID3, string cardID4, string cardID5, StreamWriter writer)//creats pack
        {
            Console.WriteLine($"** inside Insert pack **");//debug
            using (var connection = Database.Database.Connection())
            using (var command = new NpgsqlCommand("INSERT INTO Packages (card_1, card_2, card_3, card_4, card_5) VALUES (@card_id1, @card_id2, @card_id3, @card_id4, @card_id5)", connection))
            {
                command.Parameters.AddWithValue("@card_id1", cardID1);
                command.Parameters.AddWithValue("@card_id2", cardID2);
                command.Parameters.AddWithValue("@card_id3", cardID3);
                command.Parameters.AddWithValue("@card_id4", cardID4);
                command.Parameters.AddWithValue("@card_id5", cardID5);

                try
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"** pack created **");//debug
                    _response.HttpResponse(201, "Package created successfully", writer);

                    return rowsAffected > 0;
                }
                catch (NpgsqlException ex)
                {
                    Console.WriteLine($"PostgresException caught in InsertPack! Error: {ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception caught in InsertPack! Error: {ex.Message}");//debug
                    return false;
                }
            }
        }

        //----------------------UPDATE--DATA----------------------
        public bool PayPack(int? userId)//detucts 5 coins from user
        {
            Console.WriteLine("** inside PayPack **");
            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("UPDATE Users SET coins = coins - 5 WHERE id = @userId AND coins >= 5", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"** 5 coins were successfully deducted for user ID {userId}. **");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("** Insufficient funds or user not found. **");
                        return false;
                    }
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in PayPack! Error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in PayPack! Error: {ex.Message}");
                return false;
            }
        }

        public bool TransferPack(int? userId, int packId, StreamWriter writer)//moves cards from pack to stack
        {
            Console.WriteLine("** Transferring cards from pack to user stack **");

            try
            {
                using (var connection = Database.Database.Connection())
                {
                    List<string> cardIds = GetCardIdsFromPack(packId, connection);//gets cards from pack

                    if (cardIds == null || cardIds.Count == 0)
                    {
                        Console.WriteLine("** Pack not found or no cards in pack. **");
                        return false;
                    }

                    InsertCardsIntoUserStack(userId, cardIds, connection);//moves them to stack

                    Console.WriteLine("** Cards successfully transferred to user's stack **");
                    return true;
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in TransferCardsToUserStack! Error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in TransferCardsToUserStack! Error: {ex.Message}");
                return false;
            }
        }

        //gets all card ids from a pack
        private List<string> GetCardIdsFromPack(int packId, NpgsqlConnection connection)
        {
            List<string> cardIds = new List<string>();
            using (var command = new NpgsqlCommand("SELECT card_1, card_2, card_3, card_4, card_5 FROM Packages WHERE package_id = @packId;", connection))
            {
                command.Parameters.AddWithValue("@packId", packId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        for (int i = 1; i <= 5; i++)
                        {
                            cardIds.Add(reader.GetString(reader.GetOrdinal($"card_{i}")));
                        }
                    }
                }
            }

            return cardIds;
        }

        //method to insert cards into the user's stack
        private void InsertCardsIntoUserStack(int? userId, List<string> cardIds, NpgsqlConnection connection)
        {
            using (var insertCommand = new NpgsqlCommand("INSERT INTO StackCards (stack_id, card_id) VALUES ((SELECT stack_id FROM Stacks WHERE user_id = @userId), @cardId);", connection))
            {
                insertCommand.Parameters.Add(new NpgsqlParameter("@userId", userId));
                var cardIdParam = insertCommand.Parameters.Add("@cardId", NpgsqlTypes.NpgsqlDbType.Text);

                foreach (string cardId in cardIds)
                {
                    cardIdParam.Value = cardId;
                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        //----------------------DELETE--DATA----------------------
        public bool DeletePack(int packId)//deletes pack after purchase
        {
            Console.WriteLine("** Deleting pack after successful transfer **");

            try
            {
                using (var connection = Database.Database.Connection())
                using (var command = new NpgsqlCommand("DELETE FROM Packages WHERE package_id = @packId;", connection))
                {
                    command.Parameters.AddWithValue("@packId", packId);
                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"** Pack with ID {packId} deleted successfully. **");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("** Failed to delete pack. **");
                        return false;
                    }
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in DeletePack! Error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in DeletePack! Error: {ex.Message}");
                return false;
            }
        }
    }
}
