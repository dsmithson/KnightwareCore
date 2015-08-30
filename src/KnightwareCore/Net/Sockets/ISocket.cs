using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knightware.Net.Sockets
{
    public interface ISocket
    {
        bool IsRunning { get; }

        string ServerIP { get; }

        int ServerPort { get; }

        Task<bool> Startup(string serverIP, int serverPort);

        void Shutdown();
    }
}
