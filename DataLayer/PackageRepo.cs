using Npgsql;

namespace MonsterTradingCardGame.DataLayer
{
    internal class PackageRepo
    {
        private readonly Response _response = new();
        private readonly StackRepo _stackMan = new();

        //----------------------GET--DATA----------------------
        public async Task<int> GetPack(StreamWriter writer)//gets pack for purchasing
        {
            Console.WriteLine("** Picking random pack **");//debug
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT package_id FROM Packages ORDER BY package_id ASC LIMIT 1;", connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        Console.WriteLine($"** Pack selected with ID: {result} **");//debug
                        return Convert.ToInt32(result);//returns random pack
                    }
                    else
                    {
                        Console.WriteLine("** No packs available to select from. **");//debug
                        await _response.HttpResponse(404, "No packages available", writer);
                        return -1; 
                    }
                }
            }
            catch(NpgsqlException ex)
            {
                Console.WriteLine($"PostgresException caught in GetPack! Error: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetPack: {ex.Message}");
                return -1;
            }
        }

        //----------------------WRITE--DATA----------------------
        public async Task<bool> InsertPack(string cardID1, string cardID2, string cardID3, string cardID4, string cardID5, StreamWriter writer)//creats pack
        {
            Console.WriteLine($"** inside Insert pack **");//debug
            await using (var connection = await Database.Database.Connection())
            await using (var command = new NpgsqlCommand("INSERT INTO Packages (card_1, card_2, card_3, card_4, card_5) VALUES (@card_id1, @card_id2, @card_id3, @card_id4, @card_id5)", connection))
            {
                command.Parameters.AddWithValue("@card_id1", cardID1);
                command.Parameters.AddWithValue("@card_id2", cardID2);
                command.Parameters.AddWithValue("@card_id3", cardID3);
                command.Parameters.AddWithValue("@card_id4", cardID4);
                command.Parameters.AddWithValue("@card_id5", cardID5);

                try
                {
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"** pack created **");//debug
                    await _response.HttpResponse(201, "Package created successfully", writer);

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
        public async Task<bool> PayPack(int? userId)//detucts 5 coins from user
        {
            Console.WriteLine("** inside PayPack **");//debug
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("UPDATE Users SET coins = coins - 5 WHERE id = @userId AND coins >= 5", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"** 5 coins were successfully deducted for user ID {userId}. **");//debug
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("** Insufficient funds or user not found. **");//debug
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

        public async Task<bool> TransferPack(int? userId, int packId, StreamWriter writer)//moves cards from pack to stack
        {
            Console.WriteLine("** Transferring cards from pack to user stack **");//debug

            try
            {
                await using (var connection = await Database.Database.Connection())
                {
                    List<string> cardIds = await GetCardIdsFromPack(packId, connection);//gets cards from pack

                    if (cardIds == null || cardIds.Count == 0)
                    {
                        Console.WriteLine("** Pack not found or no cards in pack. **");//debug
                        return false;
                    }

                    await _stackMan.InsertCardsIntoUserStack((int)userId, cardIds);//moves them to stack

                    Console.WriteLine("** Cards successfully transferred to user's stack **");//debug
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
        private async Task<List<string>> GetCardIdsFromPack(int packId, NpgsqlConnection connection)
        {
            List<string> cardIds = new List<string>();
            await using (var command = new NpgsqlCommand("SELECT card_1, card_2, card_3, card_4, card_5 FROM Packages WHERE package_id = @packId;", connection))
            {
                command.Parameters.AddWithValue("@packId", packId);
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
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

        //----------------------DELETE--DATA----------------------
        public async Task<bool> DeletePack(int packId)//deletes pack after purchase
        {
            Console.WriteLine("** Deleting pack after successful transfer **");//debug

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("DELETE FROM Packages WHERE package_id = @packId;", connection))
                {
                    command.Parameters.AddWithValue("@packId", packId);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"** Pack with ID {packId} deleted successfully. **");//debug
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("** Failed to delete pack. **");//debug
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
