using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using MonsterTradingCardGame.Routing;
using MonsterTradingCardGame.DataLayer;


namespace MonsterTradingCardGame.Server
{
    public class Server
    {
        private readonly TcpListener _listener;
        private readonly Router _router;
        private readonly SemaphoreSlim _connectionLimit = new SemaphoreSlim(100);

        public Server(string address, int port, Router router)
        {
            _listener = new TcpListener(IPAddress.Parse(address), port);//makes the TcpListener with specific adress and port
            _router = router ?? throw new ArgumentNullException(nameof(router));
            //use existing router or make a new one
        }

        public async Task StartAsync()//starts serevr
        {
            try
            {
                _listener.Start();//starts TcpListener to accept connections
                Console.WriteLine($"-> Server started at {IPAddress.Any} : {_listener.LocalEndpoint} <-"); //shows what adress and port is listend at
                Console.WriteLine("-> connecting . . . <-");
                while (true)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected...");
                    _ = Task.Run(async () => await HandleRequest(client));//starts the task for handling the request
                }
            }
            catch (Exception ex)//exception for when server cant start
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        public async Task HandleRequest(TcpClient client)//conncurrent und asynchron
        {
            await _connectionLimit.WaitAsync();//waits for the semaphore to be released
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[8192];
                int length;
                StringBuilder requestBuilder = new StringBuilder();

                
                while ((length = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)//ließt die daten in chunks für große datenmengen
                {
                    requestBuilder.Append(Encoding.UTF8.GetString(buffer, 0, length));
                    if (!stream.DataAvailable) break;
                }

                string requestString = requestBuilder.ToString();//makes the request into a string
                Console.WriteLine("Received: " + requestString);

                //--------Routing--------

                
                using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                // asynchrones routing
                await _router.RequestParseRouter(requestString, writer);

                await writer.FlushAsync();//stellt sicher das alles geschrieben wird bevor die verbindung geschlossen wird
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
            }
            finally
            {
                _connectionLimit.Release();
                Console.WriteLine("-----XXX Closing client connection XXX-----");
                client.Close(); //schließt die Verbindung
            }
        }

    }
}
