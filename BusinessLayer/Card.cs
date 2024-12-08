using Microsoft.VisualBasic;
using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class Card
    {
        private readonly Response _response = new();
        private readonly CardManager _cards = new();
        private readonly Tokens _token = new();
        [JsonProperty("Id")]
        public string CardID { get; set; }
        [JsonProperty("Name")]
        public string CardName { get; set; }
        [JsonProperty("Damage")]
        public float CardDmg { get; set; }
        
        public string CardType { get; set; }
        public string CardElement { get; set; }
        public string CardMonster { get; set; }
    }
}
