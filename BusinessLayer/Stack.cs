using MonsterTradingCardGame.DataLayer;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Stack
    {
        public int UserId { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();//list of cards in the stack
        private readonly object _stackLock = new();
        private readonly StackRepo _stackMan = new();
        private readonly CardRepo _cardMan = new();
        private readonly Response _response = new();
        public async Task GetStack(User user, StreamWriter writer)
        {
            Console.WriteLine("** in get stack *");//debug
            var stackId = await _stackMan.GetStackIdForUser(user);
            if (stackId < 0)
            {
                Console.WriteLine("** Failure at stack creation **");//debug
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

                Console.WriteLine("** Displaying cards **");//debug
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
                Console.WriteLine("** No cards found for the user in the stack **");//debug
                await _response.HttpResponse(200, "Here are your cards: ( no cards )", writer);
            }
        }
    }
}
