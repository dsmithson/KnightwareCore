using System.Threading.Tasks;

namespace Knightware.Net.Sockets
{
    public interface IMulticastListener
    {
        bool IsRunning { get; }
        string MulticastIP { get; }
        int MulticastPort { get; }

        event DataReceivedHandler DataReceived;

        Task<bool> StartupAsync(string multicastIP, int multicastPort);
        Task ShutdownAsync();
    }
}
