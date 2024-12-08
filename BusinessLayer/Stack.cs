using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonsterTradingCardGame.DataLayer;
using Newtonsoft.Json.Linq;
using Npgsql.Internal;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Stack
    {
        private readonly StackManager _stackMan = new();
        private readonly Response _response = new();
        private readonly CardManager _cardMan = new();
        private readonly Tokens _token = new();
        public int StackId { get; set; }
        public int UserId { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();//list of cards in the stack

        public void GetStack(string headers, StreamWriter writer)
        {
            Console.WriteLine("** in get stack *");//debug
            var (isValid, isAdmin, userId) = _token.CheckToken(headers, writer);
            if (!isValid)
            {
                return;
            }
            int stackId = CheckStack(userId, writer);
            if (stackId < 0)
            {
                Console.WriteLine("** Failure at stack creation **");
                return;//stackcreation/search failure, handeld in checkStack
            }

            List<string> cardIds = _stackMan.GetCardIdsFromStack(userId);
            if (cardIds.Any())
            {
                List<Card> cards = _cardMan.GetCardInfoByIds(cardIds);
                this.Cards = cards;

                Console.WriteLine("** Displaying cards **");
                _response.HttpResponse(200, "Here are your cards:", writer);

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
                _response.HttpResponse(200, "Here are your cards: ( no cards )", writer);
            }
        }

        public int CheckStack(int? userId, StreamWriter writer)//checks if stack exists /creates one
        {
            Console.WriteLine("** Checking stack for user **");

            int? stackId = _stackMan.GetStackIdForUser(userId);

            if (stackId > -1)
            { 
                Console.WriteLine($"User has the stack : {stackId.Value}");
                return stackId.Value;
            }
            else if (stackId == -1)
            {
                Console.WriteLine("User does not have a stack yet");
                if (_stackMan.CreateStackForUser(userId).HasValue)
                {
                    stackId = _stackMan.GetStackIdForUser(userId);
                    Console.WriteLine($"** Stack created successfully. Stack ID is {stackId!.Value} **");
                    return stackId.Value;
                }
                else
                {
                    Console.WriteLine("** Failed to create stack for the user **");
                    _response.HttpResponse(500, "Stack Creation failed", writer);
                    return -1;
                }
            }
            else
            {
                _response.HttpResponse(500, "Stack search failed", writer);
                return -1;
            }
        }
    }
}
