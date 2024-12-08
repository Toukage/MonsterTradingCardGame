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
        private readonly Parser _parser = new();
        private readonly Tokens _token = new();
        private readonly Response _response = new();
        private readonly Stack _stack = new();
        private readonly PackageManager _packMan = new();
        private readonly CardManager _cardMan = new ();
        private readonly UserManager _userMan = new ();


        public void CreatePack(string body, string headers, StreamWriter writer)
        {
            Console.WriteLine("** in create pack **");//debug
            //Console.WriteLine($"** body for creating a pack {body}  **");//debug
            var (isValid, isAdmin, userID) = _token.CheckToken(headers, writer);
            Console.WriteLine($"** after CheckToken -> isValid : {isValid}, isAdmin : {isAdmin}, userID : {userID}  **");//debug
            if (isAdmin)
            {
                Console.WriteLine($"** in isAdmin if condition **");//debug
                List<Card> cards = _parser.CardDataParse(body, writer);

                foreach (var card in cards)
                {
                    if (string.IsNullOrEmpty(card.CardID) || string.IsNullOrEmpty(card.CardName) || string.IsNullOrEmpty(card.CardType) || string.IsNullOrEmpty(card.CardMonster) || string.IsNullOrEmpty(card.CardElement) || card.CardDmg <= 0)
                    {
                        _response.HttpResponse(400, "Invalid card data. All cards must have a name, type, element, and damage.", writer);
                        return;
                    }
                }

                if (cards.Count == 5)
                {
                    
                    for(int i = 0; i < 5; i++)
                    {
                        _cardMan.InsertCard(cards[i].CardID, cards[i].CardName, cards[i].CardType, cards[i].CardMonster, cards[i].CardElement, cards[i].CardDmg);
                    }
                    _packMan.InsertPack(cards[0].CardID, cards[1].CardID, cards[2].CardID, cards[3].CardID, cards[4].CardID, writer);
                        
                    return;
                }
                else
                {
                    _response.HttpResponse(400, "Invalid number of cards. Exactly 5 cards are required", writer);
                    return;
                }
            } 
        }

        public void BuyPack(string headers, StreamWriter writer) 
        {
            Console.WriteLine("** in create pack **");//debug
            var (isValid, isAdmin, userId) = _token.CheckToken(headers, writer);
            if (!isValid)
            {
                _response.HttpResponse(401, "Invalid token", writer);
                return;
            }

            int packId = _packMan.GetRandomPack(writer);

            if (packId < 0)
            {
                Console.WriteLine("** No packs available **");
                return;//no pack left, is handeld in GetRandomPack
            }

            int? funds = _userMan.GetCoins(userId!.Value);

            if (!funds.HasValue || funds < 5)
            {
                Console.WriteLine("** User does not have enough funds to buy a pack **");
                _response.HttpResponse(400, "Not enough money", writer);
                return;
            }

            int stackId = _stack.CheckStack(userId, writer);

            if (stackId < 0)
            {
                Console.WriteLine("** Failure at stack creation **");
                return;//stackcreation/search failure, handeld in checkStack
            }

            

            if (!_packMan.PayPack(userId))
            {
                Console.WriteLine("** Payment failed **");
                _response.HttpResponse(402, "Payment failed", writer);
                return;
            }

            bool transfered = _packMan.TransferPack(userId.Value, packId, writer);
            if (transfered)
            {
                _packMan.DeletePack(packId);
                _response.HttpResponse(201, "Successfully acquired pack", writer);
            }
            else
            {
                Console.WriteLine("** Transfer failed **");
                _response.HttpResponse(500, "Pack transfer failed", writer);
            }

        }
    }
}
