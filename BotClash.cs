using System.Linq;
using WebSocketSharp;
using System.Threading;
using TR = System.Timers;
using WebSocketSharp.Net;
using Newtonsoft.Json.Linq;
using WebSocketSharp.Server;
using Chatbot.Server.BotFactory;
using System.Collections.Generic;

namespace Chatbot.Server
{
    public class Clash : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            if (!HasCookie("X-Auth", out string Val) || !Val.Equals(Utils.AuthKEY))
                Context.WebSocket.CloseAsync(3001, "Access Code Is Invalid!");
            else
            {
                var Starter = HasCookie("X-Start", out string ChatInit) ? ChatInit : "Hello.";
                if (HasCookie("X-Session", out string Sess) && ClashManager.Sessions.ContainsKey(Sess))
                {
                    if (ClashManager.Sessions.ContainsKey(Sess))
                        ClashManager.Sessions[Sess].ReSetSession(Context.WebSocket);
                    else Context.WebSocket.Close(3004, "Session Is Invalid Or Has Expired!");
                    return;
                }
                else if (!HasCookie("X-Bots", out string BIds) || BIds.Length != 3 || !BIds.Contains("-"))
                    Context.WebSocket.Close(3002, "You Have Issued A Malformed Request!");
                else new ClashManager(ID, BIds, Starter, Context.WebSocket);
            }

