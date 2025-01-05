using Npgsql;

namespace MonsterTradingCardGame.DataLayer
{
    public class TokenRepo
    {
        private readonly Response _response = new();

        //----------------------GET--DATA----------------------
        public async Task<string> GetTokenFromId(int userId)//gets token from userid
        {
            Console.WriteLine("** inside GetTokenFromId **");//debug
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT token FROM Tokens WHERE user_id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    await using (var reader = await command.ExecuteReaderAsync())//reads the answer
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader.GetString(reader.GetOrdinal("token"));
                        }
                    }
                }
                Console.WriteLine("No token found for the specified user.");
                return null;
            }
            catch(NpgsqlException ex)
            {
                Console.WriteLine($"PostgresException caught in GetTokenFromId: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetTokenFromId: {ex.Message}");
                return null;
            }

        }

        public async Task<int> GetUserIdFromToken(string token)//gets id from token
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT user_id FROM Tokens WHERE token = @token;", connection))
                {
                    command.Parameters.AddWithValue("@token", token);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader.GetInt32(reader.GetOrdinal("user_id"));
                        }
                    }
                }
                Console.WriteLine("Token not found.");//debug
                return -1;
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetUserIdFromToken: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PostgresException caught in GetUserIdFromToken: {ex.Message}");
                return -1;
            }
        }

        public async Task <bool> ValidateToken(string token)//validates token
        {
            try
            {
                await using (var connection = await Database.Database.Connection())//connects to db
                await using (var command = new NpgsqlCommand("SELECT 1 FROM Tokens WHERE token = @token;", connection))
                {
                    command.Parameters.AddWithValue("@token", token);
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Console.WriteLine($"** token found !!!! **");//debug
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"PostgresException caught in ValidateToken: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in ValidateToken: {ex.Message}");
                return false;
            }           
        }

        //----------------------WRITE--DATA----------------------
        public async Task InsertToken(int userId, string token)//Saves token into DB
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("INSERT INTO tokens (user_id, token) VALUES (@userId, @token) ON CONFLICT (user_id) DO UPDATE SET token = @token;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@token", token);
                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"Debug: Token for user {userId} inserted/updated successfully.");
                    }
                }
            }
            catch(NpgsqlException ex)
            {
                Console.WriteLine($"PostgresException caught in InsertToken! Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in InsertToken! Error: {ex.Message}");
            }
        }
    }
}