using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Knightware.Diagnostics;
using Knightware.Net.Sockets;

namespace Knightware.Net
{
    public class UDPMulticastListener : IMulticastListener
    {
        private Socket socket;

        public bool IsRunning
        {
            get;
            private set;
        }

        public string MulticastIP
        {
            get;
            private set;
        }

        public int MulticastPort
        {
            get;
            private set;
        }

        public event DataReceivedHandler DataReceived;
        protected void OnDataReceived(DataReceivedEventArgs e)
        {
            if (DataReceived != null)
                DataReceived(this, e);
        }

        public async Task<bool> StartupAsync(string multicastIP, int multicastPort)
        {
            await ShutdownAsync();
            IsRunning = true;

            IPAddress serverIP;
            if (string.IsNullOrEmpty(multicastIP) || !IPAddress.TryParse(multicastIP, out serverIP))
            {
                await ShutdownAsync();
                return false;
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            //Extrememly important to bind the socket BEFORE joing the multicast group
            socket.Bind(new IPEndPoint(IPAddress.Any, 11118));
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(serverIP, IPAddress.Any));

            if (!BeginListening())
            {
                await ShutdownAsync();
                return false;
            }
            return true;
        }

        public Task ShutdownAsync()
        {
            IsRunning = false;

            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
                socket = null;
            }

            return Task.FromResult(true);
        }

        private bool BeginListening()
        {
            if (!IsRunning || socket == null)
                return false;

            EndPoint sender = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = new byte[1500];
            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref sender, new AsyncCallback(socket_DataReceived), buffer);
            return true;
        }

        private void socket_DataReceived(IAsyncResult ar)
        {
            if (!IsRunning || socket == null)
                return;

            try
            {
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, MulticastPort);
                int count = socket.EndReceiveFrom(ar, ref remoteEP);
                if (count > 0)
                {
                    byte[] buffer = (byte[])ar.AsyncState;
                    OnDataReceived(new DataReceivedEventArgs()
                    {
                        Data = buffer,
                        Length = count,
                        SenderAddress = ((IPEndPoint)remoteEP).Address.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                TraceQueue.Trace(this, TracingLevel.Warning, "{0} occurred while processing UDP incoming data: {1}", ex.GetType().Name, ex.Message);
            }
            finally
            {
                //Setup for next packet
                BeginListening();
            }
        }
    }
}
