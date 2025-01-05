using MonsterTradingCardGame.DataLayer;

namespace MonsterTradingCardGame.BusinessLayer
{
    internal class Scores
    {
        private readonly Response _response = new();
        private readonly ScoreRepo _scoreRepo = new();
        public int UserId { get; set; }
        public string Username { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int TotalGames { get; set; }
        public int Elo { get; set; }

        public List<Scores> ScoreBoard { get; set; } = new List<Scores>();
        private static readonly object _scoreBoardLock = new();

        //----------------------STATS----------------------
        public async Task Stats(User user, StreamWriter writer)
        {
            Console.WriteLine("** inside stats function **");//debug
            var stats = new Scores { UserId = user.UserId };
            if (await _scoreRepo.GetStats(stats, writer))
            {
                Console.WriteLine($"  --  ID : {stats.UserId} -- ");
                Console.WriteLine($"  --  Wins: {stats.Wins}   --  ");
                Console.WriteLine($"  --  Losses: {stats.Losses}  --  ");
                Console.WriteLine($"  --  Draws: {stats.Draws}  --  ");
                Console.WriteLine($"  --  Total Games: {stats.TotalGames}  --  ");
                Console.WriteLine($"  --  Elo: {stats.Elo}  --  ");

                await _response.HttpResponse(200, "Stats:", writer);
                writer.WriteLine($"Wins: {stats.Wins}");
                writer.WriteLine($"Losses: {stats.Losses}");
                writer.WriteLine($"Draws: {stats.Draws}");
                writer.WriteLine($"Total Games: {stats.TotalGames}");
                writer.WriteLine($"Elo: {stats.Elo}");
            }
            else
            {
                await _response.HttpResponse(404, "User stats not found", writer);
            }
        }

        //----------------------SCOREBOARD----------------------
        public async Task scoreBoard(StreamWriter writer)
        {
            Console.WriteLine("** inside scoreboard function **");//debug
            lock (_scoreBoardLock)//Prevents race conditions
            {
                ScoreBoard.Clear();
            }

            if (await _scoreRepo.GetScores(ScoreBoard, writer))
            {
                await _response.HttpResponse(200, "Scoreboard:", writer);
                lock (_scoreBoardLock)
                {
                    foreach (var score in ScoreBoard)
                    {
                        writer.WriteLine($"{score.Username} - Elo: {score.Elo}");
                    }
                }
            }
            else
            {
                await _response.HttpResponse(404, "User scoreboard not found", writer);
            }

        }
    }
}
