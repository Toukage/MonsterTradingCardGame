using MonsterTradingCardGame.BusinessLayer;

namespace MonsterTradingCardGame.Routing
{
    using MonsterTradingCardGame.DataLayer;
    using Newtonsoft.Json;

    public class Parser
    {
        private readonly Router _router;
        private readonly Response _response = new();


        public Parser(Router router)
        {
            _router = router;
        }

        public Parser()
        {
        }

        public void RequestParse(string requestString, StreamWriter writer)
        {
            string[] requestLines = requestString.Split(new[] { "\r\n" }, StringSplitOptions.None);//system enviroment line ending.

            if (requestLines.Length < 1)
            {
                Console.WriteLine("Invalid request received, closing connection.");
                return;
            }

            //--------Request-Line Handling--------
            string requestStartLine = requestLines[0];//The first line (method + path)
            string[] requestLineParts = requestStartLine.Split(' ');

            if (requestLineParts.Length < 2)
            {
                Console.WriteLine("Malformed request line: " + requestStartLine);
                return;
            }

            string method = requestLineParts[0];//extract method
            string path = requestLineParts[1];//Extract path

            Console.WriteLine($"----> Parsed Request: Method={method}, Path={path} <----");//debug

            //--------Header Handling--------
            Dictionary<string, string> headers = new Dictionary<string, string>();
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


            //--------Routing to Path--------
            string headersString = string.Join("\r\n", headers.Select(h => $"{h.Key}: {h.Value}"));//Convert headers back to string
            _router.MethodRouter(method, path, headersString, body, writer); 
        }


        //----------------------BODY--PARSERS----------------------
        public (string username, string password) UserDataParse(string body, StreamWriter writer)//parser for getting username/password from the request body
        {
            dynamic credentials;
            try
            {
                credentials = JsonConvert.DeserializeObject<dynamic>(body);//deserializes the json string into dynamic c# obj so we can access the object easier
            }
            catch (Exception ex)
            {
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine();
                writer.WriteLine("Invalid JSON format.");
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                return (null, null);
            }

            if (credentials == null)//error for if there is no data in body, or its in the wrong form 
            {
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine();
                writer.WriteLine("Empty or malformed JSON.");
                return (null, null);
            }

            string username = credentials.Username;//converts dynamic obj into string for username / password string 
            string password = credentials.Password;

            //writer.WriteLine($"The Users Credentials are : Username {username} : Password: {password}");
            return (username, password);//sends back parsed data
        }

        //----------TOKEN--PARSER----------
        public string TokenParse(string headers, StreamWriter writer)//gets token from header
        {
            Console.WriteLine("** TokenParse **");
            //Console.WriteLine($"** with this header {headers} **");

            string[] headerLines = headers.Split("\r\n");//splits header by lines (again)
            //Console.WriteLine($"** headers after splitting: {string.Join(", ", headerLines)} **");
            foreach (string header in headerLines)//looks for the authorization header
            {
                if (header.StartsWith("Authorization: Bearer "))
                {
                    string token = header.Substring("Authorization: Bearer ".Length).Trim();
                    Console.WriteLine($"** Found token: {token} **");
                    return token;
                }
            }
            Console.WriteLine("** No Authorization token found **");
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