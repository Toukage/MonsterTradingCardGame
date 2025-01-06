using MonsterTradingCardGame.BusinessLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.Database
{
    public interface IDatabaseManager
    {
        //User and Token
        Task<int?> GetUserIdByUsername(string username);
        Task<string> GetUsernameById(int userId);
        Task<bool> InsertUser(string username, string password);
        Task<string> GetToken(int userId);
        Task<bool> ValidateToken(string token);
        Task<bool> UpdateUserProfile(int userId, string username, string bio, string image);

        //Trading
        Task<Trade?> GetTradeById(string tradeId);


        //Card and Deck
        Task<bool> UserOwnsCard(int userId, string cardId);
        Task<bool> UpdateDeck(int userId, List<string> cardIds);

        //Package
        Task<bool> InsertPack(string packId, List<Card> cards);
    }
}
