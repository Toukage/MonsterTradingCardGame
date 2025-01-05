﻿using System;
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
        private static readonly Random _random = new Random();
        private readonly DeckManager _deckMan = new();
        private readonly StackManager _stackMan = new();

        [JsonProperty("Id")]
        public string CardID { get; set; }
        [JsonProperty("Name")]
        public string CardName { get; set; }
        [JsonProperty("Damage")]
        public float CardDmg { get; set; }
        
        public string CardType { get; set; }
        public string CardElement { get; set; }
        public string CardMonster { get; set; }
        private static readonly object _deckLock = new();
        public Card RandomCard(Deck deckcopy)
        {
            Console.WriteLine("inside random card picker");

            if (deckcopy.DeckCards == null || deckcopy.DeckCards.Count == 0)
            {
                throw new InvalidOperationException($"Player {deckcopy.UserId}'s deck is empty or invalid.");
            }
            Card randomCard = deckcopy.DeckCards[_random.Next(deckcopy.DeckCards.Count)];
            return randomCard;
        }

        public bool MonsterFight(Card card1, Card card2)
        {
            if (card1.CardType == "Monster" && card2.CardType == "Monster")
            {
                Console.WriteLine("+++++ pure monster fight +++++");
                return true;
            }
            Console.WriteLine("+++++ element effects apply +++++");
            return false;
        }

        public int SpecialRules(Card card1, Card card2, List<string> battleLog)
        {
            // Check both ways for special interactions
            if (card1.CardMonster == "Goblin" && card2.CardMonster == "Dragon") { battleLog.Add($"{card1.CardMonster} is too afraid to attack {card2.CardMonster}."); return -1; }
            if (card1.CardMonster == "Dragon" && card2.CardMonster == "Goblin") { battleLog.Add($"{card2.CardMonster} is too afraid to attack {card1.CardMonster}."); return 1; }

            if (card1.CardMonster == "Ork" && card2.CardMonster == "Wizzard") { battleLog.Add($"{card2.CardMonster} controls {card1.CardMonster}, so {card1.CardMonster} is not able to damage {card2.CardMonster}."); return -1; }
            if (card1.CardMonster == "Wizzard" && card2.CardMonster == "Ork") { battleLog.Add($"{card1.CardMonster} controls {card2.CardMonster}, so {card2.CardMonster} is not able to damage {card1.CardMonster}."); return 1; } 

            if (card1.CardMonster == "Knight" && card2.CardName == "WaterSpell") { battleLog.Add($"{card1.CardMonster} armor is so heavy that {card2.CardName} drowned it."); ; return -1; }; 
            if (card1.CardName == "WaterSpell" && card2.CardMonster == "Knight") { battleLog.Add($"{card2.CardMonster} armor is so heavy that {card1.CardName} drowned it."); return 1; } 

            if (card1.CardType == "Spell" && card2.CardMonster == "Kraken") { battleLog.Add($"{card2.CardMonster} is immune to {card1.CardType}."); return -1; } 
            if (card1.CardMonster == "Kraken" && card2.CardType == "Spell") { battleLog.Add($"{card1.CardMonster} is immune to {card2.CardType}."); return 1; } 

            if (card1.CardMonster == "Dragon" && card2.CardName == "FireElve") { battleLog.Add($"The {card2.CardName} know {card1.CardMonster} since they were little and can evade their attacks."); return -1; } 
            if (card1.CardName == "FireElve" && card2.CardMonster == "Dragon") { battleLog.Add($"The {card1.CardName} know {card2.CardMonster} since they were little and can evade their attacks."); return 1; } 

            Console.WriteLine("+++++  No special interactions  +++++");
            return 0; // No special interactions
        }

        public int ElementEffect(Card card1, Card card2, List<string> battleLog)
        {
            // Elemental effectiveness applies to all cards, not just spells
            if (card1.CardElement == card2.CardElement)
            {
                battleLog.Add("No elemental advantage. Same elements.");
                return 0; // No effect if elements are the same
            }

            // Water beats Fire
            if (card1.CardElement == "Water" && card2.CardElement == "Fire") { battleLog.Add($"{card1.CardElement} is effective against {card2.CardElement}."); return 1; }
            if (card2.CardElement == "Water" && card1.CardElement == "Fire") { battleLog.Add($"{card2.CardElement} is effective against {card1.CardElement}."); return -1; }

            // Fire beats Normal
            if (card1.CardElement == "Fire" && card2.CardElement == "Normal") { battleLog.Add($"{card1.CardElement} is effective against {card2.CardElement}."); return 1; }
            if (card2.CardElement == "Fire" && card1.CardElement == "Normal") { battleLog.Add($"{card2.CardElement} is effective against {card1.CardElement}."); return -1; }

            // Normal beats Water
            if (card1.CardElement == "Normal" && card2.CardElement == "Water") { battleLog.Add($"{card1.CardElement} is effective against {card2.CardElement}."); return 1; }
            if (card2.CardElement == "Normal" && card1.CardElement == "Water") { battleLog.Add($"{card2.CardElement} is effective against {card1.CardElement}."); return -1; }

            // If none of the conditions matched, return neutral effect
            
            return 0;
        }

        public async Task FinalizeDeck(Deck originalDeck, Deck battleDeck, int userId, List<string> battleLog)
        {
            // Create empty lists for lost and won cards
            List<Card> cardsLost = new List<Card>();
            List<Card> cardsWon = new List<Card>();

            // Identify lost cards (cards present in originalDeck but missing in battleDeck)
            foreach (var card in originalDeck.DeckCards)
            {
                if (!battleDeck.DeckCards.Any(bCard => bCard.CardID == card.CardID))
                {
                    cardsLost.Add(card);
                }
            }

            // Identify won cards (cards present in battleDeck but missing in originalDeck)
            foreach (var card in battleDeck.DeckCards)
            {
                if (!originalDeck.DeckCards.Any(oCard => oCard.CardID == card.CardID))
                {
                    cardsWon.Add(card);
                }
            }

            // Debugging Output for Lost Cards
            lock (_deckLock)
            {
                battleLog.Add($"Cards Player {userId} lost during battle:");
            }
            foreach (var card in cardsLost)
            {
                lock (_deckLock)
                {
                    battleLog.Add($"- {card.CardName} | ID: {card.CardID}");
                }
                await _deckMan.RemoveCardFromDeck(userId, card.CardID);
            }

            // Debugging Output for Won Cards
            lock (_deckLock)
            {
                battleLog.Add($"Cards Player {userId} won during battle:");
            }
            foreach (var card in cardsWon)
            {
                lock (_deckLock)
                {
                    battleLog.Add($"- {card.CardName} | ID: {card.CardID}");
                }
                await _stackMan.InsertCardsIntoUserStack(userId, new List<string> { card.CardID });
            }
        }
    }
}
