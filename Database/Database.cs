using System;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.Database
{
    public class Database
    {
        private static readonly string connectionString = "Host=localhost;Port=5432;Username=toukage;Password=mtcgserver;Database=MTCG_DB";

        public static async Task<NpgsqlConnection> Connection()
        {
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);//erstellt eine verbindung zur datenbank aus den infos von connectionString
            await connection.OpenAsync(); //opend die connection zur datenbank
            return connection;
        }
    }
}
