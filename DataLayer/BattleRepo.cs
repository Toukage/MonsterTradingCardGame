using MonsterTradingCardGame.BusinessLayer;

namespace MonsterTradingCardGame.DataLayer
{
    public class BattleRepo
    {
        private readonly Response _response = new();
        private readonly Card _card = new();

        //----------------------ROUND--DETERMINATION----------------------
        public void player1RoundWin(Battle player1, Battle player2, Deck battleDeck1, Deck battleDeck2, Card loosingCard, List<string> Log)//logic for if player 1 wins a round
        {
            player1.RoundWon++;
            player1.Streak = 0;
            player2.RoundLost++;
            player2.Streak++;
            battleDeck1.DeckCards.Add(loosingCard);
            battleDeck2.DeckCards.Remove(loosingCard);
            Log.Add("");
            Log.Add(" -- Player 1 wins the round. -- ");
            Log.Add("");
            // Debugging output to print current cards for both decks
            Console.WriteLine($"x------x Updated Decks After Round Win x------x");
            Log.Add($"Player 1 new Deck (UserId {battleDeck1.UserId}):");
            foreach (var card in battleDeck1.DeckCards)
            {
                Log.Add($"- {card.CardName} | ID: {card.CardID} | DMG: {card.CardDmg}");
            }
            Log.Add("");
            Log.Add($"Player 2 new Deck (UserId {battleDeck2.UserId}):");
            foreach (var card in battleDeck2.DeckCards)
            {
                Log.Add($"- {card.CardName} | ID: {card.CardID} | DMG: {card.CardDmg}");
            }
        }

        public void player2RoundWin(Battle player1, Battle player2, Deck battleDeck1, Deck battleDeck2, Card loosingCard, List<string> Log)//logic for if player 2 wins a round
        {
            player1.RoundLost++;
            player2.Streak++;
            player2.RoundWon++;
            player2.Streak = 0;
            battleDeck1.DeckCards.Remove(loosingCard);
            battleDeck2.DeckCards.Add(loosingCard);
            Log.Add("");
            Log.Add(" -- Player 2 wins the round. -- ");
            Log.Add("");
            // Debugging output to print current cards for both decks
            Console.WriteLine($"x------x Updated Decks After Round Win x------x");
            Log.Add($"Player 1 Deck (UserId {battleDeck1.UserId}):");
            foreach (var card in battleDeck1.DeckCards)
            {
                Log.Add($"- {card.CardName} | ID: {card.CardID} | DMG: {card.CardDmg}");
            }
            Log.Add("");
            Log.Add($"Player 2 Deck (UserId {battleDeck2.UserId}):");
            foreach (var card in battleDeck2.DeckCards)
            {
                Log.Add($"- {card.CardName} | ID: {card.CardID} | DMG: {card.CardDmg}");
            }
        }

        public void drawRound(Battle player1, Battle player2, List<string> Log)//logic for if a round ends in a draw
        {
            player1.Draw++;
            player2.Draw++;
            player1.Streak = 0;
            player2.Streak = 0;
            Log.Add("");
            Log.Add("The round enden in a draw.");
        }

        //----------------------BATTLE--DETERMINATION----------------------
        public void player1Win(Battle player1, Battle player2)//logic for if player 1 wins the battle
        {
            player1.Win++;
            player2.Loss++;
            player1.Elo = 3;
            player2.Elo = -5;
        }

        public void player2Win(Battle player1, Battle player2)//logic for if player 2 wins the battle
        {
            player2.Loss++;
            player1.Win++;
            player1.Elo = 3;
            player2.Elo = -5;
        }

        public void draw(Battle player1, Battle player2)//logic for if the battle ends in a draw
        {
            player1.Draw++;
            player2.Draw++;
        }

        //----------------------UNIQUE--FEATURE----------------------
        public bool PityEffect(Battle player, List<string> Log)
        {
            if (player.RoundLost >= 3)
            {
                Random rng = new Random();
                bool pityTriggered = rng.Next(2) == 1;//50% chance
                if (pityTriggered)
                {
                    Log.Add("The cards have taken pity on you! Elements will not have an effect this round.");
                    return true;
                }
            }
            return false;
        }

        public Card CardChecker(Deck battleDeck, ref Card lastCard, ref int sameCardCount, List<string> Log)
        {
            Card drawnCard = _card.RandomCard(battleDeck);
            if (lastCard != null && lastCard.CardID == drawnCard.CardID)//checks if the same card was drawn 3 times in a row
            {
                sameCardCount++;
            }
            else
            {
                sameCardCount = 0;
            }

            if (sameCardCount >= 3 && battleDeck.DeckCards.Count > 1)//if the same card was drawn 3 times in a row, a new card is drawn
            {
                List<Card> otherCards = battleDeck.DeckCards.FindAll(c => c.CardID != drawnCard.CardID);
                drawnCard = otherCards[new Random().Next(otherCards.Count)];
                Log.Add("Your winning card streak is over, have mercy!");
                sameCardCount = 0;
            }

            lastCard = drawnCard;
            return drawnCard;
        }

        public (bool buffApplied, int player, float buffAmount) RandomBuff(List<string> Log)
        {
            Random rng = new Random();
            bool shouldBuff = rng.Next(2) == 1;//50% chance for a buff
            if (shouldBuff)
            {
                int buffTarget = rng.Next(2) + 1;//picks player
                float buffAmount = rng.Next(10, 31) / 100f;//picks buff %
                Log.Add($"Player {buffTarget} received a {buffAmount * 100}% damage buff this round!");
                return (true, buffTarget, buffAmount);
            }
            return (false, 0, 0);
        }
    }
}
