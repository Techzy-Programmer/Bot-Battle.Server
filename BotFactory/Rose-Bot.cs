using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Chatbot.Server.BotFactory
{
    public class RoseBot : IBotBase
    {
        private string VirtUsr = "";
        public string Name => "Rose-Bot";
        private Uri ApiURI = new Uri("http://54.215.197.164");
        private readonly string[] Names = new string[]
        {
            "User", "Ram", "Mohan", "Alex", "Rishabh", "Buddy",
            "Divya", "Alok", "Rohan", "Sania", "Ishika", "Rinku",
            "Amit", "Fernaldis", "Crab", "Toper", "Jack", "John Dev",
            "Aniket", "Anonym", "Vini", "Ayush", "Sunny", "Bachha",
            "Golu", "Yog", "Andres", "Trinity", "Neo", "Tripti",
            "Gopal", "Rajesh", "Baby", "Misty", "Joe", "Tom",
        };

        public RoseBot() { VirtUsr = Names[new Random().Next(0, 36)]; }

        public async Task<string> Think(string TQuery)
        {
            var HisSV = TQuery;
            var Sendable = string.Empty; ReCall:;
            var PostDT = new Dictionary<string, string>
            {
                { "user", VirtUsr },
                { "send", Sendable },
                { "message", Uri.EscapeDataString(TQuery) }
            };

            try
            {
                using (var HPoster = new HttpClient() { BaseAddress = ApiURI })
                {
                    var HMsg = new HttpRequestMessage(HttpMethod.Post, "/ui.php");
                    HMsg.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit" +
                        "/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36 Edg/94.0.992.50");
                    HMsg.Content = new FormUrlEncodedContent(PostDT);
                    var RzReply = await (await HPoster.SendAsync(HMsg)).Content.ReadAsStringAsync();

                    if (RzReply.Contains("[callback="))
                    {
                        await Task.Delay(2500);
                        TQuery = "[callback ]";
                        Sendable = "true";
                        goto ReCall;
                    }

                    return RzReply;
                }
            }
            catch (Exception E) { E.ErrorToDisk(); }
            return null;
        }
    }
}
