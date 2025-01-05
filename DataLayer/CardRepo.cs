using Npgsql;
using MonsterTradingCardGame.BusinessLayer;


namespace MonsterTradingCardGame.DataLayer
{
    internal class CardRepo
    {
        private readonly Response _response = new();

        //----------------------GET--DATA----------------------
        public async Task<List<Card>> GetCardInfoByIds(List<string> cardIds)//gets card information by card ids
        {
            Console.WriteLine("** Fetching card information by card IDs **");//debug
            List<Card> cards = new List<Card>();

            if (cardIds == null || cardIds.Count == 0)
            {
                Console.WriteLine("No card IDs provided.");//debug
                return cards;
            }
            try
            {
                await using (var connection = await Database.Database.Connection())
                {
                    foreach (var cardId in cardIds)
                    {
                        await using (var command = new NpgsqlCommand("SELECT * FROM Cards WHERE card_id = @cardId;", connection))
                        {
                            command.Parameters.AddWithValue("@cardId", cardId);
                            await using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    string cardName = reader.GetString(reader.GetOrdinal("name"));
                                    string cardType = reader.GetString(reader.GetOrdinal("card_type"));
                                    string cardMonster = reader.GetString(reader.GetOrdinal("card_monster"));
                                    string cardElement = reader.GetString(reader.GetOrdinal("card_element"));
                                    float cardDmg = reader.GetFloat(reader.GetOrdinal("card_dmg"));

                                    var card = new Card
                                    {
                                        CardID = cardId,
                                        CardName = cardName,
                                        CardType = cardType,
                                        CardMonster = cardMonster,
                                        CardElement = cardElement,
                                        CardDmg = cardDmg
                                    };

                                    cards.Add(card);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching card information: {ex.Message}");
            }

            return cards;
        }

        //----------------------WRITE--DATA----------------------
        public async Task<bool> InsertCard(string cardID, string cardName, string cardType, string cardMonster, string cardElement, float cardDmg) //inserts a card into the database
        {
            Console.WriteLine($"** inside Insert Card **");//debug

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("INSERT INTO Cards (card_id, name, card_type, card_monster, card_element, card_dmg) VALUES (@card_id, @name, @card_type, @card_monster, @card_element, @card_dmg)", connection))
                {
                    command.Parameters.AddWithValue("@card_id", cardID);
                    command.Parameters.AddWithValue("@name", cardName);
                    command.Parameters.AddWithValue("@card_type", cardType);
                    command.Parameters.AddWithValue("@card_monster", cardMonster);
                    command.Parameters.AddWithValue("@card_element", cardElement);
                    command.Parameters.AddWithValue("@card_dmg", cardDmg);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"PostgresException caught! Error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting card: {ex.Message}");
                return false;
            }           
            
        }
    }
}
