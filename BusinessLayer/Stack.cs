using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Stack
    {
        public int UserId { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();//list of cards in the stack
        private readonly object _stackLock = new();
        public async Task GetStack(User user, StreamWriter writer)
        {
            Console.WriteLine("** in get stack *");//debug
            int stackId = await CheckStack(user, writer);
            if (stackId < 0)
            {
                Console.WriteLine("** Failure at stack creation **");
                return;//stackcreation/search failure, handeld in checkStack
            }

            List<string> cardIds = await _stackMan.GetCardIdsFromStack(user);
            if (cardIds.Any())
            {
                List<Card> cards = await _cardMan.GetCardInfoByIds(cardIds);
                lock (_stackLock)
                {
                    this.Cards = cards;
                }

                Console.WriteLine("** Displaying cards **");
                await _response.HttpResponse(200, "Here are your cards:", writer);

                foreach (var card in this.Cards)
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
            else
            {
                Console.WriteLine("** No cards found for the user in the stack **");
                await _response.HttpResponse(200, "Here are your cards: ( no cards )", writer);
            }
        }

        public async Task<int> CheckStack(User user, StreamWriter writer)//checks if stack exists /creates one
        {
            Console.WriteLine("** Checking stack for user **");

            var stackId = await _stackMan.GetStackIdForUser(user);

            if (stackId > -1)
            { 
                Console.WriteLine($"User has the stack : {stackId}");
                return stackId.Value;
            }
            else if (stackId == -1)
            {
                Console.WriteLine("User does not have a stack yet");
                if (await _stackMan.CreateStackForUser(user))
                {
                    stackId = await _stackMan.GetStackIdForUser(user);
                    Console.WriteLine($"** Stack created successfully. Stack ID is {stackId} **");
                    return stackId.Value;
                }
                else
                {
                    Console.WriteLine("** Failed to create stack for the user **");
                    await _response.HttpResponse(500, "Stack Creation failed", writer);
                    return -1;
                }
            }
            else
            {
                await _response.HttpResponse(500, "Stack search failed", writer);
                return -1;
            }
        }
    }
}
