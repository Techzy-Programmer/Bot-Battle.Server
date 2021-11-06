using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Chatbot.Server.BotFactory
{
    public class CleverBot : IBotBase
    {
        private int CCount = 1;
        private string XAI = "";
        private string Session = "";
        private bool HasStarted = false;

        public CleverBot()
        {
            CookieStore.Capacity = 1000;
            CookieStore.MaxCookieSize = 10240;
            CookieStore.PerDomainCapacity = 100;
        }

        public string Name => "Clever-Bot";
        private Uri ApiURI { get; } = new Uri("https://www.cleverbot.com");
        private LinkedList<string> History { get; } = new LinkedList<string>();
        private CookieContainer CookieStore { get; set; } = new CookieContainer();

        public async Task<string> Think(string TQuery)
        {
            if (!HasStarted) return await SelfSetUP(TQuery);
            else
            {
                try
                {
                    CCount++;
                    int Retry = 0;
                    string[] BotReply;

                    var TkParam = new Dictionary<string, string>
                    {
                        { "uc", "UseOfficialCleverbotAPI" },
                        { "out", Uri.EscapeDataString(History.ElementAt(0)) },
                        { "in", Uri.EscapeDataString(TQuery) },
                        { "bot", "c" },
                        { "cbsid", Session },
                        { "xai", $"WYD,{XAI}" },
                        { "ns", CCount.ToString() },
                        { "al", "" },
                        { "dl", "en" },
                        { "flag", "" },
                        { "user", "" },
                        { "mode", "1" },
                        { "alt", "0" },
                        { "reac", "" },
                        { "emo", "" },
                        { "sou", "website" },
                        { "xed", "" }
                    };

                    var CBStt = new List<string>();
                    int HisCNT = History.Count();
                    var TkPayLD = new Dictionary<string, string> { { "stimulus", Uri.EscapeDataString(TQuery) } };

                    for (int I = 0; I < HisCNT; I++)
                    {
                        TkPayLD.Add($"vText{I + 2}", Uri.EscapeDataString(History.ElementAt(I)));
                        CBStt.Add(Uri.EscapeDataString(History.ElementAt((HisCNT - 1) - I)));
                    }

                    CookieStore.SetCookies(ApiURI, $"CBSTATE=&&0&&0&{CCount - 1}&{string.Join("&", CBStt)}");
                    TkPayLD.Add("cb_settings_language", "en");
                    TkPayLD.Add("cb_settings_scripting", "no");
                    TkPayLD.Add("sessionid", Session);
                    TkPayLD.Add("islearning", "1");
                    TkPayLD.Add("icognoid", "wsf");

                ReThink:;
                    if ((BotReply = await SendPost(TkPayLD, TkParam)) != null && BotReply.Length > 2 && !BotReply[0].Contains("<html>"))
                    {
                        History.AddFirst(TQuery);
                        History.AddFirst(BotReply[0]);
                        XAI = BotReply[2];
                        return BotReply[0];
                    }
                    else if (Retry < 2)
                    {
                        await Task.Delay(2000);
                        Retry++; goto ReThink;
                    }
                }
                catch (Exception E) { E.ErrorToDisk(); }
                return null;
            }
        }
        
        private async Task<string> SelfSetUP(string FQry)
        {
            int Retry = 0;
            string[] ReplyST;
            CookieStore.SetCookies(ApiURI, "_cbsid=-1");
            CookieStore.SetCookies(ApiURI, "XVIS=TE1939AFFIAGAYQZ0RS10");
            var ReplyPost = new Dictionary<string, string>
            {
                { "stimulus", FQry },
                { "cb_settings_language", "en" },
                { "cb_settings_scripting", "no" },
                { "islearning", "1" },
                { "icognoid", "wsf" }
            };

            try
            {
            ReSet:;
                if ((ReplyST = await SendPost(ReplyPost, new Dictionary<string, string> { { "uc", "UseOfficialCleverbotAPI" } })) != null
                    && ReplyST.Length > 2 && !ReplyST[0].Contains("<html>"))
                {
                    History.Clear();
                    History.AddFirst(FQry);
                    History.AddFirst(ReplyST[0]);
                    Session = ReplyST[1];
                    XAI = ReplyST[2];

                    CookieStore.SetCookies(ApiURI, "XAI=WXB");
                    var AckMsg = new HttpRequestMessage(HttpMethod.Get, "/webservicemin?uc=UseOfficialCleverbotAPI" +
                        $"&out=&in={Uri.EscapeDataString(FQry)}&bot=c&cbsid={Session}&xai=WXB&ns=1" +
                        "&al=&dl=&flag=&user=&mode=1&alt=0&reac=&emo=&sou=website&xed=&");
                    AckMsg.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit" +
                            "/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36 Edg/94.0.992.50");
                    var GetHandler = new HttpClientHandler { CookieContainer = CookieStore };
                    await new HttpClient(GetHandler) { BaseAddress = ApiURI }.SendAsync(AckMsg);
                    HasStarted = true;
                    return ReplyST[0];
                }
                else if (Retry < 2)
                {
                    await Task.Delay(2000);
                    Retry++; goto ReSet;
                }
            }
            catch (Exception E) { E.ErrorToDisk(); }
            return null;
        }

        public IEnumerable<string> GetHistory(int HLimit = 0)
            => History.Reverse().Take(HLimit == 0 ? History.Count() : HLimit);

        private async Task<string[]> SendPost(Dictionary<string, string> Postable, Dictionary<string, string> Queryable)
        {
            var QueryStore = new List<string>();
            foreach (var QueryData in Queryable) QueryStore.Add
                    ($"{QueryData.Key}={Uri.EscapeDataString(QueryData.Value)}");
            string FinalQuery = string.Join("&", QueryStore);

            try
            {
                using (var HttpHandler = new HttpClientHandler() { CookieContainer = CookieStore, UseCookies = true })
                using (var HPoster = new HttpClient(HttpHandler) { BaseAddress = ApiURI })
                {
                    var HMsg = new HttpRequestMessage(HttpMethod.Post, $"/webservicemin?{FinalQuery}");
                    HMsg.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit" +
                        "/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36 Edg/94.0.992.50");
                    var MD5Raw = (await new FormUrlEncodedContent(Postable).ReadAsStringAsync()).Substring(7, 26);
                    if (!Postable.ContainsKey("icognocheck")) Postable.Add("icognocheck", Utils.MD5Y(MD5Raw));
                    HMsg.Content = new FormUrlEncodedContent(Postable);
                    var RMsg = await HPoster.SendAsync(HMsg);

                    var ICookie = HttpHandler.CookieContainer
                        .GetCookies(ApiURI).Cast<Cookie>();
                    using (var RespReader = new StreamReader(await RMsg
                        .Content.ReadAsStreamAsync(), Encoding.UTF8)) return RespReader.ReadToEnd()
                            .Trim().Split(new string[] { "\r" }, StringSplitOptions.None);
                }
            }
            catch (Exception E) { E.ErrorToDisk(); return null; }
        }
    }
}
