using MonsterTradingCardGame.BusinessLayer;
using MonsterTradingCardGame.DataLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;

namespace MonsterTradingCardGame.Routing
{
    public class Parser
    {
        private readonly Router _router;
        public Parser(Router router)
        {
            _router = router;
        }

        public Parser()
        {
        }

        public (string method, string path, string headers, string body) RequestParse(string requestString, StreamWriter writer)
        {
            try
            {
                string[] requestLines = requestString.Split(new[] { "\r\n" }, StringSplitOptions.None);//system enviroment line ending.

                if (requestLines.Length < 1)
                {
                    Console.WriteLine("Invalid request received, closing connection.");
                    _response.HttpResponse(400, "Invalid request format", writer);
                    return (null, null, null, null);
                }

                //--------Request-Line Handling--------
                string[] requestLineParts = requestLines[0].Split(' ');

                if (requestLineParts.Length < 2)
                {
                    _response.HttpResponse(400, "Invalid request line", writer);
                    Console.WriteLine("Malformed request line ");
                    return (null, null, null, null);
                }

                string method = requestLineParts[0];//extract method
                string path = requestLineParts[1];//Extract path
                int queryIndex = path.IndexOf('?');
                if (queryIndex != -1)
                {
                    path = path.Substring(0, queryIndex); // Keep only the base path
                }

                Console.WriteLine($"----> Parsed Request: Method={method}, Path={path} <----");//debug

                //--------Header Handling--------
                var headers = new Dictionary<string, string>();
                string body = string.Empty;
                bool inHeaders = true;
                for (int i = 1; i < requestLines.Length; i++)
                {
                    string line = requestLines[i].Trim();

                    if (string.IsNullOrEmpty(line))
                    {
                        inHeaders = false;
                        continue;
                    }

                    if (inHeaders)
                    {
                        int colonIndex = line.IndexOf(':');
                        if (colonIndex > 0)
                        {
                            //Console.WriteLine("** inside colconIndex > 0 **");
                            string headerName = line.Substring(0, colonIndex).Trim(); 
                            string headerValue = line.Substring(colonIndex + 1).Trim();
                            headers[headerName] = headerValue;
                            //Console.WriteLine($"Header: {headerName} = {headerValue}");//debug
                        }
                    }
                    else
                    {
                        body = line.Trim(); // Capture the body after headers
                        //Console.WriteLine($"Body: {body}"); // Debug
                    }
                }

            Console.WriteLine($"Received headers: {string.Join(", ", headers)}");//STRING BUILDER

                //--------Routing to Path--------
                string headersString = string.Join("\r\n", headers.Select(h => $"{h.Key}: {h.Value}"));//Convert headers back to string
                return(method, path, headersString, body);
            }
            catch (Exception ex)
            {
                _response.HttpResponse(500, $"Error parsing request: {ex.Message}", writer);
                return (null, null, null, null);
            }
        }

        //----------------------BODY--PARSERS----------------------
        public (string username, string password) UserDataParse(string body, StreamWriter writer)//parser for getting username/password from the request body
        {
            dynamic credentials;
            try
            {
                credentials = JsonConvert.DeserializeObject<dynamic>(body);//deserializes the json string into dynamic c# obj so we can access the object easier
                string username = credentials.Username;//converts dynamic obj into string for username / password string 
                string password = credentials.Password;
                return (username, password);//sends back parsed data
            }
            catch (Exception ex)
            {
                _response.HttpResponse(400, $"Invalid JSON format: {ex.Message}", writer);
                return (null, null);
            }
        }

        public List<string> ProfileDataParse(string body, StreamWriter writer)
        {
            dynamic profile;
            try
            {
                profile = JsonConvert.DeserializeObject<dynamic>(body);
            }
            catch (Exception ex)
            {
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine();
                writer.WriteLine("Invalid JSON format.");
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                return new List<string>();
            }

            if (profile == null)
            {
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine();
                writer.WriteLine("Empty or malformed JSON.");
                return new List<string>();
            }

            // Prevent null assignments explicitly
            List<string> parsedData = new List<string>
            {
            profile.Name?.ToString() ?? string.Empty,
            profile.Bio?.ToString() ?? string.Empty,
            profile.Image?.ToString() ?? string.Empty
            };

            Console.WriteLine($"-- Parsed Data --");
            Console.WriteLine($"Name: {parsedData[0]}, Bio: {parsedData[1]}, Image: {parsedData[2]}");

            return parsedData;
        }

        public Trade TradeDataParse(string body, StreamWriter writer)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject<dynamic>(body);
                if (data == null)
                {
                    writer.WriteLine("HTTP/1.1 400 Bad Request");
                    writer.WriteLine("Invalid trade data format.");
                    return null;
                }

                float minDmg;
                if (!float.TryParse(data.MinimumDamage?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out minDmg))
                {
                    writer.WriteLine("HTTP/1.1 400 Bad Request");
                    writer.WriteLine("Invalid minimum damage value.");
                    return null;
                }

                var trade = new Trade
                {
                    TradeId = data.Id?.ToString(),
                    OfferedCard = new Card
                    {
                        CardID = data.CardToTrade?.ToString()
                    },
                    RequestedCardType = data.Type?.ToString(),
                    RequestedMinimumDamage = minDmg
                };

                if (string.IsNullOrEmpty(trade.TradeId) || trade.OfferedCard == null || string.IsNullOrEmpty(trade.RequestedCardType) || trade.RequestedMinimumDamage < 0)
                {
                    writer.WriteLine("HTTP/1.1 400 Bad Request");
                    writer.WriteLine("Invalid trade data provided.");
                    return null;
                }

