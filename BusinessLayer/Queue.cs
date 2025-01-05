using System.Collections.Concurrent;

namespace MonsterTradingCardGame.BusinessLayer
{
    //----------------------BATTLE--QUEUE----------------------
    internal class Queue
    {
        private static ConcurrentQueue<int> _playerQueue = new();
        private static readonly SemaphoreSlim _queueLock = new(1, 1);
        public int User1 { get; private set; }
        public int User2 { get; private set; }

        public async Task<bool> Waitlist(int userId)//returns true if a match is found, false if not
        {
            Console.WriteLine($"User {userId} has entered the queue.");//debug
            await _queueLock.WaitAsync();
            try
            {
                _playerQueue.Enqueue(userId);//add user to queue

                if (_playerQueue.Count >= 2)
                {
                    if (_playerQueue.TryDequeue(out int player1) && _playerQueue.TryDequeue(out int player2))//if there are at least 2 players in the queue, dequeue 2 players
                    {
                        User1 = player1;
                        User2 = player2;

                        Console.WriteLine($"Matched Player {User1} and Player {User2} for battle.");//debug
                        return true;
                    }
                }
            }
            finally
            {
                _queueLock.Release();
            }

            Console.WriteLine($"User {userId} is waiting for an opponent...");
            return false;
            
        }
    }
}
