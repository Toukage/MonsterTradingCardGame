using MonsterTradingCardGame.BusinessLayer;
using Npgsql;


namespace MonsterTradingCardGame.DataLayer
{
    internal class ScoreRepo
    {
        private readonly Response _response = new();
        private static readonly object _scoreLock = new();
        //----------------------GET--DATA----------------------
        public async Task<bool> GetStats(Scores stats, StreamWriter writer)//gets stats for user
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT wins, losses, draws, total_games, elo FROM Stats WHERE user_id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", stats.UserId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int win = reader.GetInt32(reader.GetOrdinal("wins"));
                            int loss = reader.GetInt32(reader.GetOrdinal("losses"));
                            int draw = reader.GetInt32(reader.GetOrdinal("draws"));
                            int totalGames = reader.GetInt32(reader.GetOrdinal("total_games"));
                            int elo = reader.GetInt32(reader.GetOrdinal("elo"));


                            stats.Wins = win;
                            stats.Losses = loss;
                            stats.Draws = draw;
                            stats.TotalGames = totalGames;
                            stats.Elo = elo;


                            return true;
                        }
                    }
                }
                await _response.HttpResponse(404, "User not found", writer);
                return false;
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetStats: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetStats: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> GetScores(List <Scores> scoreboard, StreamWriter writer)//gets top 10 scores
        {
            lock (_scoreLock)//protects against concurrent modification of the list
            {
                scoreboard.Clear();
            }
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT u.username, s.user_id, s.elo FROM Stats s JOIN Users u ON s.user_id = u.id ORDER BY s.elo DESC LIMIT 10;", connection))
                {
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string username = reader.GetString(reader.GetOrdinal("username"));
                            int elo = reader.GetInt32(reader.GetOrdinal("elo"));
                            Console.WriteLine($"DEBUG: Username: {username}, Elo: {elo}");//debug

                            lock (_scoreLock)
                            {
                                scoreboard.Add(new Scores { Username = username, Elo = elo });
                            }

                        }
                    }
                    return scoreboard.Count > 0;
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetScores: {ex.Message}");
                await _response.HttpResponse(500, "Database error occurred", writer);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetScores: {ex.Message}");
                await _response.HttpResponse(500, "Internal server error occurred", writer);
                return false;
            }
        }

        //----------------------UPDATE--DATA----------------------
        public async Task<bool> UpdateScores(Battle player1, Battle player2, StreamWriter writer)//updates scores after battle
        {
            try
            {
                var players = new[] { player1, player2 };
                await using (var connection = await Database.Database.Connection())
                foreach (var player in players)
                {
                    await using (var command = new NpgsqlCommand("UPDATE Stats SET wins = wins + @wins, losses = losses + @losses, draws = draws + @draws,  elo = elo + @eloChange  WHERE user_id = @userId;", connection))
                    {
                        command.Parameters.AddWithValue("@wins", player.Win);
                        command.Parameters.AddWithValue("@losses", player.Loss);
                        command.Parameters.AddWithValue("@draws", player.Draw);
                        command.Parameters.AddWithValue("@eloChange", player.Elo);
                        command.Parameters.AddWithValue("@userId", player.Player);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"Scores updated for User ID {player.Player}. Rows affected: {rowsAffected}");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating scores for after battle {ex.Message}");
                await _response.HttpResponse(500, "Failed to update scores.", writer);
                return false;
            }
        }
    }
}
