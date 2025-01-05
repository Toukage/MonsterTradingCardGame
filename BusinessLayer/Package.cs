using Microsoft.VisualBasic;
using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    internal class Package
    {
        private static readonly object _buyPackLock = new();

        private readonly Parser _parser = new();
        private readonly Tokens _token = new();
        private readonly Response _response = new();
        private readonly Stack _stack = new();
        private readonly PackageRepo _packMan = new();
        private readonly CardManager _cardMan = new ();
        private readonly UserRepo _userMan = new ();


        public async Task CreatePack(string body, string headers, StreamWriter writer)
        {
            Console.WriteLine("** in create pack **");//debug
            //Console.WriteLine($"** body for creating a pack {body}  **");//debug

            List<Card> cards = _parser.CardDataParse(body, writer);

            foreach (var card in cards)
            {
                if (string.IsNullOrEmpty(card.CardID) || string.IsNullOrEmpty(card.CardName) || string.IsNullOrEmpty(card.CardType) || string.IsNullOrEmpty(card.CardMonster) || string.IsNullOrEmpty(card.CardElement) || card.CardDmg <= 0)
                {
                    await _response.HttpResponse(400, "Invalid card data. All cards must have a name, type, element, and damage.", writer);
                    return;
                }
            }

            if (cards.Count == 5)
            {
                    
                for(int i = 0; i < 5; i++)
                {
                    await _cardMan.InsertCard(cards[i].CardID, cards[i].CardName, cards[i].CardType, cards[i].CardMonster, cards[i].CardElement, cards[i].CardDmg);
                }
                await _packMan.InsertPack(cards[0].CardID, cards[1].CardID, cards[2].CardID, cards[3].CardID, cards[4].CardID, writer);
                        
                return;
            }
            else
            {
                await _response.HttpResponse(400, "Invalid number of cards. Exactly 5 cards are required", writer);
                return;
            }
             
        }

        public async Task BuyPack(User user, StreamWriter writer) 
        {
            lock (_buyPackLock) // Prevent two users from buying the same pack simultaneously
            {
                Console.WriteLine("** in BuyPack **");
            }
            int packId = await _packMan.GetPack(writer);

            if (packId < 0)
            {
                Console.WriteLine("** No packs available **");
                return;//no pack left, is handeld in GetRandomPack
            }

            int? funds = await _userMan.GetCoins(user.UserId);

            if (!funds.HasValue || funds < 5)
            {
                Console.WriteLine("** User does not have enough funds to buy a pack **");
                await _response.HttpResponse(400, "Not enough money", writer);
                return;
            }

            int stackId = await _stack.CheckStack(user, writer);

            if (stackId < 0)
            {
                Console.WriteLine("** Failure at stack creation **");
                return;//stackcreation/search failure, handeld in checkStack
            }

            

            if (!await _packMan.PayPack(user.UserId))
            {
                Console.WriteLine("** Payment failed **");
                await _response.HttpResponse(402, "Payment failed", writer);
                return;
            }

            bool transfered = await _packMan.TransferPack(user.UserId, packId, writer);
            if (transfered)
            {
                await _packMan.DeletePack(packId);
                await _response.HttpResponse(201, "Successfully acquired pack", writer);
            }
            else
            {
                Console.WriteLine("** Transfer failed **");
                await _response.HttpResponse(500, "Pack transfer failed", writer);
            }

        }
    }
}