            base.OnOpen();
        }

        private bool HasCookie(string Search, out string Value)
        {
            Value = null;
            foreach (Cookie _Cookie in Context.CookieCollection)
                if (_Cookie.Name == Search)
                {
                    Value = _Cookie.Value;
                    return true;
                }
            return false;
        }
    }

    public class ClashManager
    {
        private readonly TR.Timer Continuer;
        private readonly List<Chat> Chats;
        private readonly TR.Timer Cleaner;
        private IBotBase FstBot, SecBot;
        private bool IsChatting = true;
        private WebSocket _WSC = null;
        private Chat NextChat = null;
        private readonly string CId;
        private float Speed = 3;

        private WebSocket WSClient
        {
            get => _WSC;
            set
            {
                _WSC = value;
                _WSC.OnError += WS_OnError;
                _WSC.OnClose += WS_OnClose;
                _WSC.OnMessage += WS_OnMessage;
            }
        }

        private readonly ManualResetEvent PauseSync = new ManualResetEvent(false);
        public static IReadOnlyDictionary<ushort, string> CloseCodes = new Dictionary<ushort, string>
        {
            { 1000, "Normal Closure" }, { 1001, "Going Away" }, { 1002, "Protocol Error" }, { 1003, "Close Unsupported" },
            { 1004, "[* RESERVED *]" }, { 1005, "[!] Normal Close" }, { 1006, "Abnormal Closure" }, { 1007, "Payload Error" },
            { 1008, "Policy Violation" }, { 1009, "Too Large Closure" }, { 1010, "Mandory Extension" }, { 1011, "Server Error" },
            { 1012, "Service Restart" }, { 1013, "Try Again Later" }, { 1014, "Bad Gateway" }, { 1015, "TLS Handshake Failed" },
        };
        public static Dictionary<string, ClashManager> Sessions { get; } = new Dictionary<string, ClashManager>();

        public ClashManager(string _Id, string BotIds, string _ConvS, WebSocket _WS)
        {
            CId = _Id;
            Cleaner = new TR.Timer();
            Chats = new List<Chat>(); WSClient = _WS;
            if (!Sessions.ContainsKey(CId)) Sessions.Add(CId, this);
            Continuer = new TR.Timer(Speed * 1000) { AutoReset = false };
            Continuer.Elapsed += (_, __) => { Continuer.Close(); ContinueChats(); };
            Cleaner.Elapsed += (_, __) => { Sessions.Remove(CId); Chats.Clear(); Cleaner.Dispose(); }; PopulateBots(BotIds, _ConvS);
        }

        private void PopulateBots(string BotIds, string ChtMSG)
        {
            Chats.Clear();
            string[] Bots = BotIds.Split('-');
            var TempStore = new List<IBotBase>();

            foreach (var BIds in Bots)
                switch (BIds)
                {
                    case "R": TempStore.Add(new RoseBot()); break;
                    case "B": TempStore.Add(new BingBot()); break;
                    case "C": TempStore.Add(new CleverBot()); break;
                    case "P": TempStore.Add(new PandoraBot()); break;
                    default: WSClient.Close(3003, "Bots Field Is Invalid!"); return;
                }

            NextChat = new Chat { Message = ChtMSG };
            FstBot = TempStore[0];
            SecBot = TempStore[1];
            Continuer.Start();
        }

        public void ReSetSession(WebSocket _NewWS)
        {
            Cleaner.Stop();
            IsChatting = true;
            WSClient = _NewWS;
            ContinueChats();
        }

        private async void ContinueChats()
        {
            if (NextChat == null || !WSClient.IsAlive) return;
            
            if (IsChatting)
            {
                string Reply; int Retry = 0; ReGet:;
                var Thinker = NextChat.IsFirst ? FstBot : SecBot;
                Reply = await Thinker.Think(NextChat.Message);

                if (Reply == null)
                {
                    if (Retry < 3) { Retry++; goto ReGet; }
                    else WSClient.Close(3006, "Internal API Error Occured!");
                }
                else if (Chats.Count() > 0 && Chats.Last()?.Message == Reply)
                    Reply = "It looks like we are going off topic!";

                var NChat = new Chat
                {
                    Message = Reply,
                    BotName = Thinker.Name,
                    IsFirst = !NextChat.IsFirst
                };

                NextChat = NChat;
                Chats.Add(NChat);
                Continuer.Start();
                Send(new { Action = "Chat", Talk = NChat });
                return;
            }

            Send(new { Action = "State", Type = "Paused" });
            PauseSync.WaitOne(); Continuer.Start();
            Send(new { Action = "State", Type = "Resumed" });
        }

        private void WS_OnClose(object _, CloseEventArgs WClsArgs)
        {
            Continuer.Stop();
            IsChatting = false;
            WSClient.OnError -= WS_OnError;
            WSClient.OnClose -= WS_OnClose;
            WSClient.OnMessage -= WS_OnMessage;
            Cleaner.Interval = 3600000; Cleaner.Start();
            this.LogInfo($"Client-Disconnect\r\nCode => {WClsArgs.Code} || Reason => {WClsArgs.Reason}");
        }

        private void WS_OnError(object _, ErrorEventArgs WErArgs)
            => WErArgs.Exception.ErrorToDisk();

        private void WS_OnMessage(object _, MessageEventArgs WMsgArgs)
        {
            try
            {
                var Request = JObject.Parse(WMsgArgs.Data);

                switch ((string)Request["Action"])
                {
                    case "Speed":

                        float NewSpeed = (float)Request["Value"];
                        bool IsChangeable = NewSpeed != Speed && NewSpeed <= 10 && NewSpeed >= 2;
                        if (!IsChangeable) Send(new { Action = "Speed", HasChanged = false, MySpeed = (float)Request["MySpeed"] });
                        else
                        {
                            Speed = NewSpeed;
                            Continuer.Interval = Speed * 1000;
                            Send(new { Action = "Speed", HasChanged = true });
                        }
                        break;

                    case "State":

                        IsChatting = !(bool)Request["IsPause"];
                        if (IsChatting) { PauseSync.Set(); PauseSync.Reset(); }
                        break;

                    case "Bot-Change":

                        var BIds = (string)Request["Bots"];
                        bool IsBotable = BIds.Length == 3 && BIds.Contains('-');
                        Send(new { Action = "Bot-Change", BotsChanged = IsBotable });

                        if (IsBotable)
                        {
                            IsChatting = true;
                            PopulateBots(BIds, Request["Msg"].Type == JTokenType.Null ? "Hello." : (string)Request["Msg"]);
                        }
                        break;

                    case "GetChat":

                        if (Chats.Count() > 0)
                        {
                            var TCL = new List<string>();
                            foreach (var CItem in Chats)
                            {
                                TCL.Add($"==========> [{CItem.BotName}] <==========");
                                TCL.Add(CItem.Message);
                                TCL.Add("---------------|---------------");
                                TCL.Add(string.Empty);
                            }

                            var PlainChat = System.Text.Encoding.UTF8.GetBytes(string.Join("\r\n", TCL));
                            var BaseDChat = System.Convert.ToBase64String(PlainChat);
                            Send(new { Action = "Download", HasData = true, Data = BaseDChat });
                        }
                        else Send(new { Action = "Download", HasData = false });
                        break;
                }
            }
            catch { }
        }

        private void Send(object ToSend)
        {
            try
            {
                if (WSClient.IsAlive)
                    WSClient.Send(JObject.FromObject(ToSend)
                        .ToString(Newtonsoft.Json.Formatting.None));
            }
            catch { }
        }

        private class Chat
        {
            public string BotName { get; set; }
            public string Message { get; set; } = "Hi";
            [Newtonsoft.Json.JsonProperty("IsRight")] public bool IsFirst { get; set; } = true;
        }
    }
}
