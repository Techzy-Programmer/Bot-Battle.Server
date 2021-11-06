using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Chatbot.Server
{
    public class Program
    {
        private const int WSPort = 8443;
        private static WebSocketServer WSSvr;

        private static void Main(string[] _)
        {
            Utils.UpdateAuth();
            var Command = "Continue";
            WSSvr = new WebSocketServer(WSPort);
            WSSvr.AddWebSocketService<Clash>("/Clash");
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            WSSvr.Start(); WSSvr.Log.Level = LogLevel.Fatal;
            WSSvr.KeepClean = true;

            while (true)
            {
                if (Command == "Exit") { WSSvr.Stop(); break; }
                else if (Command == "Change")
                {
                    Console.WriteLine("Enter A New Auth-Key");
                    if (!Utils.UpdateAuth(Console.ReadLine()))
                    {
                        Console.WriteLine("Update-Failed!");
                        System.Threading.Thread.Sleep(2000);
                    }
                }

                Console.Clear();
                if (WSSvr.IsListening) Console.WriteLine($"WebSocket Server Running [On {WSPort}].....");
                Console.WriteLine("Use Commands 'Change' OR 'Exit' To Perform A Task!");
                Command = Console.ReadLine();
            }
        }

        private static void OnUnhandledException(object _, UnhandledExceptionEventArgs UEArgs)
        {
            
        }
    }
}
