using System;
using System.IO;
using System.Linq;
using System.Text;
using Npgsql.Internal;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using System.Collections.Generic;
using MonsterTradingCardGame.DataLayer;


namespace MonsterTradingCardGame.Routing
{
    public class Router
    {
        private readonly Parser _parser;
        private readonly UserManagement _management;
        public Router()
        {
            _parser = new Parser(this);
            _management = new UserManagement(_parser);//for calling register/login

        }

        //----------RequestParseRouter----------

        public void RequestParseRouter(string requestString, StreamWriter writer)
        {
           _parser.RequestParse(requestString, writer);//sends the request to the parser to get some legible 
        }
        

        //----------Router-Request-Methode----------
        public void MethodRouter(string method, string path, string headers, string body, StreamWriter writer)
        {
            Console.WriteLine("** inside MethodeRouter **");

            if (path == "/")
            {
                HandleRoot(writer);//root path sends a welcome message
                Console.WriteLine("** inside root if condition **");
                return;
            }

            switch (method.ToUpper())// toupper to avoid case issues
            {
                case "GET"://sends all get requets to the get router
                    Console.WriteLine("** inside switch case for GET **");
                    GetRouter(path, headers, body, writer);
                    break;
                case "POST": //sends all post requests to post router
                    Console.WriteLine("** inside switch case for POST **");
                    PostRouter(path, headers, body, writer);
                    break;
                default:
                    Console.WriteLine("** inside switch case badreq **");
                    BadReq(writer);
                    break;
            }
        }

        //----------Routers-for-Request-Parsing----------
        private void PostRouter(string path, string headers, string body, StreamWriter writer)//routs all Post requests
        {
            Console.WriteLine("** inside PostRouter **");
            switch (path)
            {
                case "/sessions"://path for login
                    Console.WriteLine("** inside switch case for sessions **");
                    _management.Login(body, headers, writer);
                    break;

                case "/users"://path for register
                    Console.WriteLine("** inside switch case for register **");
                    _management.Register(body, writer);
                    break;

                default:
                    Console.WriteLine("** inside switch case for not found **");
                    NotFound(writer);
                    break;
            }
        }

        private void GetRouter(string path, string headers, string body, StreamWriter writer)//routes all Get method actions
        {
            Console.WriteLine("** inside GetRouter **");
            Console.WriteLine($"GetRouter called for path: {path}");
            switch (path)
            {
                case "/cards"://path for accessing cards
                    _management.GetCards(body, headers, writer);
                    string responseBody = "GET request received for /cards.";
                    Console.WriteLine("Handling GET /cards"); 
                    break;
                default:
                    NotFound(writer);
                    break;
            }
        }

        //----------Root-Path-Handler----------
        private void HandleRoot(StreamWriter writer)//creates welcome page for root path :)
        {
            string responseBody = "200 OK";
            writer.WriteLine("HTTP/1.1 200 OK");
            writer.WriteLine("Content-Type: text/html");
            writer.WriteLine("Content-Length: 76");
            writer.WriteLine();
            writer.WriteLine("<html><body><h1>Welcome to the Monster Trading Card Game Server!</h1></body></html>");
        }

        //----------Error-Handlers----------
        private void NotFound(StreamWriter writer)//Not Found error handler
        {
            string responseBody = "404 Not Found";
            writer.WriteLine("HTTP/1.1 404 Not Found");
            writer.WriteLine("Content-Type: text/plain");
            writer.WriteLine($"Content-Length: {responseBody.Length}");
            writer.WriteLine("Connection: close");
            writer.WriteLine();
            writer.WriteLine(responseBody);
        }

        public void BadReq(StreamWriter writer)//Bad request error handler
        {
            string responseBody = "400 Bad Request";
            writer.WriteLine("HTTP/1.1 400 Bad Request");
            writer.WriteLine("Content-Type: text/plain");
            writer.WriteLine($"Content-Length: {responseBody.Length}");
            writer.WriteLine("Connection: close");
            writer.WriteLine();
            writer.WriteLine(responseBody);
        }
    }
}
