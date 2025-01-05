using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MonsterTradingCardGame.BusinessLayer.User;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Trade
    {
        public int TraderUserId { get; set; }//userid of trade creator
        public int UserId { get; set; }//userid of person wanting to trade
        public string Trader { get; set; }//name of trade creator
        public string User { get; set; }//name of person wanting to trade 
        public string? TradeId { get; set; }
        public Card? OfferedCard { get; set; }//card offered by trade creator
        public Card? RequestedCard { get; set; }//card given by person wanting to trade
        public string? RequestedCardType { get; set; }
        public float RequestedMinimumDamage { get; set; }
        public bool Status { get; set; }
        public List<Trade> Trades { get; set; } = new List<Trade>();


        private readonly Response _response = new();
        private readonly TradeManager _tradeManager = new();
        private readonly UserRepo _userMan = new();
        private readonly StackManager _stackManager = new();
        private readonly CardManager _cardManager = new();
        private readonly Parser _parser = new Parser();
        private readonly DeckManager _deckManager = new();


        public async Task GetTrades(StreamWriter writer)
        {
            Console.WriteLine("** Fetching all trades **");
            List<Trade> allTrades = await _tradeManager.GetAllTrades();

            if (allTrades == null || allTrades.Count == 0)
            {
                await _response.HttpResponse(200, "No trades available.", writer);
                writer.WriteLine("[]"); // Representing an empty list as JSON array
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

        public async Task NewTrade(string body, User user, StreamWriter writer)
        {
            Console.WriteLine("** Creating a new trade **");

            // Parse the trade data
            var trade = _parser.TradeDataParse(body, writer);
            if (trade == null)
            {
                await _response.HttpResponse(400, "Invalid trade data provided.", writer);
                return;
            }

            // ** Fetch the offered card's details from the database **
            var (offeredCardName, offeredCardDmg, traderName) = await _tradeManager.GetCardAndUserInfo(user.UserId, trade.OfferedCard.CardID);

            if (string.IsNullOrEmpty(offeredCardName))
            {
                await _response.HttpResponse(404, "Offered card not found.", writer);
                return;
            }

            // Set the fetched details into the trade object
            trade.OfferedCard.CardName = offeredCardName;
            trade.OfferedCard.CardDmg = offeredCardDmg;
            trade.Trader = traderName;
            trade.TraderUserId = user.UserId;

            // ** Check if the offered card is in the user's deck **
            bool isCardInDeck = await _deckManager.IsCardInDeck(user.UserId, trade.OfferedCard.CardID);
            if (isCardInDeck)
            {
                await _response.HttpResponse(400, "You cannot create a trade with a card currently in your deck.", writer);
                return;
            }

            // ** Print Trade Details as Requested **
            Console.WriteLine($"{traderName} adds {offeredCardName} ({offeredCardDmg}) in the store and wants '{trade.RequestedCardType}' with min {trade.RequestedMinimumDamage} damage.");

            // Add the trade to the database
            bool tradeCreated = await _tradeManager.CreateTrade(trade);
            if (tradeCreated)
            {
                await _response.HttpResponse(201, "Trade created successfully.", writer);
            }
            else
            {
                await _response.HttpResponse(500, "Failed to create trade.", writer);
            }
        }

        public async Task RemoveTrade(string path, User user, StreamWriter writer)
        {
            Console.WriteLine("** Attempting to delete trade **");

            // Extract the trade ID from the path
            string[] pathParts = path.Split('/');
            if (pathParts.Length < 3 || string.IsNullOrWhiteSpace(pathParts[2]))
            {
                await _response.HttpResponse(400, "Invalid trade ID provided.", writer);
                return;
            }

            string tradeId = pathParts[2].Trim(); // Trim whitespace to avoid issues
            Trade trade = new Trade { TradeId = tradeId };

            // Fetch the creator of the trade
            int? tradeCreatorId = await _tradeManager.GetTradeCreator(trade);
            if (tradeCreatorId == null)
            {
                await _response.HttpResponse(404, "Trade not found.", writer);
                return;
            }

            // Ensure the user is authorized to delete the trade (either the creator or an admin)
            if (tradeCreatorId != user.UserId && !await _userMan.CheckAdmin(user.UserId))
            {
                await _response.HttpResponse(403, "You do not have permission to delete this trade.", writer);
                return;
            }

            // Perform the deletion and check results
            trade.TraderUserId = tradeCreatorId.Value; // Ensure the UserId is set correctly
            bool tradeDeleted = await _tradeManager.DeleteTradeAsync(trade);
            if (tradeDeleted)
            {
                await _response.HttpResponse(200, "Trade deleted successfully.", writer);
            }
            else
            {
                await _response.HttpResponse(500, "Failed to delete the trade.", writer);
            }
        }

        public async Task TradeCard(string path, string body, User user, StreamWriter writer)
        {
            Console.WriteLine("** Attempting to trade a card **");

            // Extract and parse the trade ID using the parser
            string tradeId = _parser.ParseTradeId(path, writer);
            if (string.IsNullOrEmpty(tradeId))
            {
                await _response.HttpResponse(400, "Invalid trade ID provided.", writer);
                return;
            }

            // Fetch the existing trade from the database
            Trade existingTrade = await _tradeManager.GetTradeById(tradeId);

            if (existingTrade == null || !existingTrade.Status)
            {
                await _response.HttpResponse(404, "Trade not found or inactive.", writer);
                return;
            }

            if (existingTrade.TraderUserId == user.UserId)
            {
                await _response.HttpResponse(403, "You cannot trade with yourself.", writer);
                return;
            }

            // Proper parsing of the offered card ID using the parser
            string offeredCardId = _parser.ParseCardId(body, writer);
            if (string.IsNullOrEmpty(offeredCardId))
            {
                await _response.HttpResponse(400, "Invalid card provided for the trade.", writer);
                return;
            }

            // Check if the card is in the user's deck
            bool isCardInDeck = await _deckManager.IsCardInDeck(user.UserId, offeredCardId);
            if (isCardInDeck)
            {
                await _response.HttpResponse(400, "You cannot trade a card currently in your deck.", writer);
                return;
            }

            // Verify card ownership using the parser
            bool userOwnsCard = await _tradeManager.GetCard(new Trade
            {
                UserId = user.UserId,
                OfferedCard = new Card { CardID = offeredCardId }
            });

            if (!userOwnsCard)
            {
                await _response.HttpResponse(403, "You do not own this card.", writer);
                return;
            }

            // Fetch offered card details using the card manager
            var offeredCard = await _cardManager.GetCardInfoByIds(new List<string> { offeredCardId });
            if (offeredCard == null || offeredCard.Count == 0)
            {
                await _response.HttpResponse(404, "Card not found.", writer);
                return;
            }

            var offered = offeredCard[0];
            if (!string.Equals(offered.CardType, existingTrade.RequestedCardType, StringComparison.OrdinalIgnoreCase) || offered.CardDmg < existingTrade.RequestedMinimumDamage)
            {
                await _response.HttpResponse(400, "Card does not meet trade requirements.", writer);
                return;
            }

            // Execute the trade: swapping the cards
            await _stackManager.RemoveCardFromStack(user.UserId, offeredCardId);
            await _stackManager.RemoveCardFromStack(existingTrade.TraderUserId, existingTrade.OfferedCard.CardID);

            await _stackManager.InsertCardsIntoUserStack(user.UserId, new List<string> { existingTrade.OfferedCard.CardID });
            await _stackManager.InsertCardsIntoUserStack(existingTrade.TraderUserId, new List<string> { offeredCardId });

            // Mark the trade as inactive
            existingTrade.Status = false;
            await _tradeManager.UpdateTradeStatus(existingTrade);

            // **New: Delete the trade after a successful transaction**
            bool tradeDeleted = await _tradeManager.DeleteTradeById(tradeId);
            if (!tradeDeleted)
            {
                await _response.HttpResponse(500, "Failed to remove completed trade from the database.", writer);
                return;
            }

            await _response.HttpResponse(200, $"Trade successful! {user.UserName} traded with {existingTrade.Trader}.", writer);
        }



    }
}
