using MonsterTradingCardGame.BusinessLayer;
using Npgsql;


namespace MonsterTradingCardGame.DataLayer
{
    public class StackRepo
    {

        //----------------------GET--DATA----------------------
        public async Task<int?>  GetStackIdForUser(User user)//gets stackId from UserId
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT stack_id FROM Stacks WHERE user_id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", user.UserId);
                    var result = await command.ExecuteScalarAsync();

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

        public async Task<List<string>> GetCardIdsFromStack(User user)//get all card IDs for a user from the Stacks table
        {
            Console.WriteLine("** Fetching card IDs from user's stack **");
            List<string> cardIds = new List<string>();

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT card_id FROM StackCards WHERE stack_id = (SELECT stack_id FROM Stacks WHERE user_id = @userId);", connection))//join in db 
                {
                    command.Parameters.AddWithValue("@userId", user.UserId);
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string cardId = reader.GetString(reader.GetOrdinal("card_id"));
                            cardIds.Add(cardId);
                        }
                    }
                }
                return cardIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching card IDs: {ex.Message}");
                return null;
            }
        }

        public async Task InsertCardsIntoUserStack(int userId, List<string> cardIds)//method to insert cards into the user's stack
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var insertCommand = new NpgsqlCommand("INSERT INTO StackCards (stack_id, card_id) VALUES ((SELECT stack_id FROM Stacks WHERE user_id = @userId), @cardId);", connection))
                {
                    insertCommand.Parameters.Add(new NpgsqlParameter("@userId", userId));
                    var cardIdParam = insertCommand.Parameters.Add("@cardId", NpgsqlTypes.NpgsqlDbType.Text);

                    foreach (string cardId in cardIds)
                    {
                        cardIdParam.Value = cardId;
                        await insertCommand.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting cards into stack: {ex.Message}");
            }
        }

        //----------------------CREATE--DATA----------------------
        public async Task<bool>  CreateStackForUser(User user)//creates stack and returns id
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("INSERT INTO Stacks (user_id) VALUES (@userId) RETURNING stack_id;", connection))
                {
                    command.Parameters.AddWithValue("@userId", user.UserId);
                    var result = await command.ExecuteScalarAsync();

                    if (result != null)
                    {
                        Console.WriteLine($"** Stack created for user ID {user.UserId}. Stack ID: {result} **");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("** Failed to create stack **");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in CreateStackForUser: {ex.Message}");
                return false; 
            }
        }

        //----------------------DELETE--DATA----------------------
        public async Task RemoveCardFromStack(int userId, string cardId)//removes card from stack
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand(@"DELETE FROM StackCards WHERE stack_id = (SELECT stack_id FROM Stacks WHERE user_id = @userId) AND card_id = @cardId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@cardId", cardId);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing card from stack: {ex.Message}");
            }
        }
    }
}
