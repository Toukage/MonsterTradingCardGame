using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.BusinessLayer;

namespace MonsterTradingCardGame.Routing
{
    public class Router
    {
        private readonly Parser _parser;
        private readonly Package _pack = new();
        private readonly User _user = new();
        private readonly UserRepo _userMan = new();
        private readonly Response _response = new();
        private readonly Deck _deck = new();
        private readonly Trade _trade = new();
        private readonly Stack _stacks = new();
        private readonly Tokens _token = new();
        private readonly Scores _score = new();
        private readonly Battle _battle = new();

        public Router(Parser parser)
        {
            _parser = parser;
        }

        //----------Router--for--Request--Parsing----------

        public async Task RequestParseRouter(string requestString, StreamWriter writer)
        {
            try
            {
                var parser = new Parser();
                var (method, path, headers, body) = parser.RequestParse(requestString, writer);//parses the request

                await MethodRouter(method, path, headers, body, writer);//sends parsed data to method router where it gets handeld acording to the users actions
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during request parsing: {ex.Message}");
                await _response.HttpResponse(500, "Internal Server Error during parsing.", writer);
            }

        }

        //----------Router-Request-Methode----------
        private async Task MethodRouter(string method, string path, string headers, string body, StreamWriter writer)
        {
            Console.WriteLine("** inside MethodeRouter **");
            if (path == "/sessions" || path == "/users")//router for users and sessions since no need for token
            {
                await NoTokenRouter(path, body, writer);
                return;
            }
            User user = new User();
            var (isValid, userId) = await _token.CheckToken(headers, user, writer);//checks token and gives userid

            if (isValid && userId != -1)
            {
                switch (method.ToUpper())//toupper to avoid case issues
                {
                    case "GET"://sends all get requets to the get router etc.
                        Console.WriteLine("** inside switch case for GET **");
                        await GetRouter(path, body, user, writer);
                        break;
                    case "POST":
                        Console.WriteLine("** inside switch case for POST **");
                        await PostRouter(path, headers, body, user, writer);
                        break;
                    case "PUT":
                        Console.WriteLine("** inside switch case for PUT **");
                        await PutRouter(path, body, user, writer);
                        break;
                    case "DELETE":
                        Console.WriteLine("** inside switch case for DELETE **");
                        await DeleteRouter(path, body, user, writer);
                        break;
                    default:
                        Console.WriteLine("** inside switch case badreq **");
                        await _response.HttpResponse(400, "Invalid Methode", writer);
                        break;
                }
            }           
            else if (!isValid)//already handled in tokens
            {
                return;
            }
            else
            {
                await _response.HttpResponse(400, "No Endpoint given", writer);
                return;
            }
            
            
        }

        //----------Router--for--No-Token--Requests----------
        private async Task NoTokenRouter(string path, string body, StreamWriter writer)
        {
            Console.WriteLine("** inside Non-Token Router **");
            switch (path)
            {
                case "/sessions"://path for login
                    await _user.Login(body, writer);
                    Console.WriteLine("** inside switch case for sessions **");//debug
                    break;

                case "/users"://path for register
                    await _user.Register(body, writer);
                    Console.WriteLine("** inside switch case for register **");//debug
                    break;
                default:
                    await _response.HttpResponse(404, "Endpoint not found", writer);
                    Console.WriteLine("** inside switch case for not found **");//debug
                    break;
            }
        }

        //----------Routers-for-Requests----------
        private async Task PostRouter(string path, string headers, string body, User user, StreamWriter writer)//routs all Post requests
        {
            Console.WriteLine("** inside PostRouter **");
            switch (path)
            {
                case "/packages"://path for admin pack generation
                    if (await _userMan.CheckAdmin(user.UserId) == true) //checks if user is admin
                    {
                        Console.WriteLine("** inside switch case for package **");
                        await _pack.CreatePack(body, headers, writer);
                    }
                    else //otherwise error
                    {
                        await _response.HttpResponse(401, "Admin access required", writer);
                    }
                    break;

                case "/transactions/packages":
                    Console.WriteLine("** end of switch case for buying packs **");//debug
                    await _pack.BuyPack(user, writer);
                    break;

                case "/battles":
                    Console.WriteLine("** end of switch case for battles **");//debug
                    await _battle.CardBattle(user.UserId, writer);
                    break;

                case string s when s.StartsWith("/tradings"):
                    bool hasTradeId = s.Split('/').Length == 3;
                    if (hasTradeId)
                    {
                        await _trade.TradeCard(path, body, user, writer);
                    }
                    else
                    {
                        await _trade.NewTrade(body, user, writer);
                    }
                    Console.WriteLine("** end of switch case for trading **");//debug
                    break;

                default:
                    Console.WriteLine("** end of switch case for not found **");//debug
                    await _response.HttpResponse(404, "Endpoint not found", writer);
                    break;
            }
        }

        private async Task GetRouter(string path, string body, User user, StreamWriter writer)//routes all Get method actions
        {
            Console.WriteLine($"GetRouter called for path: {path}");
            switch (path)
            {
                case "/cards":
                    await _stacks.GetStack(user, writer);
                    Console.WriteLine("Handling GET /cards");//debug
                    break;

                case "/deck":
                    await _deck.GetDeck(user, writer);
                    Console.WriteLine("Handling GET /deck");//debug
                    break;

                case string s when s.StartsWith("/users/"):
                    bool correctUser = _token.ValidateUser(path, user, writer);
                    if (correctUser) 
                    {
                        await _user.Profile(user, writer);
                    }
                    else
                    {
                        await _response.HttpResponse(401, "Unauthorized", writer);
                    }
                    Console.WriteLine("** end of switch case for getting user data **");//debug
                    break;

                case "/tradings":
                    await _trade.GetTrades(writer);
                    Console.WriteLine("** end of  switch case for trading **");//debug
                    break;

                case "/stats":
                    await _score.Stats(user, writer);
                    Console.WriteLine("** end of  switch case for stats **");//debug
                    break;

                case "/scoreboard":
                    await _score.scoreBoard(writer);
                    Console.WriteLine("** inside switch case for scoreboard **");
                    break;

                default:
                    await _response.HttpResponse(404, "Endpoint not found", writer);
                    break;
            }
        }

        private async Task PutRouter(string path, string body, User user, StreamWriter writer)//routes all Get method actions
        {
            Console.WriteLine($"GetRouter called for path: {path}");//debug
            switch (path)
            {
                case "/deck":
                    await _deck.UpdateDeck(body, user, writer);
                    Console.WriteLine("** inside switch case for deck creation **");//debug
                    break;

                case string s when s.StartsWith("/users/"):
                    bool correctUser = _token.ValidateUser(path, user, writer);
                    if (correctUser)
                    {
                        await _user.EditProfile(body, user, writer);
                    }
                    else
                    {
                        await _response.HttpResponse(401, "Unauthorized", writer);
                    }
                    Console.WriteLine("** end of switch case for putting user data **");//debug
                    break;

                default:
                    await _response.HttpResponse(404, "Endpoint not found", writer);
                    break;
            }
        }

        private async Task DeleteRouter(string path, string body, User user, StreamWriter writer)//routes all Get method actions
        {
            Console.WriteLine($"GetRouter called for path: {path}");//debug
            switch (path)
            {
                case string s when s.StartsWith("/tradings"):
                    await _trade.RemoveTrade(path, user, writer);
                    Console.WriteLine("** inside switch case for deleting a trade **");//debug
                    break;

                default:
                    await _response.HttpResponse(404, "Endpoint not found", writer);
                    break;
            }
        }
    }
}
