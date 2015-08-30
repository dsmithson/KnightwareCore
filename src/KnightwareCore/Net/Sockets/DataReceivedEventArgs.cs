using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knightware.Net.Sockets
{
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public int Length { get; set; }
        public string SenderAddress { get; set; }
    }
}
