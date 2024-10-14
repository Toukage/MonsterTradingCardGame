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
        private bool _isRunning = true;//flag for running the server or closing it
        private readonly Router _router;

        public Server(string address, int port = 10001, Router router = null)
        {
            _listener = new TcpListener(IPAddress.Parse(address), port);//makes the TcpListener with specific adress and port
            _router = router ?? new Router();//use existing router or make a new one
        }


        public async Task StartAsync()//starts serevr
        {
            try
            {
                _listener.Start();//starts TcpListener to accept connections
                Console.WriteLine($"-> Server started at {IPAddress.Any} : {_listener.LocalEndpoint} <-"); //shows what adress and port is listend at
                Console.WriteLine("-> connecting . . . <-");

                _ = ServerLoop();//starts server loop async,fire and forget because i dont need to wait for it to be done

                while (true)//stops server if exit is input
                {
                    string input = Console.ReadLine();
                    if (input == "exit")
                    {
                        _isRunning = false;
                        _listener.Stop();//stops listener too
                        Console.WriteLine("-> Server stopped <-");
                        break;
                    }
                }
            }
            catch (Exception ex)//exception for when server cant start
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }


        private async Task ServerLoop()
        {
            while (_isRunning)//loop to accept and handle incoming con
            {
                try
                {
                    using TcpClient client = await _listener.AcceptTcpClientAsync();//waits for client , then creats TcpListener obj
                    Console.WriteLine("-> Client connected. <-");
                    _ = HandleRequest(client);//Fire and forget, handels the request async
                }
                catch (Exception ex)//exception for the loop
                {
                    Console.WriteLine($"Error in ServerLoop: {ex.Message}");
                }
            }
        }

        public async Task HandleRequest(TcpClient client)// processes the request
        {
            try
            {
                NetworkStream stream = client.GetStream();// get the stream with all the data
                byte[] buffer = new byte[1024];//creats byte array because stream is in byte
                int length = await stream.ReadAsync(buffer, 0, buffer.Length);//makes serverloop wait until ReadAsync is done, reads data async, and gives the length of the message (use full for knowing if it was empty)
                string requestString = Encoding.UTF8.GetString(buffer, 0, length);//coverts byte to string so we can parse the HTTP req

                if (length == 0)//check foor empty req
                {
                    Console.WriteLine("Empty request received, closing connection.");
                    return;
                }

                Console.WriteLine("Received: " + requestString);//shows the request

                using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };//creates a writer for the response


                //--------Routing--------

                _router.RequestParseRouter(requestString, writer);//sends the requeststring to the router

                writer.Flush();//makes sure the writer is sent
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Closing client connection.");
                client.Close();//closes client even if error happens
            }
        }
    }
}
