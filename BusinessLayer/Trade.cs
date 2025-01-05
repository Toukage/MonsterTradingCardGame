using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Trade
    {
        private readonly Response _response = new();
        private readonly TradeRepo _tradeManager = new();
        private readonly UserRepo _userMan = new();
        private readonly StackRepo _stackManager = new();
        private readonly CardRepo _cardManager = new();
        private readonly Parser _parser = new Parser();
        private readonly DeckRepo _deckManager = new();

        public int TraderUserId { get; set; }//userid of trade creator
        public int UserId { get; set; }//userid of person wanting to trade
        public string Trader { get; set; }//name of trade creator
        public string User { get; set; }//name of person wanting to trade 
        public string? TradeId { get; set; }
        public Card? OfferedCard { get; set; }//card offered by trade creator
        public Card? RequestedCard { get; set; }//card given by person wanting to trade
        public string? RequestedCardType { get; set; }
        public float RequestedMinimumDamage { get; set; }
        public List<Trade> Trades { get; set; } = new List<Trade>();

        //----------------------GET--ALL--TRADES----------------------
        public async Task GetTrades(StreamWriter writer)
        {
            Console.WriteLine("** Fetching all trades **");
            List<Trade> allTrades = await _tradeManager.GetAllTrades();

            if (allTrades == null || allTrades.Count == 0)
            {
                await _response.HttpResponse(200, "No trades available.", writer);
                writer.WriteLine("[]");//Representing an empty list as JSON array
                return;
            }
            await _response.HttpResponse(200, "Trades:", writer);

            foreach (var trade in allTrades)
            {
                writer.WriteLine($"Trade ID: {trade.TradeId}");
                writer.WriteLine($"Trade Created by: {trade.Trader}");
                writer.WriteLine($"Offered Card ID: {trade.OfferedCard.CardID}");
                writer.WriteLine($"Requested Card ID: {trade.OfferedCard.CardName}");
                writer.WriteLine($"Requested Card Type: {trade.RequestedCardType}");
                writer.WriteLine($"Requested Minimum Damage: {trade.RequestedMinimumDamage}");
                writer.WriteLine("");
                writer.WriteLine("----");
                writer.WriteLine("");
            }
        }

        //----------------------NEW--TRADE----------------------
        public async Task NewTrade(string body, User user, StreamWriter writer)
        {
            Console.WriteLine("** Creating a new trade **");

            var trade = _parser.TradeDataParse(body, writer);//parsing trade data
            if (trade == null)
            {
                await _response.HttpResponse(400, "Invalid trade data provided.", writer);
                return;
            }

            var (offeredCardName, offeredCardDmg, traderName) = await _tradeManager.GetCardAndUserInfo(user.UserId, trade.OfferedCard.CardID);//gets card and user info

            if (string.IsNullOrEmpty(offeredCardName))
            {
                await _response.HttpResponse(404, "Offered card not found.", writer);
                return;
            }

            //populates trade object
            trade.OfferedCard.CardName = offeredCardName;
            trade.OfferedCard.CardDmg = offeredCardDmg;
            trade.Trader = traderName;
            trade.TraderUserId = user.UserId;

            
            bool isCardInDeck = await _deckManager.IsCardInDeck(user.UserId, trade.OfferedCard.CardID);//checking if card is in deck
            if (isCardInDeck)
            {
                await _response.HttpResponse(400, "You cannot create a trade with a card currently in your deck.", writer);
                return;
            }

            Console.WriteLine($"{traderName} adds {offeredCardName} ({offeredCardDmg}) in the store and wants '{trade.RequestedCardType}' with min {trade.RequestedMinimumDamage} damage.");

            
            bool tradeCreated = await _tradeManager.CreateTrade(trade);//Adds the trade to the database
            if (tradeCreated)
            {
                await _response.HttpResponse(201, "Trade created successfully.", writer);
            }
            else
            {
                await _response.HttpResponse(500, "Failed to create trade.", writer);
            }
        }

        //----------------------REMOVE--TRADE----------------------
        public async Task RemoveTrade(string path, User user, StreamWriter writer)
        {
            Console.WriteLine("** Attempting to delete trade **");//debug

            string[] pathParts = path.Split('/');
            if (pathParts.Length < 3 || string.IsNullOrWhiteSpace(pathParts[2]))//check if trade id is provided in path
            {
                await _response.HttpResponse(400, "Invalid trade ID provided.", writer);
                return;
            }

            string tradeId = pathParts[2].Trim();
            Trade trade = new Trade { TradeId = tradeId };

            int? tradeCreatorId = await _tradeManager.GetTradeCreator(trade);//gets trade creator
            if (tradeCreatorId == null)
            {
                await _response.HttpResponse(404, "Trade not found.", writer);
                return;
            }

            if (tradeCreatorId != user.UserId && !await _userMan.CheckAdmin(user.UserId))//check if user is trade creator or admin
            {
                await _response.HttpResponse(403, "You do not have permission to delete this trade.", writer);
                return;
            }

            trade.TraderUserId = tradeCreatorId.Value;
            bool tradeDeleted = await _tradeManager.DeleteTradeById(trade.TradeId);//deletes trade
            if (tradeDeleted)
            {
                await _response.HttpResponse(200, "Trade deleted successfully.", writer);
            }
            else
            {
                await _response.HttpResponse(500, "Failed to delete the trade.", writer);
            }
        }

        //----------------------TRADE----------------------
        public async Task TradeCard(string path, string body, User user, StreamWriter writer)
        {
            Console.WriteLine("** Attempting to trade a card **");

            string tradeId = _parser.ParseTradeId(path, writer);//gets trade id from path
            if (string.IsNullOrEmpty(tradeId))
            {
                await _response.HttpResponse(400, "Invalid trade ID provided.", writer);
                return;
            }

            Trade existingTrade = await _tradeManager.GetTradeById(tradeId);//fetching trade by id

            if (existingTrade == null)
            {
                await _response.HttpResponse(404, "Trade not found or inactive.", writer);
                return;
            }

            if (existingTrade.TraderUserId == user.UserId)//check if user is trying to trade with themselves
            {
                await _response.HttpResponse(403, "You cannot trade with yourself.", writer);
                return;
            }

            string offeredCardId = _parser.ParseCardId(body, writer);//parsing the card from body
            if (string.IsNullOrEmpty(offeredCardId))
            {
                await _response.HttpResponse(400, "Invalid card provided for the trade.", writer);
                return;
            }

            bool isCardInDeck = await _deckManager.IsCardInDeck(user.UserId, offeredCardId);//check if card is in deck
            if (isCardInDeck)
            {
                await _response.HttpResponse(400, "You cannot trade a card currently in your deck.", writer);
                return;
            }

            bool userOwnsCard = await _tradeManager.GetCard(new Trade
            {
                UserId = user.UserId,
                OfferedCard = new Card { CardID = offeredCardId }
            });//check if user owns the card

            if (!userOwnsCard)
            {
                await _response.HttpResponse(403, "You do not own this card.", writer);
                return;
            }
            var offeredCard = await _cardManager.GetCardInfoByIds(new List<string> { offeredCardId });
            if (offeredCard == null || offeredCard.Count == 0)//check if card exists
            {
                await _response.HttpResponse(404, "Card not found.", writer);
                return;
            }

            var offered = offeredCard[0];
            if (!string.Equals(offered.CardType, existingTrade.RequestedCardType, StringComparison.OrdinalIgnoreCase) || offered.CardDmg < existingTrade.RequestedMinimumDamage)//check if card meets trade requirements
            {
                await _response.HttpResponse(400, "Card does not meet trade requirements.", writer);
                return;
            }

            //swapping the cards
            await _stackManager.RemoveCardFromStack(user.UserId, offeredCardId);
            await _stackManager.RemoveCardFromStack(existingTrade.TraderUserId, existingTrade.OfferedCard.CardID);

            await _stackManager.InsertCardsIntoUserStack(user.UserId, new List<string> { existingTrade.OfferedCard.CardID });
            await _stackManager.InsertCardsIntoUserStack(existingTrade.TraderUserId, new List<string> { offeredCardId });

            bool tradeDeleted = await _tradeManager.DeleteTradeById(tradeId);//delete trade after its done
            if (!tradeDeleted)
            {
                await _response.HttpResponse(500, "Failed to remove completed trade from the database.", writer);
                return;
            }

            await _response.HttpResponse(200, $"Trade successful! {user.UserName} traded with {existingTrade.Trader}.", writer);
        }
    }
}
