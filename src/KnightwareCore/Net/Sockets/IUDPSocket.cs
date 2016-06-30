using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knightware.Net.Sockets
{
    /// <summary>
    /// Generic interface for a socket supporting synchronous communication
    /// </summary>
    public interface IUDPSocket : ISocket
    {
        event DataReceivedHandler DataReceived;

        Task<byte[]> RetrieveDataAsync(byte[] txBuffer, int startIndex, int length, TimeSpan timeout);

        Task<bool> SendDataAsync(byte[] buffer, int startIndex, int length);
    }
}
