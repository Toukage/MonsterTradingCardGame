using MonsterTradingCardGame.BusinessLayer;
using Npgsql;

namespace MonsterTradingCardGame.DataLayer
{
    internal class DeckRepo
    {
        private readonly Response _response = new();

        //----------------------GET--DATA----------------------
        public async Task<bool> GetDeckCards(int userId, Deck deck)//get all card IDs for a user from the Decks table

        {
            Console.WriteLine("** Fetching card IDs from user's deck **");
            List<string> cardIds = new List<string>();
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT card_1, card_2, card_3, card_4 FROM Decks WHERE user_id = @userId;", connection))//join in db 
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    await using (var reader = await command.ExecuteReaderAsync())
                    {

                        if (await reader.ReadAsync())
                        {
                            for (int i = 1; i <= 4; i++)
                            {
                                string columnName = $"card_{i}";
                                if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                                {
                                    cardIds.Add(reader.GetString(reader.GetOrdinal(columnName)));
                                }
                            }
                        }
                    }
                }

                if (cardIds.Count > 0)
                {
                    var cardManager = new CardRepo();
                    deck.DeckCards = await cardManager.GetCardInfoByIds(cardIds);
                }
                else
                {
                    deck.DeckCards = new List<Card>();
                }

                Console.WriteLine($"Card IDs fetched from deck: {string.Join(", ", cardIds)}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching card IDs: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsCardInDeck(int userId, string cardId)//checks if a card is in a user's deck
        {
            Console.WriteLine($"** Checking if card {cardId} is in the deck for user {userId} **");

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT COUNT(*) FROM Decks WHERE user_id = @userId AND (card_1 = @cardId OR card_2 = @cardId OR card_3 = @cardId OR card_4 = @cardId);", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@cardId", cardId);

                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if card is in deck: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> VerifyCards(int userId, List<string> cardIds)//checks if a user owns all the provided cards
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT COUNT(*) FROM StackCards WHERE stack_id = (SELECT stack_id FROM Stacks WHERE user_id = @userId) AND card_id = ANY(@cardIds);", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@cardIds", cardIds);
                    int matchCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return matchCount == cardIds.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying cards: {ex.Message}");
                return false;
            }
        }

        //----------------------WRITE--DATA----------------------
        public async Task<bool> CreateDeckForUser(int userId)//creates deck and returns id
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("INSERT INTO Decks (user_id) VALUES (@userId);", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"** Deck created.**");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("** Failed to create deck **");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in CreateDeckForUser: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateDeckCards(int userId, List<string> cardIds)//updates the deck with the provided cards
        {
            Console.WriteLine("** Updating deck for user **");

            if (cardIds.Count != 4)
            {
                Console.WriteLine("User provided invalid number of cards for the deck.");
                return false;
            }

            // Check for duplicates in the provided cards
            if (cardIds.Distinct().Count() != 4)
            {
                Console.WriteLine("User provided duplicate cards for the deck.");
                return false;
            }

            try
            {
                await using (var connection = await Database.Database.Connection())
                {
                    // Check if user already owns the cards
                    if (!await VerifyCards(userId, cardIds))
                    {
                        Console.WriteLine("User does not own the provided cards.");
                        return false;
                    }

                    // Update the deck by copying cards (not removing from stack)
                    await using (var command = new NpgsqlCommand(
                        "UPDATE Decks SET card_1 = @card1, card_2 = @card2, card_3 = @card3, card_4 = @card4 WHERE user_id = @userId;", connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@card1", cardIds[0]);
                        command.Parameters.AddWithValue("@card2", cardIds[1]);
                        command.Parameters.AddWithValue("@card3", cardIds[2]);
                        command.Parameters.AddWithValue("@card4", cardIds[3]);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating deck cards: {ex.Message}");
                return false;
            }
        }

        //----------------------DELETE--DATA----------------------
        public async Task<bool> RemoveCardFromDeck(int userId, string cardId)//removes a card from a user's deck
        {
            Console.WriteLine("** removing card from user's deck **");

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("UPDATE Decks SET card_1 = CASE WHEN card_1 = @cardId THEN NULL ELSE card_1 END, card_2 = CASE WHEN card_2 = @cardId THEN NULL ELSE card_2 END, card_3 = CASE WHEN card_3 = @cardId THEN NULL ELSE card_3 END, card_4 = CASE WHEN card_4 = @cardId THEN NULL ELSE card_4 END WHERE user_id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@cardId", cardId);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing card from deck: {ex.Message}");
                return false;
            }
        }
    }
}
