using System;
using System.Xml;
using System.Net.Http;
using System.Threading.Tasks;

namespace Chatbot.Server.BotFactory
{
    public class PandoraBot : IBotBase
    {
        readonly string Session;
        public string Name => "Pandora-Bot";
        private Uri ApiURI = new Uri("https://www.pandorabots.com");
        public PandoraBot() { Session = Utils.MD5Y(Guid.NewGuid().ToString()).ToLower(); }

        public async Task<string> Think(string TQuery)
        {
            try
            {
                bool HasSignaled = false; REAnalyze:;
                var AckMsg = new HttpRequestMessage(HttpMethod.Get, "/pandora/talk-xml?botid=b0dafd24ee35a477" +
                    $"&input={Uri.EscapeDataString(TQuery)}&custid={Session}");
                AckMsg.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit" +
                        "/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36 Edg/94.0.992.50");
                var Reply = await (await new HttpClient() { BaseAddress = ApiURI }.SendAsync(AckMsg)).Content.ReadAsStringAsync();
                var XMLDocRDR = new XmlDocument(); XMLDocRDR.LoadXml(Reply);
                var RNode = XMLDocRDR.SelectSingleNode("/result/that");

                if (RNode != null)
                {
                    var Retable = Normalize(RNode.InnerText);
                    return Retable;
                }
                else if (!HasSignaled)
                {
                    await Task.Delay(2000);
                    HasSignaled = true;
                    goto REAnalyze;
                }
            }
            catch (Exception E) { E.ErrorToDisk(); }
            return null;
        }

        private string Normalize(string WhiteText)
        {
            WhiteText = WhiteText.Trim();
            bool IsPrevWhitespaced = false;
            var NewTxt = new System.Text.StringBuilder();

            for (int I = 0; I < WhiteText.Length; I++)
            {
                if (char.IsWhiteSpace(WhiteText[I]))
                {
                    if (IsPrevWhitespaced) continue;
                    IsPrevWhitespaced = true;
                }
                else IsPrevWhitespaced = false;
                NewTxt.Append(WhiteText[I]);
            }

            return NewTxt.ToString();
        }
    }
}
