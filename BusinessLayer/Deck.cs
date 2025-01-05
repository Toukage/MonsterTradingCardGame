using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Deck
    {
        private readonly DeckRepo _deckMan = new();
        private readonly Response _response = new();
        private readonly CardRepo _cardMan = new();
        private readonly Tokens _token = new();
        private readonly StackRepo _stackMan = new();
        private readonly Parser _parser = new();

        public int UserId { get; set; }
        public List<Card> DeckCards { get; set; } = new List<Card>();

        //-----------DECK--VIEWING-----------
        public async Task GetDeck(User user, StreamWriter writer)
        {
            Console.WriteLine("** in get deck *");//debug

            var userDeck = new Deck { UserId = user.UserId };

            await _deckMan.GetDeckCards(user.UserId, userDeck);//gets cardids from deck

            if (userDeck.DeckCards == null || userDeck.DeckCards.Count == 0)
            {
                await _response.HttpResponse(200, "Your deck is empty.", writer);
                return;
            }

            Console.WriteLine("Card IDs fetched from deck: " + string.Join(", ", userDeck.DeckCards.Select(c => c.CardID)));
            await _response.HttpResponse(200, "Your Deck:", writer);


            foreach (var card in userDeck.DeckCards)
            {
                writer.WriteLine($"Card ID: {card.CardID}");
                writer.WriteLine($"Name: {card.CardName}");
                writer.WriteLine($"Type: {card.CardType}");
                if (!string.IsNullOrEmpty(card.CardMonster))
                    writer.WriteLine($"Monster: {card.CardMonster}");
                writer.WriteLine($"Element: {card.CardElement}");
                writer.WriteLine($"Damage: {card.CardDmg}");
                writer.WriteLine();
            }
        }

        //-----------EDITING--DECK-----------

        private static readonly object _deckLock = new();//lock object for deck editing
        public async Task UpdateDeck(string body, User user, StreamWriter writer)//updating deck with new cards 
        {
            Console.WriteLine("** Updating deck **");
            lock (_deckLock)//lock so only one thread can edit the deck at a time
            {
                Console.WriteLine("** Checking for existing deck **");//debug
            }

            List<string> cardIds = _parser.CardIdParse(body, writer);

            if (cardIds.Count != 4)
            {
                Console.WriteLine("** Incorrect number of card IDs provided **");//debug
                await _response.HttpResponse(400, "You must provide exactly 4 card IDs.", writer);
                return;
            }

            if (!await _deckMan.VerifyCards(user.UserId, cardIds))//checks if the user owns all the cards
            {
                Console.WriteLine("** User does not own all provided cards **");//debug
                await _response.HttpResponse(403, "You do not own all the provided cards.", writer);
                return;
            }

            if (await _deckMan.UpdateDeckCards(user.UserId, cardIds))//updates the deck with the new cards
            {
                Console.WriteLine("** Deck updated successfully **");//debug
                await _response.HttpResponse(200, "Deck updated successfully.", writer);
            }
            else
            {
                Console.WriteLine("** Failed to update deck **");//debug
                await _response.HttpResponse(500, "Failed to update deck.", writer);
            }

        }

        public async Task FinalizeDeck(Deck originalDeck, Deck battleDeck, int userId, List<string> battleLog)//removes lost cards from the deck and adds won cards to the stack
        {
            //lists for lost and won cards
            List<Card> cardsLost = new List<Card>();
            List<Card> cardsWon = new List<Card>();

            //checks which cards are missing from the original deck
            foreach (var card in originalDeck.DeckCards)
            {
                if (!battleDeck.DeckCards.Any(bCard => bCard.CardID == card.CardID))
                {
                    cardsLost.Add(card);
                }
            }

            //checks which cards are new / not from the original deck
            foreach (var card in battleDeck.DeckCards)
            {
                if (!originalDeck.DeckCards.Any(oCard => oCard.CardID == card.CardID))
                {
                    cardsWon.Add(card);
                }
            }

            lock (_deckLock)
            {
                battleLog.Add($"Cards Player {userId} lost during battle:");
            }
            foreach (var card in cardsLost)
            {
                lock (_deckLock)
                {
                    battleLog.Add($"- {card.CardName} | ID: {card.CardID}");
                }
                await _deckMan.RemoveCardFromDeck(userId, card.CardID);
            }

            lock (_deckLock)
            {
                battleLog.Add($"Cards Player {userId} won during battle:");
            }
            foreach (var card in cardsWon)
            {
                lock (_deckLock)
                {
                    battleLog.Add($"- {card.CardName} | ID: {card.CardID}");
                }
                await _stackMan.InsertCardsIntoUserStack(userId, new List<string> { card.CardID });
            }
        }
    }
}
