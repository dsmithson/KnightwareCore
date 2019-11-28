using System;

namespace Knightware.Net.Sockets
{
    public delegate void DataReceivedHandler(object sender, DataReceivedEventArgs e);

    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public int Length { get; set; }
        public string SenderAddress { get; set; }

        public DataReceivedEventArgs()
        {
        }

        public DataReceivedEventArgs(string senderAddress, byte[] data)
            : this(senderAddress, data, data.Length)
        {

        }

        public DataReceivedEventArgs(string senderAddress, byte[] data, int length)
        {
            this.SenderAddress = senderAddress;
            this.Data = data;
            this.Length = length;
        }
    }
}
