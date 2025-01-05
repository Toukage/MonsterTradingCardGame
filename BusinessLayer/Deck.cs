using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Deck
    {
        private readonly DeckManager _deckMan = new();
        private readonly Response _response = new();
        private readonly CardManager _cardMan = new();
        private readonly Tokens _token = new();
        private readonly StackManager _stackMan = new();
        private readonly Parser _parser = new();

        public int UserId { get; set; }
        public List<Card> DeckCards { get; set; } = new List<Card>();
       
        public async Task GetDeck(User user, StreamWriter writer)
        {
            Console.WriteLine("** in get deck *");//debug

            if (!await CheckDeck(user, writer))
            {
                return; // Deck doesn't exist or creation failed
            }

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

        public async Task <bool> CheckDeck(User user, StreamWriter writer)//checks if deck exists /creates one
        {
            Console.WriteLine("** Checking deck for user. **");

            bool deckExists = await _deckMan.GetDeckForUser(user.UserId);// X

            if (deckExists)// found deck
            {
                Console.WriteLine($"** User has a deck. **");
                return true;
            }

            Console.WriteLine("** User does not have a Deck yet. **");

            bool deckCreated = await _deckMan.CreateDeckForUser(user.UserId);
            if (deckCreated)
            {
                Console.WriteLine($"** Deck created successfully. **");
                return true;
            }
            await _response.HttpResponse(500, "Deck search failed", writer);
                return false;
        }
        private static readonly object _deckLock = new();
        public async Task UpdateDeck(string body, User user, StreamWriter writer) 
        {
            Console.WriteLine("** Updating deck **");
            lock (_deckLock) // Locking to avoid concurrent modifications
            {
                Console.WriteLine("** Checking for existing deck **");
            }

            await CheckDeck(user, writer);

            List<string> cardIds = _parser.CardIdParse(body, writer); // MUST MAKE PARSING METHOD

            if (cardIds.Count != 4)
            {
                Console.WriteLine("** Incorrect number of card IDs provided **");
                await _response.HttpResponse(400, "You must provide exactly 4 card IDs.", writer);
                return;
            }

            // Verify that the user owns the cards
            if (!await _deckMan.VerifyCards(user.UserId, cardIds))
            {
                Console.WriteLine("** User does not own all provided cards **");
                await _response.HttpResponse(403, "You do not own all the provided cards.", writer);
                return;
            }

            // Update the deck with the new card IDs
            if (await _deckMan.UpdateDeckCards(user.UserId, cardIds))
            {
                Console.WriteLine("** Deck updated successfully **");
                await _response.HttpResponse(200, "Deck updated successfully.", writer);
            }
            else
            {
                Console.WriteLine("** Failed to update deck **");
                await _response.HttpResponse(500, "Failed to update deck.", writer);
            }

        }
    }
}
