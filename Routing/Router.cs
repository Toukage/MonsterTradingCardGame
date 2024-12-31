using System;
using System.IO;
using System.Linq;
using System.Text;
using Npgsql.Internal;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using System.Collections.Generic;
using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.BusinessLayer;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;


namespace MonsterTradingCardGame.Routing
{
    public class Router
    {
        private readonly Parser _parser = new();
        private readonly Package _pack = new();
        private readonly Card _card = new();
        private readonly User _user = new();
        private readonly Response _response = new();
        private readonly Deck _deck = new();
        private readonly Trade _trade = new();
        private readonly Stack _stacks = new();
        private readonly Tokens _token = new();

        public Router()
        {
            _parser = new Parser(this);
        }

        //token check before routing 
        //----------RequestParseRouter----------

        public void RequestParseRouter(string requestString, StreamWriter writer)
        {
            _parser.RequestParse(requestString, writer);//sends the request to the parser to get some legible 
        }


        //----------Router-Request-Methode----------
        public void MethodRouter(string method, string path, string headers, string body, StreamWriter writer)
        {
            Console.WriteLine("** inside MethodeRouter **");
            string token = _parser.TokenParse(headers, writer);
            var (isValid, isAdmin, userId) = _token.CheckToken(headers, writer);
            if (isValid)
            {
                switch (method.ToUpper())//toupper to avoid case issues
                {
                    case "GET"://sends all get requets to the get router
                        Console.WriteLine("** inside switch case for GET **");
                        GetRouter(path, userId, body, writer);
                        break;
                    case "POST": //sends all post requests to post router
                        Console.WriteLine("** inside switch case for POST **");
                        PostRouter(path, userId, isAdmin, body, writer);
                        break;
                    case "PUT":
                        Console.WriteLine("** inside switch case for PUT **");
                        PutRouter(path, userId, body, writer);
                        break;
                    case "DELETE":
                        Console.WriteLine("** inside switch case for DELETE **");
                        DeleteRouter(path, userId, body, writer);
                        break;
                    default:
                        Console.WriteLine("** inside switch case badreq **");
                        _response.HttpResponse(400, "Invalid Methode", writer);
                        break;
                }
            }
            else if (path == "/sessions" || path == "/users")
            {
                switch (path)
                {
                    case "/sessions"://path for login
                        Console.WriteLine("** inside switch case for sessions **");
                        _user.Login(body, writer);
                        break;

                    case "/users"://path for register
                        Console.WriteLine("** inside switch case for register **");
                        _user.Register(body, writer);
                        break;
                    default:
                        Console.WriteLine("** inside switch case for not found **");
                        _response.HttpResponse(404, "Endpoint not found", writer);
                        break;
                }
            }
            else if (!isValid)
            {
                //error mssg for invalid
            }
            else
            {
                _response.HttpResponse(400, "No Endpoint given", writer);
                return;
            }
            
            
        }

        //----------Routers-for-Request-Parsing----------
        private void PostRouter(string path, int? userId, bool isAdmin, string body, StreamWriter writer)//routs all Post requests
        {
            Console.WriteLine("** inside PostRouter **");
            switch (path)
            {
                case "/packages"://path for admin pack generation
                    if (isAdmin) {
                        Console.WriteLine("** inside switch case for package **");
                        _pack.CreatePack(body, writer);
                    }
                    break;

                case "/transactions/packages":
                    Console.WriteLine("** inside switch case for buying packs **");
                    _pack.BuyPack(userId, writer);
                    break;

                case "/battles":
                    Console.WriteLine("** inside switch case for battles **");
                    break;

                case "/tradings":
                    Console.WriteLine("** inside switch case for trading **");
                    break;

                default:
                    Console.WriteLine("** inside switch case for not found **");
                    _response.HttpResponse(404, "Endpoint not found", writer);
                    break;
            }
        }

        private void GetRouter(string path, int? userId, string body, StreamWriter writer)//routes all Get method actions
        {
            Console.WriteLine("** inside GetRouter **");
            Console.WriteLine($"GetRouter called for path: {path}");
            switch (path)
            {
                case "/cards":
                    _stacks.GetStack(userId, writer);
                    Console.WriteLine("Handling GET /cards");//debug
                    break;

                case "/deck":
                    _deck.CheckDeck(body, userId, writer);
                    Console.WriteLine("Handling GET /deck");//debug
                    break;

                case "/users":
                    Console.WriteLine("** inside switch case for getting user data **");
                    break;

                case "/tradings":
                    Console.WriteLine("** inside switch case for trading **");

                    break;

                case "/stats":
                    Console.WriteLine("** inside switch case for stats **");
                    break;

                case "/scoreboard":
                    Console.WriteLine("** inside switch case for scoreboard **");
                    break;

                default:
                    _response.HttpResponse(404, "Endpoint not found", writer);
                    break;
            }
        }

        private void PutRouter(string path, int? userId, string body, StreamWriter writer)//routes all Get method actions
        {
            Console.WriteLine("** inside GetRouter **");
            Console.WriteLine($"GetRouter called for path: {path}");
            switch (path)
            {
                case "/deck":
                    Console.WriteLine("** inside switch case for deck creation **");//debug
                    break;

                case "/users":
                    Console.WriteLine("** inside switch case for user editing **");//debug
                    break;

                default:
                    _response.HttpResponse(404, "Endpoint not found", writer);
                    break;
            }
        }

        private void DeleteRouter(string path, int? userId, string body, StreamWriter writer)//routes all Get method actions
        {
            Console.WriteLine("** inside GetRouter **");//debug
            Console.WriteLine($"GetRouter called for path: {path}");
            switch (path)
            {
                case "/tradings":
                    Console.WriteLine("** inside switch case for deleting a trade **");
                    break;

                default:
                    _response.HttpResponse(404, "Endpoint not found", writer);
                    break;
            }
        }
    }
}
