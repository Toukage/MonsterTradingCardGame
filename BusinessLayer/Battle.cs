using MonsterTradingCardGame.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonsterTradingCardGame.DataLayer;
using System.Reflection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using Npgsql.Internal;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Battle
    {
        public int Player { get; set; }
        public int RoundLost { get; set; } = 0;
        public int RoundWon { get; set; } = 0;
        public int Win { get; set; } = 0;
        public int Loss { get; set; } = 0;
        public int Draw { get; set; } = 0;
        public int Streak { get; set; } = 0;
        public int Elo { get; set; } = 0;

        public List<string> BattleLog { get; set; } = new List<string>();

        public void Log(string log)
        {
            BattleLog.Add(log);
            Console.WriteLine(log);
        }

        private readonly DeckManager _deck = new();
        private readonly Response _response = new();
        private readonly ScoreRepo _scoreRepo = new();
        private readonly Card _card = new();
        private static readonly SemaphoreSlim _battleLock = new(1, 1);

        private Card _lastCardPlayer1 = null;
        private Card _lastCardPlayer2 = null;
        private int _sameCardCountPlayer1 = 0;
        private int _sameCardCountPlayer2 = 0;

        public async Task CardBattle(int userId, StreamWriter writer)
        {
            int rounds = 0;
            int winner = -1;

            var queue = new Queue();
            if (await queue.Waitlist(userId))
            {
                await _battleLock.WaitAsync();

                try
                {
                    Log($"Battle starting between User {queue.User1} and User {queue.User2}");

                    var player1 = new Battle { Player = queue.User1 };
                    var player2 = new Battle { Player = queue.User2 };
                    // Create separate deck instances
                    var deck1 = new Deck { UserId = player1.Player };
                    var deck2 = new Deck { UserId = player2.Player };

                    await _deck.GetDeckCards(player1.Player, deck1);
                    await _deck.GetDeckCards(player2.Player, deck2);

                    // Create battle copies to modify without affecting the original
                    var battleDeck1 = new Deck { UserId = deck1.UserId, DeckCards = new List<Card>(deck1.DeckCards) };
                    var battleDeck2 = new Deck { UserId = deck2.UserId, DeckCards = new List<Card>(deck2.DeckCards) };

                    //Battle start

                    while (battleDeck1.DeckCards.Count > 0 && battleDeck2.DeckCards.Count > 0 && rounds < 10)
                    {
                        Card card1 = CardChecker(battleDeck1, ref _lastCardPlayer1, ref _sameCardCountPlayer1);
                        Card card2 = CardChecker(battleDeck2, ref _lastCardPlayer2, ref _sameCardCountPlayer2);
                        Log("");
                        Log($" -- Round {rounds + 1} starting : Player 1 : ({card1.CardName}) / ({card1.CardDmg}) vs Player 2 : ({card2.CardName}) / ({card2.CardDmg}) -- ");
                        Log("");

                        switch (_card.SpecialRules(card1, card2, BattleLog))//checks if a special rule applies
                        {
                            case 1://if card1 special rule wins
                                player1RoundWin(player1, player2, battleDeck1, battleDeck2, card2);
                                break;
                            case -1://if card1 special rule looses
                                player2RoundWin(player1, player2, battleDeck1, battleDeck2, card1);
                                break;
                            case 0://if no special rule applies
                                Log("No special rule applies.");

                                if (_card.MonsterFight(card1, card2))//checks if its a pure monsterfight to avoid elemental effects
                                {
                                    Log("Pure Monster Fight.");
                                    DmgCalculator(player1, card1, player2, card2, battleDeck1, battleDeck2, false);
                                }
                                else //if its not a pure monsterfight, apply elemental effects
                                {
                                    bool pityEffect = PityEffect(player1) || PityEffect(player2);
                                    DmgCalculator(player1, card1, player2, card2, battleDeck1, battleDeck2, !pityEffect);

                                }
                                break;
                            default:
                                Console.WriteLine("An error occurred in battle logic.");
                                break;
                        }


                        rounds++;
                        Log("");
                        Log($"Current Scores -> Player 1: {player1.RoundWon}, Player 2: {player2.RoundWon}");
                        Log("");
                    }

                    winner = WinnerDetermination(player1, player2, battleDeck1, battleDeck2, rounds);
                    await _card.FinalizeDeck(deck1, battleDeck1, player1.Player, BattleLog);
                    await _card.FinalizeDeck(deck2, battleDeck2, player2.Player, BattleLog);
                    await BattleResult(player1, player2, winner, writer);
                }
                finally
                {
                    _battleLock.Release();
                }
            }
            else
            {
                Console.WriteLine("Waiting for an opponent...");
                await _response.HttpResponse(200, "You have been added to the queue. Waiting for an opponent...", writer);
                return;
            }

        }

        public int WinnerDetermination(Battle player1, Battle player2, Deck battleDeck1, Deck battleDeck2, int rounds)
        {
            int winner = -1;

            if (rounds == 10)
            {
                Log("");
                Log("The battle ended because max Rounds were reached.");
                switch (player1.RoundWon.CompareTo(player2.RoundWon))
                {
                    case 1: //player 1 wins
                        player1Win(player1, player2);
                        winner = player1.Player;

                        return winner;

                    case -1: //player 2 wins
                        player2Win(player1, player2);
                        winner = player2.Player;
                        return winner;

                    default: //draw
                        draw(player1, player2);
                        winner = 0;
                        return winner;
                }
            }

            else if (battleDeck1.DeckCards.Count <= 0)//if player 1 lost all their cards
            {
                Log("");
                Log("The battle ended because Player 1 lost all their cards.");
                Log("");
                player2Win(player1, player2);
                winner = player2.Player;
                return winner;
            }
            else if (battleDeck2.DeckCards.Count <= 0)//if player 2 lost all their cards
            {
                Log("");
                Log("The battle ended because Player 2 lost all their cards.");
                Log("");
                player1Win(player1, player2);
                winner = player1.Player;
                return winner;
            }

            return winner;
        }

        public async Task BattleResult(Battle player1, Battle player2, int winner, StreamWriter writer)
        {
            if (winner == 0)
            {
                if (await _scoreRepo.UpdateScores(player1, player2, writer))
                {
                    await _response.HttpResponse(200, $"Battle Log:\n{string.Join("\n", BattleLog)} \n\nThe battle ended in a draw.", writer);
                }
            }
            else if (winner == player1.Player)
            {
                if (await _scoreRepo.UpdateScores(player1, player2, writer))
                {
                    await _response.HttpResponse(200, $"Battle Log:\n{string.Join("\n", BattleLog)} \n\nWinner: Player 1. Winner ID: {winner}, Loser ID: {player2.Player}.", writer);
                    Console.WriteLine("Game done, Player 1 won");
                }
            }
            else if (winner == player2.Player)
            {
                if (await _scoreRepo.UpdateScores(player1, player2, writer))
                {
                    await _response.HttpResponse(200, $"Battle Log:\n{string.Join("\n", BattleLog)} \n\nWinner: Player 2. Winner ID: {winner}, Loser ID: {player1.Player}.", writer);
                    Console.WriteLine("Game done, Player 2 won");
                }
            }
            else if (winner == -1)
            {
                Console.WriteLine("Error: Winner or Loser not determined.");
                await _response.HttpResponse(500, "Battle result could not be determined.", writer);
                return;
            }
        }

        public bool PityEffect(Battle player)
        {
            if (player.RoundLost >= 3)
            {
                Random rng = new Random();
                bool pityTriggered = rng.Next(2) == 1; // 50% chance
                if (pityTriggered)
                {
                    Log("The cards have taken pity on you! Elements will not have an effect this round.");
                    return true;
                }
            }
            return false;
        }

        public void DmgCalculator(Battle player1, Card card1, Battle player2, Card card2, Deck battleDeck1, Deck battleDeck2, bool applyElementalEffect)
        {
            float tempCard1Dmg = card1.CardDmg;
            float tempCard2Dmg = card2.CardDmg;

            var (buffApplied, buffPlayer, buffAmount) = RandomBuff();
            if (buffApplied)
            {
                if (buffPlayer == 1) card1.CardDmg *= (1 + buffAmount);
                else card2.CardDmg *= (1 + buffAmount);
            }

            if (applyElementalEffect)
            {
                switch (_card.ElementEffect(card1, card2, BattleLog))
                {
                    case 1: // card1 is effective against card2
                        tempCard1Dmg *= 2;
                        tempCard2Dmg /= 2;
                        Log($"{card1.CardName} was effective so damage was doubled : {tempCard1Dmg} | {card2.CardName} was not effective sp damage was halved : {tempCard2Dmg}");
                        break;

                    case -1: // card2 is effective against card1
                        tempCard1Dmg /= 2;
                        tempCard2Dmg *= 2;
                        Log($"{card2.CardName} was effective so damage was doubled : {tempCard2Dmg} | {card1.CardName} was not effective sp damage was halved : {tempCard1Dmg}");
                        break;

                    default: // no effect
                        break;
                }
            }

            // Compare using the modified temporary damage values
            switch (tempCard1Dmg.CompareTo(tempCard2Dmg))
            {
                case 1: // Player 1 wins
                    Log($"Player 1's card : {card1.CardName} with {tempCard1Dmg} beats Player 2's card 2 : {tempCard2Dmg}.");
                    Log($"So Player 2's Card goes to Player 1's Deck.");
                    player1RoundWin(player1, player2, battleDeck1, battleDeck2, card2);
                    break;
                case -1: // Player 2 wins
                    Log($"Player 2's card : {card2.CardName} with {tempCard2Dmg} beats Player 1's card 1 : {tempCard1Dmg}.");
                    Log($"So Player 1's Card goes to Player 2's Deck.");
                    player2RoundWin(player1, player2, battleDeck1, battleDeck2, card1);
                    break;
                default: // Draw
                    Log($"Both cards have the same damage value: {tempCard1Dmg}.");
                    drawRound(player1, player2);
                    break;
            }
        }

        private Card CardChecker(Deck battleDeck, ref Card lastCard, ref int sameCardCount)
        {
            Card drawnCard = _card.RandomCard(battleDeck);
            if (lastCard != null && lastCard.CardID == drawnCard.CardID)
            {
                sameCardCount++;
            }
            else
            {
                sameCardCount = 0;
            }

            if (sameCardCount >= 3 && battleDeck.DeckCards.Count > 1)
            {
                List<Card> otherCards = battleDeck.DeckCards.FindAll(c => c.CardID != drawnCard.CardID);
                drawnCard = otherCards[new Random().Next(otherCards.Count)];
                Log("Your winning card streak is over, have mercy!");
                sameCardCount = 0;
            }

            lastCard = drawnCard;
            return drawnCard;
        }
        public (bool buffApplied, int player, float buffAmount) RandomBuff()
        {
            Random rng = new Random();
            bool shouldBuff = rng.Next(2) == 1; // 50% chance for a buff
            if (shouldBuff)
            {
                int buffTarget = rng.Next(2) + 1; // Player 1 or Player 2
                float buffAmount = rng.Next(10, 31) / 100f; // Buff between 10% and 30%
                Log($"Player {buffTarget} received a {buffAmount * 100}% damage buff this round!");
                return (true, buffTarget, buffAmount);
            }
            return (false, 0, 0);
        }
        public void player1RoundWin(Battle player1, Battle player2, Deck battleDeck1, Deck battleDeck2, Card loosingCard)
        {
            player1.RoundWon++;
            player1.Streak = 0;
            player2.RoundLost++;
            player2.Streak++;
            battleDeck1.DeckCards.Add(loosingCard);
            battleDeck2.DeckCards.Remove(loosingCard);
            Log("");
            Log(" -- Player 1 wins the round. -- ");
            Log("");
            // Debugging output to print current cards for both decks
            Console.WriteLine($"x------x Updated Decks After Round Win x------x");
            Log($"Player 1 new Deck (UserId {battleDeck1.UserId}):");
            foreach (var card in battleDeck1.DeckCards)
            {
                Log($"- {card.CardName} | ID: {card.CardID} | DMG: {card.CardDmg}");
            }
            Log("");
            Log($"Player 2 new Deck (UserId {battleDeck2.UserId}):");
            foreach (var card in battleDeck2.DeckCards)
            {
                Log($"- {card.CardName} | ID: {card.CardID} | DMG: {card.CardDmg}");
            }
        }

        public void player2RoundWin(Battle player1, Battle player2, Deck battleDeck1, Deck battleDeck2, Card loosingCard)
        {
            player1.RoundLost++;
            player2.Streak++;
            player2.RoundWon++;
            player2.Streak = 0;
            battleDeck1.DeckCards.Remove(loosingCard);
            battleDeck2.DeckCards.Add(loosingCard);
            Log("");
            Log(" -- Player 2 wins the round. -- ");
            Log("");
            // Debugging output to print current cards for both decks
            Console.WriteLine($"x------x Updated Decks After Round Win x------x");
            Log($"Player 1 Deck (UserId {battleDeck1.UserId}):");
            foreach (var card in battleDeck1.DeckCards)
            {
                Log($"- {card.CardName} | ID: {card.CardID} | DMG: {card.CardDmg}");
            }
            Log("");
            Log($"Player 2 Deck (UserId {battleDeck2.UserId}):");
            foreach (var card in battleDeck2.DeckCards)
            {
                Log($"- {card.CardName} | ID: {card.CardID} | DMG: {card.CardDmg}");
            }
        }

        public void drawRound(Battle player1, Battle player2)
        {
            player1.Draw++;
            player2.Draw++;
            player1.Streak = 0;
            player2.Streak = 0;
            Log("");
            Log("The round enden in a draw.");
        }

        public void player1Win(Battle player1, Battle player2)
        {
            player1.Win++;
            player2.Loss++;
            player1.Elo = 3;
            player2.Elo = -5;
        }

        public void player2Win(Battle player1, Battle player2)
        {
            player2.Loss++;
            player1.Win++;
            player1.Elo = 3;
            player2.Elo = -5;
        }

        public void draw(Battle player1, Battle player2)
        {
            player1.Draw++;
            player2.Draw++;
        }

    }
}