using System.Threading.Tasks;

namespace Chatbot.Server
{
    internal interface IBotBase
    {
        string Name { get; }
        Task<string> Think(string TQuery);
    }
}
