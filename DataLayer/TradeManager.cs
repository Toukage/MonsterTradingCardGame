﻿using MonsterTradingCardGame.BusinessLayer;
using MonsterTradingCardGame.Routing;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.DataLayer
{
    internal class TradeManager
    {
        private readonly Response _response;

        public TradeManager()
        {
            _response = new Response();
        }

        public async Task<bool> GetCard(Trade deal)
        {
            Console.WriteLine($"** Fetching card ID from user's stack for user {deal.UserId} **");
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand(
                    "SELECT card_id FROM StackCards WHERE stack_id = (SELECT stack_id FROM Stacks WHERE user_id = @userId) AND card_id = @cardId;",
                    connection))
                {
                    command.Parameters.AddWithValue("@userId", deal.UserId);
                    command.Parameters.AddWithValue("@cardId", deal.OfferedCard.CardID); // FIXED HERE

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Console.WriteLine($"** Card {deal.OfferedCard.CardID} found in the stack! **");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"** Card {deal.OfferedCard.CardID} NOT found in the stack! **");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching card: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Trade>> GetAllTrades()
        {
            List<Trade> trades = new List<Trade>();

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand(@"SELECT t.trade_id, t.user_id AS TraderUserId, u.username AS Trader, t.card_id AS OfferedCardId, c_offered.name AS OfferedCard, t.card_type AS RequestedCardType, t.min_dmg AS RequestedMinimumDamage, t.status FROM Trades t JOIN Users u ON t.user_id = u.id JOIN Cards c_offered ON t.card_id = c_offered.card_id;", connection))
                {
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            trades.Add(new Trade
                            {
                                TradeId = reader.GetString(reader.GetOrdinal("trade_id")),
                                TraderUserId = reader.GetInt32(reader.GetOrdinal("TraderUserId")),
                                Trader = reader.GetString(reader.GetOrdinal("Trader")),
                                OfferedCard = new Card
                                {
                                    CardID = reader.GetString(reader.GetOrdinal("OfferedCardId")),
                                    CardName = reader.GetString(reader.GetOrdinal("OfferedCard"))
                                },
                                RequestedCardType = reader.GetString(reader.GetOrdinal("RequestedCardType")),
                                RequestedMinimumDamage = reader.GetFloat(reader.GetOrdinal("RequestedMinimumDamage")),
                                Status = reader.GetBoolean(reader.GetOrdinal("status"))
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching trades: {ex.Message}");
            }
            return trades;
        }

        public async Task<bool> CreateTrade(Trade trade)
        {
            Console.WriteLine("** Creating a new trade **");

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand(@"INSERT INTO Trades (trade_id, user_id, card_id, card_type, min_dmg) VALUES (@tradeId, @userId, @cardId, @cardType, @minDmg);", connection))
                {
                    command.Parameters.AddWithValue("@tradeId", trade.TradeId);
                    command.Parameters.AddWithValue("@userId", trade.TraderUserId);
                    command.Parameters.AddWithValue("@cardId", trade.OfferedCard.CardID);
                    command.Parameters.AddWithValue("@cardType", trade.RequestedCardType);
                    command.Parameters.AddWithValue("@minDmg", trade.RequestedMinimumDamage);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"Trade {trade.TradeId} created successfully!");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to create trade.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating trade: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteTradeAsync(Trade trade)
        {
            Console.WriteLine("** Deleting trade from the Trades table **");

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand(@"DELETE FROM Trades WHERE trade_id = @tradeId::TEXT AND user_id = @userId;", connection))
                {
                    // Parameterized query to prevent SQL Injection
                    command.Parameters.AddWithValue("@tradeId", trade.TradeId.ToString());
                    command.Parameters.AddWithValue("@userId", trade.TraderUserId);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"Trade with ID: {trade.TradeId} deleted successfully.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("No trade found matching the provided Trade ID for this User");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting trade: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteTradeById(string tradeId)
        {
            Console.WriteLine("** Deleting trade by Trade ID **");

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("DELETE FROM Trades WHERE trade_id = @tradeId;", connection))
                {
                    command.Parameters.AddWithValue("@tradeId", tradeId);
                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"Trade with ID: {tradeId} deleted successfully.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("No trade found matching the provided Trade ID.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting trade: {ex.Message}");
                return false;
            }
        }

        public async Task<int?> GetTradeCreator(Trade trade)
        {
            Console.WriteLine("** Fetching the Creators Userid from the Trades table using Trade ID **");

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand(@"SELECT user_id FROM Trades WHERE trade_id = @tradeId;", connection))
                {
                    // Parameterized query to prevent SQL Injection
                    command.Parameters.AddWithValue("@tradeId", trade.TradeId);
                    var result = await command.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int userId))
                    {
                        Console.WriteLine($"User ID found: {userId}");
                        return userId;
                    }
                    else
                    {
                        Console.WriteLine("No user associated with the given Trade ID.");
                        return null; // Return null if no matching trade
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching User ID: {ex.Message}");
                return null;
            }
        }

        public async Task<Trade> GetTradeById(string tradeId)
        {
            Console.WriteLine("** Fetching trade by Trade ID **");

            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand(@"SELECT trade_id, user_id, card_id, card_type, min_dmg, status FROM Trades WHERE trade_id = @tradeId;", connection))
                {
                    command.Parameters.AddWithValue("@tradeId", tradeId);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Trade
                            {
                                TradeId = reader.GetString(reader.GetOrdinal("trade_id")),
                                TraderUserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                OfferedCard = new Card
                                {
                                    CardID = reader.GetString(reader.GetOrdinal("card_id"))
                                },
                                RequestedCardType = reader.GetString(reader.GetOrdinal("card_type")),
                                RequestedMinimumDamage = reader.GetFloat(reader.GetOrdinal("min_dmg")),
                                Status = reader.GetBoolean(reader.GetOrdinal("status"))
                            };
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching trade: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateTradeStatus(Trade trade)
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand(@"UPDATE Trades SET status = @status WHERE trade_id = @tradeId;", connection))
                {
                    command.Parameters.AddWithValue("@tradeId", trade.TradeId);
                    command.Parameters.AddWithValue("@status", trade.Status);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating trade status: {ex.Message}");
                return false;
            }
        }

        public async Task<(string CardName, float CardDamage, string User)> GetCardAndUserInfo(int userId, string cardId)
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand(@"
            SELECT 
                c.name AS CardName, 
                c.card_dmg AS CardDamage, 
                u.username AS User
            FROM Cards c
            JOIN Users u ON u.id = @userId
            WHERE c.card_id = @cardId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@cardId", cardId);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string cardName = reader.GetString(reader.GetOrdinal("CardName"));
                            float cardDamage = reader.GetFloat(reader.GetOrdinal("CardDamage"));
                            string user = reader.GetString(reader.GetOrdinal("User"));

                            return (cardName, cardDamage, user);
                        }
                    }
                }
                return (null, 0, null); // Return default values if not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching card and user info: {ex.Message}");
                return (null, 0, null);
            }
        }
    }
}