                Console.WriteLine($"Parsed Trade Data: TradeId={trade.TradeId}, OfferedCard={trade.OfferedCard.CardID}, CardType={trade.RequestedCardType}, MinDmg={trade.RequestedMinimumDamage}");
                return trade;
            }
            catch (JsonException ex)
            {
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine($"Error parsing trade data: {ex.Message}");
                return null;
            }
        }

        public string ParseCardId(string body, StreamWriter writer)
        {
            try
            {
                string cardId = JsonConvert.DeserializeObject<string>(body);
                if (string.IsNullOrEmpty(cardId))
                {
                    writer.WriteLine("HTTP/1.1 400 Bad Request");
                    writer.WriteLine("Invalid card ID provided.");
                    return null;
                }
                return cardId;
            }
            catch (Exception ex)
            {
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine($"Error parsing card ID: {ex.Message}");
                return null;
            }
        }

        public string ParseTradeId(string path, StreamWriter writer)
        {
            string[] pathParts = path.Split('/');
            if (pathParts.Length < 3 || string.IsNullOrEmpty(pathParts[2]))
            {
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine("Invalid trade ID provided.");
                return null;
            }
            return pathParts[2].Trim();
        }


        //----------TOKEN--PARSER----------
        public string TokenParse(string headers, StreamWriter writer)//gets token from header
        {
            string[] headerLines = headers.Split("\r\n");//splits header by lines (again)

            foreach (string header in headerLines)//looks for the authorization header
            {
                if (header.StartsWith("Authorization: Bearer "))
                {
                    // Extract the token by removing the "Bearer " prefix
                    return header.Substring("Authorization: Bearer ".Length).Trim();//gets token by deleting unecessary words and only leaving the token / sends it back
                }
            }
            writer.WriteLine("HTTP/1.1 400 Bad Request");
            writer.WriteLine();
            writer.WriteLine("Invalid Authorization header format. Expected 'Bearer <token>'.");
            return null;
        }

        //----------CARD--PARSER----------
        public List<Card> CardDataParse(string body, StreamWriter writer)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<List<Card>>(body);

                if (data == null || data.Count == 0)
                {
                    Console.WriteLine("Deserialization failed or no cards found.");
                    return new List<Card>();
                }

                foreach (var card in data)
                {
                    if (string.IsNullOrEmpty(card.CardName))
                    {
                        Console.WriteLine("Card name is missing or empty.");
                        continue; 
                    }

                    //cheks if the card is a spell or monster
                    if (card.CardName.Contains("spell", StringComparison.OrdinalIgnoreCase))
                    {
                        card.CardType = "Spell";
                        card.CardMonster = "None"; 
                        card.CardElement = GetElement(card.CardName);
                    }
                    else
                    {
                        card.CardType = "Monster";
                        card.CardElement = GetElement(card.CardName); 

                        card.CardMonster = GetMonsterType(card.CardName);
                    }

                    //Debug

                    //Console.WriteLine($"Card Parsed: Name: {card.CardName}, Type: {card.CardType}, Element: {card.CardElement}, Monster: {card.CardMonster}");
                }

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing card data: {ex.Message}");
                _response.HttpResponse(400, $"Invalid card data: \"{ex.Message}\"", writer);
                return new List<Card>();
            }
        }

        public List<string> CardIdParse(string body, StreamWriter writer)
        {
            try
            {
                // Deserialize the JSON body into a list of card IDs
                var cardIds = JsonConvert.DeserializeObject<List<string>>(body);

                // Check if the list is null or empty
                if (cardIds == null || cardIds.Count == 0)
                {
                    Console.WriteLine("Failed to parse card IDs: No card IDs found in the request body.");
                    _response.HttpResponse(400, "Request body must contain a non-empty list of card IDs.", writer);
                    return new List<string>(); // Return an empty list to prevent further processing
                }

                return cardIds;
            }
            catch (JsonException ex) // Handle JSON-specific exceptions
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                _response.HttpResponse(400, "Invalid JSON format. Please provide a valid list of card IDs.", writer);
                return new List<string>(); // Return an empty list to prevent further processing
            }
            catch (Exception ex) // Handle any other unexpected exceptions
            {
                Console.WriteLine($"Unexpected error during card ID parsing: {ex.Message}");
                _response.HttpResponse(500, "An unexpected error occurred while processing your request.", writer);
                return new List<string>();
            }
        }

        private string GetElement(string cardName)
        {
            if (cardName.Contains("fire", StringComparison.OrdinalIgnoreCase))
            {
                return "Fire";
            }
            else if (cardName.Contains("water", StringComparison.OrdinalIgnoreCase))
            {
                return "Water";
            }
            else
            {
                return "Normal";
            }
        }

        private string GetMonsterType(string cardName)//enums, get name for each name 
        {
            if (cardName.Contains("dragon", StringComparison.OrdinalIgnoreCase))
            {
                return "Dragon";
            }
            else if (cardName.Contains("goblin", StringComparison.OrdinalIgnoreCase))
            {
                return "Goblin";
            }
            else if (cardName.Contains("ork", StringComparison.OrdinalIgnoreCase))
            {
                return "Ork";
            }
            else if (cardName.Contains("knight", StringComparison.OrdinalIgnoreCase))
            {
                return "Knight";
            }
            else if (cardName.Contains("kraken", StringComparison.OrdinalIgnoreCase))
            {
                return "Kraken";
            }
            else if (cardName.Contains("elf", StringComparison.OrdinalIgnoreCase))
            {
                return "Elf";
            }
            else if (cardName.Contains("wizzard", StringComparison.OrdinalIgnoreCase))
            {
                return "Wizzard";
            }
            else
            {
                return "Unknown";
            }
        }
    }
}