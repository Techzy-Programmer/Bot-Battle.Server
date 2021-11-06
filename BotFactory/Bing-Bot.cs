using System;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Chatbot.Server.BotFactory
{
    public class BingBot : IBotBase
    {
        private string CVId = null;
        private bool HasStarted = false;
        public string Name => "Bing-Bot";
        private Uri ApiURI = new Uri("https://services.bingapis.com/sydney/chat");

        public async Task<string> Think(string TQuery)
        {
            ThinkQuery Query;
            if (!HasStarted)
            {
                Query = new ThinkQuery
                {
                    userMessageText = TQuery,
                    source = "User",
                    isStartOfSession = true
                };
            }
            else
            {
                Query = new ThinkQuery
                {
                    userMessageText = TQuery,
                    isStartOfSession = false,
                    conversationId = CVId,
                    source = "User"
                };
            }
            
            var Reply = await PostThought(Query);

            if (Reply != null)
            {
                if (Reply.Contains("@failover_hyperlink"))
                    Reply = "I'm sorry! I didn't understood that.";
                HasStarted = true;
            }
            
            return Reply;
        }

        private async Task<string> PostThought(ThinkQuery Postable)
        {
            try
            {
                using (var HPoster = new HttpClient() { BaseAddress = ApiURI })
                {
                    var Cooked = new StringContent(JsonConvert.SerializeObject(Postable), Encoding.UTF8, "application/json");
                    var RespSTR = await (await HPoster.PostAsync(ApiURI, Cooked)).Content.ReadAsStringAsync();
                    var JResp = JObject.Parse(RespSTR);
                    
                    if (CVId == null && JResp.ContainsKey("conversationId")) CVId = (string)JResp["conversationId"];
                    return (string)((JObject)((JArray)JResp["messages"])[1])["text"];
                }
            }
            catch (Exception E) { E.ErrorToDisk(); }
            return null;
        }

        private class ThinkQuery
        {
            public string userMessageText { get; set; }
            public string source { get; set; }
            public bool isStartOfSession { get; set; }
            public string locale { get; set; } = "en-US";
            public string conversationId { get; set; } = null;
            public string[] optionsSets { get; set; } = new string[] { "chitchatv2_3" };
        }
    }
}
