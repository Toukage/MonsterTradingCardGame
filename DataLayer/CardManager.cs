using MonsterTradingCardGame.Routing;
using Npgsql;
using Npgsql.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MonsterTradingCardGame.BusinessLayer;

namespace MonsterTradingCardGame.DataLayer
{
    internal class CardManager
    {
        private readonly Response _response = new();

        //----------------------GET--DATA----------------------
        public List<Card> GetCardInfoByIds(List<string> cardIds)
        {
            Console.WriteLine("** Fetching card information by card IDs **");
            List<Card> cards = new List<Card>();

            try
            {
                using (var connection = Database.Database.Connection())
                {
                    foreach (var cardId in cardIds)
                    {
                        using (var command = new NpgsqlCommand("SELECT * FROM Cards WHERE card_id = @cardId;", connection))
                        {
                            command.Parameters.AddWithValue("@cardId", cardId);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
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
        public bool InsertCard(string cardID, string cardName, string cardType, string cardMonster, string cardElement, float cardDmg) 
        {
            Console.WriteLine($"** inside Insert Card **");//debug
            using (var connection = Database.Database.Connection())
            using (var command = new NpgsqlCommand("INSERT INTO Cards (card_id, name, card_type, card_monster, card_element, card_dmg) VALUES (@card_id, @name, @card_type, @card_monster, @card_element, @card_dmg)", connection))
            {
                command.Parameters.AddWithValue("@card_id", cardID);
                command.Parameters.AddWithValue("@name", cardName);
                command.Parameters.AddWithValue("@card_type", cardType);
                command.Parameters.AddWithValue("@card_monster", cardMonster);
                command.Parameters.AddWithValue("@card_element", cardElement);
                command.Parameters.AddWithValue("@card_dmg", cardDmg);

                try
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
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
}
