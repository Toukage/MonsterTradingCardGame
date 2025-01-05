using MonsterTradingCardGame.BusinessLayer;
using Npgsql;

namespace MonsterTradingCardGame.DataLayer
{
    public class UserRepo
    {
        private readonly Response _response = new();

        //----------------------GET--DATA----------------------
        public async Task<bool> GetUser(string username, string password)//looks for user in database
        {
            Console.WriteLine("** inside get user **");
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT * FROM Users WHERE username=@username AND password=@password", connection))
                {
                    Console.WriteLine("** inside get user database injection **");//debug


                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);
                    Console.WriteLine("** after parameter added **");//debug
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        if (await reader.ReadAsync())
                        {
                            Console.WriteLine("** successfull read **");//debug
                            return true;
                        }
                    }
                }
                Console.WriteLine("** failed read, user not found **");
                return false;
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetUser: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetUser: {ex.Message}");
                return false;
            }
        }

        public async Task<int?> GetUserId(string username)//gets user id using username
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT id FROM Users WHERE username = @username;", connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader.GetInt32(reader.GetOrdinal("id"));//returns the result, which in this case is the id
                        }
                    }
                }
                Console.WriteLine("User not found.");
                return null;//returns null if the user doesn't exist
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetUserId: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetUserId: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CheckAdmin(int userId)//Checks if user is admin
        {
            Console.WriteLine($"** inside CheckAdmin **");//debug
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT admin FROM Users WHERE id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            bool isAdmin = reader.GetBoolean(0);
                            Console.WriteLine($"** the users admin status : {isAdmin} **");//debug
                            return isAdmin;
                        }
                        else
                        {
                            Console.WriteLine("No user found with the specified userId.");//debug
                            return false;
                        }
                    }
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in CheckAdmin: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in CheckAdmin: {ex.Message}");
                return false;
            }
        }

        public async Task<int?> GetCoins(int userId)//gets the amount of coins a user has
        {
            Console.WriteLine("** inside CheckCoins methode **");
            try
            {
                await using (var connection = await Database.Database.Connection())
          await using (var command = new NpgsqlCommand("SELECT coins FROM Users WHERE id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int coins = reader.GetInt32(reader.GetOrdinal("coins"));
                            Console.WriteLine($"** Success! User : {userId} has {coins} coins. **");//debug
                            return coins;
                        }
                    }
                }
                Console.WriteLine("User not found.");//debug
                return null;
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in CheckCoins: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in CheckCoins: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> GetProfile(User user, StreamWriter writer)//gets user profile
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("SELECT name, bio, image FROM UserProfile WHERE user_id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", user.UserId);
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string profileName = reader.GetString(reader.GetOrdinal("name"));
                            string bio = reader.GetString(reader.GetOrdinal("bio"));
                            string image = reader.GetString(reader.GetOrdinal("image"));


                            user.ProfileName = profileName;
                            user.Bio = bio;
                            user.Image = image;
                            return true;
                        }
                    }
                }
                await _response.HttpResponse(404, "User not found", writer);
                return false;
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in GetProfile: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in GetProfile: {ex.Message}");
                return false;
            }
        }

        //----------------------WRITE--DATA----------------------
        public async Task<bool> InsertUser(string username, string password, StreamWriter writer)//Creates a new user in the database
        {
            await using (var connection = await Database.Database.Connection())
            {
                string sqlQuery;
                if (username != "admin")//checks if the user is an admin
                {
                    sqlQuery = "INSERT INTO Users (username, password) VALUES (@username, @password)";
                }
                else
                {
                    sqlQuery = "INSERT INTO Users (username, password, admin) VALUES (@username, @password, TRUE)";
                }

                await using (var command = new NpgsqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);

                    try
                    {
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                    catch (PostgresException ex) when (ex.SqlState == "23505")
                    {
                        Console.WriteLine($"PostgresException caught in InsertUser: {ex.Message}");
                        await _response.HttpResponse(409, "User already exists", writer);

                        return false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception caught in InsertUser: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public async Task<bool> UpdateProfile(User user, StreamWriter writer)//updates user profile
        {
            try
            {
                await using (var connection = await Database.Database.Connection())
                await using (var command = new NpgsqlCommand("UPDATE UserProfile SET name = @profilename, bio = @bio, image = @image WHERE user_id = @userId;", connection))
                {
                    command.Parameters.AddWithValue("@userId", user.UserId);
                    command.Parameters.AddWithValue("@profilename", user.ProfileName);
                    command.Parameters.AddWithValue("@bio", user.Bio);
                    command.Parameters.AddWithValue("@image", user.Image);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    //Check if the update was successful
                    if (rowsAffected > 0)
                    {
                        await _response.HttpResponse(200, "Profile updated successfully", writer);
                        return true;
                    }
                    else
                    {
                        await _response.HttpResponse(404, "User profile not found", writer);
                        return false;
                    }
                    
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"PostgresException caught in UpdateProfile: {ex.Message}");
                await _response.HttpResponse(500, "Database error occurred", writer);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in UpdateProfile: {ex.Message}");
                await _response.HttpResponse(500, "Internal server error occurred", writer);
                return false;
            }
        }

    }
}