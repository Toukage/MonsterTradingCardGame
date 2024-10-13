using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Text.Json;
using System.Threading.Tasks;


namespace MonsterTradingCardGame.Routing
{

    public class Parser
    {
        private readonly Router _router;
        public Parser(Router router)
        {
            _router = router;
        }

        //----------------------REQUEST--PARSER----------------------
        public void RequestParse(string requestString, StreamWriter writer)
        {
            string[] requestLines = requestString.Split(new[] { "\r\n" }, StringSplitOptions.None);//splits each line of the request into its own object

            if (requestLines.Length < 1)//error mssg when the request no lines
            {
                Console.WriteLine("Invalid request received, closing connection.");
                return;
            }
            Console.WriteLine("Request received: " + requestString);

            //--------Request-Line--Handling--------

            string requestStartLine = requestLines[0];//first line gets split off
            string[] requestLineParts = requestStartLine.Split(' ');//splits request line into methode/path etc

            if (requestLineParts.Length < 2)//checks if request line has at least method and path so it can be used
            {
                Console.WriteLine("Malformed request line: " + requestStartLine);
                return;
            }

            string method = requestLineParts[0];//extracts the method data from the request
            string path = requestLineParts[1];//extracts the path data from the request

            Console.WriteLine($"Parsed Request: Method={method}, Path={path}");//debug info to show that method and path were extracted correctly

            //--------Header--Handling--------

            Dictionary<string, string> headers = new Dictionary<string, string>();//dictionary to store the headers
            int i = 1;//counts how many lines there are in the headers
            Console.WriteLine("** setting up dicitionary for headers   **");

            while (i < requestLines.Length && !string.IsNullOrEmpty(requestLines[i]))//loops through all lines until it findes an empty one
            {
                string[] headerParts = requestLines[i].Split(new[] { ':' }, 2);//splits header into key and vlaue, needed later
                if (headerParts.Length == 2)//checks if the header is valid (has both key and vlaue)
                {
                    headers[headerParts[0].Trim()] = headerParts[1].Trim();//trims usless extra space and saves the headers into dictionary
                }
                i++;
            }

            Console.WriteLine($"Received headers: {string.Join(", ", headers)}");
            string headersString = string.Join("\r\n", headers.Select(h => $"{h.Key}: {h.Value}"));//converts the headers back to string after checking all values are valid, and no extra space is left

            //--------Body--Handling--------

            Console.WriteLine("** Handling Body **");

            string body = string.Join("\r\n", requestLines.SkipWhile(line => !string.IsNullOrEmpty(line)).Skip(1));//skips all headers and empty lines , and saves them as body

            //--------Routing--to--Path--------

            

            _router.MethodRouter(method, path, headersString, body, writer);//sends the parsed data back to the router
        }

        //----------------------BODY--PARSERS----------------------
        public (string username, string password) UserDataParse(string body, StreamWriter writer)//parser for getting password and username from the request body
        {
            dynamic credentials;
            try
            {
                credentials = JsonConvert.DeserializeObject<dynamic>(body);//deserializes the json string into dynamic c# object so we can access the objects easier 
            }
            catch (Exception ex)
            {
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine();
                writer.WriteLine("Invalid JSON format.");
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                return (null, null);
            }

            if (credentials == null)//if there is no data in body, or its in the wrong form
            {
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine();
                writer.WriteLine("Empty or malformed JSON.");
                return (null, null);
            }

            string username = credentials.Username;//converts the username into its own string
            string password = credentials.Password;//converts the password into its own string

            writer.WriteLine($"The Users Credentials are : Username {username} : Password: {password}");

            return (username, password);//sends the parsed data back
        }

        public string? GetToken(string headers, StreamWriter writer)//gets the token from the header
        {
            string[] headerLines = headers.Split("\r\n");//splits the header by lines again

            foreach (string header in headerLines)//looks for the authorization header 
            {
                if (header.StartsWith("Authorization: Bearer "))
                {
                    return header.Substring("Authorization: Bearer ".Length).Trim();//gets the token by deleteing the unecessary words and only leaving the token, and sends it back
                }
            }
            writer.WriteLine("HTTP/1.1 400 Bad Request");
            writer.WriteLine();
            writer.WriteLine("Invalid Authorization header format. Expected 'Bearer <token>'.");
            return null;
        }
    }
}
