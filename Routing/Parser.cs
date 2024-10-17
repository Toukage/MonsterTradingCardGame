using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonsterTradingCardGame.Routing
{
    using Newtonsoft.Json;
    using System.Net;

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

            if (requestLines.Length < 1)//error msg when the request has no lines
            {
                Console.WriteLine("Invalid request received, closing connection.");
                return;
            }

            //--------Request-Line--Handling--------

            string requestStartLine = requestLines[0];//first line gets split off
            string[] requestLineParts = requestStartLine.Split(' ');//splits request line into methode/path 

            if (requestLineParts.Length < 2)//checks if request line has at least methode and path so it can be used
            {
                Console.WriteLine("Malformed request line: " + requestStartLine);
                return;
            }

            string method = requestLineParts[0];//extracts method 
            string path = requestLineParts[1];//extracts path

            Console.WriteLine($"Parsed Request: Method={method}, Path={path}");//debug

            //--------Header--Handling--------

            Dictionary<string, string> headers = new Dictionary<string, string>();//dicitionary to store the headers
            int i = 1;//counts how many lines there are in the headers
            Console.WriteLine("** set up dicitionary for strings   **");

            while (i < requestLines.Length && !string.IsNullOrEmpty(requestLines[i]))//loops trough all lines unitl there are no lines / it hits an empty line
            {
                Console.WriteLine("** inside while loop for headers  **");//splits header into key n value 
                string[] headerParts = requestLines[i].Split(new[] { ':' }, 2);
                if (headerParts.Length == 2)//checks if header is valid
                {
                    headers[headerParts[0].Trim()] = headerParts[1].Trim();//trims extra space / stores headers in dictionary
                    Console.WriteLine("** inside if condition for headers  **");
                }
                i++;//counts lines
            }

            Console.WriteLine($"Received headers: {string.Join(", ", headers)}");//STRING BUILDER

            //--------Body--Handling--------
            Console.WriteLine("** inside body handling **");
            string body = string.Join("\r\n", requestLines.SkipWhile(line => !string.IsNullOrEmpty(line)).Skip(1));// skip headers and ANY extra empty lines

            //--------Routing--to--Path--------
            Console.WriteLine("Request received: " + requestString);
            Console.WriteLine("Method: " + method + ", Path: " + path);

            string headersString = string.Join("\r\n", headers.Select(h => $"{h.Key}: {h.Value}"));//converts the headers back to string after checking all values are valid, and no extra space is left
            _router.MethodRouter(method, path, headersString, body, writer);//sends parsed info back to router to be handeld
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

        //----------------------HEADER--PARSERS----------------------
        public string? GetToken(string headers, StreamWriter writer)//gets token from header
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
    }
}