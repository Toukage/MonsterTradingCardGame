
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MonsterTradingCardGame.Server;
using MonsterTradingCardGame.Routing;
using MonsterTradingCardGame.DataLayer;


namespace MTCG
{
    class Program
    {
        static async Task Main(string[] args)//async main so that the async server start works
        {
            IPAddress address = IPAddress.Any;//allows server to listen to all avaible IPAddresses
            int port = 80;//port that the server will use
            var router = new Router(null);
            var parser = new Parser(router);
            router = new Router(parser);
            var server = new Server(address.ToString(), port, router);//server listens to HTTP request on ip and port and send the request to teh router

            await server.StartAsync();//starts the server asynchronously , so it wont block the rest of the program while waiting for connection

        }
    }

}
