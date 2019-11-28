using System.Threading.Tasks;

namespace Knightware.Net.Sockets
{
    public interface ISocket
    {
        bool IsRunning { get; }

        string ServerIP { get; }

        int ServerPort { get; }

        Task<bool> StartupAsync(string serverIP, int serverPort);

        Task ShutdownAsync();
    }
}
