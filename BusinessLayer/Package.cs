using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;


namespace MonsterTradingCardGame.BusinessLayer
{
    internal class Package
    {
        private static readonly object _buyPackLock = new();

        private readonly Parser _parser = new();
        private readonly Tokens _token = new();
        private readonly Response _response = new();
        private readonly Stack _stack = new();
        private readonly StackRepo _stackMan = new();
        private readonly PackageRepo _packMan = new();
        private readonly CardRepo _cardMan = new ();
        private readonly UserRepo _userMan = new ();

        //-----------CREATE--PACK-----------
        public async Task CreatePack(string body, string headers, StreamWriter writer)
        {
            Console.WriteLine("** in create pack **");//debug

            List<Card> cards = _parser.CardDataParse(body, writer);

            foreach (var card in cards)//checks if all the cards have the required data
            {
                if (string.IsNullOrEmpty(card.CardID) || string.IsNullOrEmpty(card.CardName) || string.IsNullOrEmpty(card.CardType) || string.IsNullOrEmpty(card.CardMonster) || string.IsNullOrEmpty(card.CardElement) || card.CardDmg <= 0)
                {
                    await _response.HttpResponse(400, "Invalid card data. All cards must have a name, type, element, and damage.", writer);
                    return;
                }
            }

            if (cards.Count == 5)//checks if the pack has exactly 5 cards
            {
                    
                for(int i = 0; i < 5; i++)
                {
                    await _cardMan.InsertCard(cards[i].CardID, cards[i].CardName, cards[i].CardType, cards[i].CardMonster, cards[i].CardElement, cards[i].CardDmg);//inserts the cards into the database
                }
                await _packMan.InsertPack(cards[0].CardID, cards[1].CardID, cards[2].CardID, cards[3].CardID, cards[4].CardID, writer);//inserts the pack into the database

                return;
            }
            else
            {
                await _response.HttpResponse(400, "Invalid number of cards. Exactly 5 cards are required", writer);
                return;
            }
             
        }

        //-----------BUY--PACK-----------
        public async Task BuyPack(User user, StreamWriter writer) 
        {
            lock (_buyPackLock)//stops two users from buying the same pack simultaneously
            {
                Console.WriteLine("** in BuyPack **");
            }
            int packId = await _packMan.GetPack(writer);

            if (packId < 0)
            {
                Console.WriteLine("** No packs available **");
                return;//no pack left, is handeld GetPack 
            }

            int? funds = await _userMan.GetCoins(user.UserId);//gets the funds of the user

            if (!funds.HasValue || funds < 5)
            {
                Console.WriteLine("** User does not have enough funds to buy a pack **");
                await _response.HttpResponse(400, "Not enough money", writer);
                return;
            }

            var stackId = await _stackMan.GetStackIdForUser(user);//gets the stack id for the user

            if (stackId < 0)
            {
                Console.WriteLine("** Failure at stack search **");
                return;
            }

            if (!await _packMan.PayPack(user.UserId))//pays for the pack
            {
                Console.WriteLine("** Payment failed **");
                await _response.HttpResponse(402, "Payment failed", writer);
                return;
            }

            bool transfered = await _packMan.TransferPack(user.UserId, packId, writer);//transfers the pack to the user
            if (transfered)
            {
                await _packMan.DeletePack(packId);//deletes pack after its purchased
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
