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
    public class UDPSocket : IUDPSocket
    {
        private Stack<TaskCompletionSource<byte[]>> messageReceiptAwaiters;
        private Socket socket;
        private IPAddress server;

        public bool IsRunning
        {
            get;
            private set;
        }

        public string ServerIP
        {
            get;
            private set;
        }

        public int ServerPort
        {
            get;
            private set;
        }

        public event DataReceivedHandler DataReceived;
        protected void OnDataReceived(DataReceivedEventArgs e)
        {
            if(DataReceived != null)
                DataReceived(this, e);
        }

        public UDPSocket()
        {
        }

        public async Task<bool> StartupAsync(string serverIP, int serverPort)
        {
            await ShutdownAsync();
            this.IsRunning = true;
            this.ServerIP = serverIP;
            this.ServerPort = serverPort;

            if (string.IsNullOrEmpty(serverIP) || !IPAddress.TryParse(serverIP, out server))
            {
                await ShutdownAsync();
                return false;
            }
            
            messageReceiptAwaiters = new Stack<TaskCompletionSource<byte[]>>();
            
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            if (server.Equals(IPAddress.Broadcast))
            {
                socket.EnableBroadcast = true;
            }
            
            if (!BeginReceive())
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
                socket.Dispose();
                socket = null;
            }

            messageReceiptAwaiters = null;
            return Task.FromResult(true);
        }

        private bool BeginReceive(SocketAsyncEventArgs args = null)
        {
            if (socket == null || !IsRunning)
                return false;

            try
            {
                if (args == null)
                {
                    args = new SocketAsyncEventArgs()
                    {
                        RemoteEndPoint = new IPEndPoint(server, ServerPort)
                    };
                    byte[] buffer = new byte[1500];
                    args.SetBuffer(buffer, 0, buffer.Length);
                    args.Completed += socket_DataReceived;
                }
                return socket.ReceiveFromAsync(args);
            }
            catch (Exception ex)
            {
                if(!IsRunning)
                    TraceQueue.Trace(this, TracingLevel.Warning, "{0} ocurred while trying to begin receiving data: {1}", ex.GetType().Name, ex.Message);

                return false;
            }
        }

        private void socket_DataReceived(object sender, SocketAsyncEventArgs e)
        {
            if (!IsRunning || socket == null || e?.BytesTransferred <= 0)
                return;

            try
            {
                TaskCompletionSource<byte[]> tcs = null;
                lock (messageReceiptAwaiters)
                {
                    if (messageReceiptAwaiters.Count > 0)
                    {
                        tcs = messageReceiptAwaiters.Pop();
                    }
                }

                byte[] buffer = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, 0, buffer, 0, buffer.Length);

                //Return result to any retrieve awaiters
                if (tcs != null)
                {
                    tcs.TrySetResult(buffer);
                }

                var remoteEP = (IPEndPoint)e.RemoteEndPoint;
                OnDataReceived(new DataReceivedEventArgs(remoteEP.Address.ToString(), buffer));
            }
            catch (Exception ex)
            {
                TraceQueue.Trace(this, TracingLevel.Warning, "{0} occurred while receiving UDP data: {1}", ex.GetType().Name, ex.Message);
            }
            finally
            {
                BeginReceive(e);
            }
        }

        public Task<bool> SendDataAsync(byte[] buffer, int startIndex, int length)
        {
            if (!IsRunning || socket == null)
                return Task.FromResult(false);

            var tcs = new TaskCompletionSource<bool>();
            var args = new SocketAsyncEventArgs()
            {
                UserToken = tcs,
                RemoteEndPoint = new IPEndPoint(server, ServerPort)
            };
            args.SetBuffer(buffer, startIndex, length);
            args.Completed += socket_SendCompleted;
            if (!socket.SendToAsync(args))
            {
                //Completed synchronously
                socket_SendCompleted(socket, args);
            }

            return tcs.Task;
        }

        void socket_SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (!IsRunning || socket == null)
                return;

            bool success = (e.SocketError == SocketError.Success);
            var tcs = (TaskCompletionSource<bool>)e.UserToken;
            tcs.TrySetResult(success);
        }

        public async Task<byte[]> RetrieveDataAsync(byte[] txBuffer, int startIndex, int length, TimeSpan timeout)
        {
            //Queue for receipt of message immediately
            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
            lock (messageReceiptAwaiters)
            {
                messageReceiptAwaiters.Push(tcs);
            }

            //Try to send our data
            if (!await SendDataAsync(txBuffer, startIndex, length))
                return null;

            //Wait for response
            Task timeoutTask = Task.Delay(timeout);
            await Task.WhenAny(timeoutTask, tcs.Task);

            //Did we get a response?
            if (tcs.Task.Exception == null && tcs.Task.Status == TaskStatus.RanToCompletion)
                return tcs.Task.Result;
            else
            {
                messageReceiptAwaiters.Pop();
                return null;
            }
        }
    }
}
