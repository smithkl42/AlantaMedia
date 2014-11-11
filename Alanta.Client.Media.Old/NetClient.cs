using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
    public class NetClient : INetClient
    {
        #region Fields and Properties

        private const int DefaultReceiveBufferSize = VideoConstants.MaxPayloadSize*10;
            // Leave room for something like 10 packets.

        private readonly DnsEndPoint _endPoint;
        private readonly NetClientLogger logger;

        private readonly ObjectPool<SocketAsyncEventArgs> receiveArgsRecycler;
        private readonly ObjectPool<SocketAsyncEventArgs> sendArgsPool;
        private Action<byte[], int, int> receiveCompleteHandler;
        private Socket socket;

        public bool IsConnected
        {
            get { return socket != null && socket.Connected; }
        }

        public string Host
        {
            get { return _endPoint.Host; }
        }

        public int Port
        {
            get { return _endPoint.Port; }
        }

        #endregion

        #region Constructors

        public NetClient(string host, int port, MediaStatistics stats = null)
        {
            _endPoint = new DnsEndPoint(host, port);
            receiveArgsRecycler = new ObjectPool<SocketAsyncEventArgs>(GetReceiveArgs);
            sendArgsPool = new ObjectPool<SocketAsyncEventArgs>(GetSendArgs);
            if (stats != null)
            {
                logger = new NetClientLogger(stats);
            }
        }

        #endregion

        #region Public Methods

        public void Connect(Action<Exception> processConnection, Action<byte[], int, int> receiveCompleteHandler)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.receiveCompleteHandler = receiveCompleteHandler;
            var args = GetArgs();
            args.UserToken = processConnection;

            // Disable the Nagle algorithm.
            // For our purposes, we want the data sent as quickly as possible, to reduce latency.
            // See http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.nodelay(VS.95).aspx
            socket.NoDelay = true;

            socket.ConnectAsync(args);
        }

        public void Disconnect()
        {
            try
            {
                if (socket != null)
                {
                    // ks 12/12/11 - We've been getting some 10057 errors here. The only way I can think that they might be happening
                    // is if two threads are trying to close the socket simultaneously. I've added in some locking
                    // code to help protect against that. We'll see if it makes any difference.
                    lock (socket)
                    {
                        if (socket.Connected)
                        {
                            ClientLogger.Debug("Disconnecting from " + _endPoint);
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ks 05/11/12 - Switched from ErrorException to DebugException, as swallowing the exception seems to
                // work just fine.
                ClientLogger.DebugException(ex, "Disconnecting failed");
            }
        }

        public void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
        }

        public void Send(Byte[] packet)
        {
            Send(packet, 0, packet.Length);
        }

        public void Send(byte[] packet, int offset, int length)
        {
            if (socket.Connected)
            {
                var sendArgs = sendArgsPool.GetNext();
                sendArgs.SetBuffer(packet, offset, length);
                socket.SendAsync(sendArgs);
                if (logger != null)
                {
                    logger.LogDataSent(length);
                }
            }
        }

        public void Connect(Action<byte[], int, int> processReceivedPacket)
        {
            Connect(null, processReceivedPacket);
        }

        #endregion

        #region Private Methods

        private SocketAsyncEventArgs GetReceiveArgs()
        {
            var args = GetArgs();
            args.SetBuffer(new byte[DefaultReceiveBufferSize], 0, DefaultReceiveBufferSize);
            return args;
        }

        private SocketAsyncEventArgs GetSendArgs()
        {
            return GetArgs();
        }

        private SocketAsyncEventArgs GetArgs()
        {
            var args = new SocketAsyncEventArgs();
            args.UserToken = socket;
            args.RemoteEndPoint = _endPoint;
            args.Completed += OnNetworkEvent;
            return args;
        }

        private void OnNetworkEvent(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    HandleConnectComplete(e);
                    break;
                case SocketAsyncOperation.Send:
                    HandleSendComplete(e);
                    break;
                case SocketAsyncOperation.Receive:
                    HandleReceiveComplete(e);
                    break;
            }
        }

        private void HandleConnectComplete(SocketAsyncEventArgs e)
        {
            SocketException exception = null;
            if (!socket.Connected)
            {
                exception = new SocketException((int) e.SocketError);
            }
            var processConnection = e.UserToken as Action<Exception>;
            if (processConnection != null)
            {
                processConnection(exception);
            }
            if (socket.Connected)
            {
                socket.ReceiveAsync(receiveArgsRecycler.GetNext());
            }
        }

        private void HandleSendComplete(SocketAsyncEventArgs e)
        {
            sendArgsPool.Recycle(e);
        }

        private void HandleReceiveComplete(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred == 0)
                {
                    ClientLogger.Debug("Remote socket closed for endpoint {0}", _endPoint);
                    Disconnect();
                }
                else
                {
                    receiveCompleteHandler(e.Buffer, e.Offset, e.BytesTransferred);
                    if (logger != null)
                    {
                        logger.LogDataReceived(e.BytesTransferred);
                    }
                    if (socket.Connected)
                    {
                        socket.ReceiveAsync(receiveArgsRecycler.GetNext());
                    }
                }
            }
            finally
            {
                receiveArgsRecycler.Recycle(e);
            }
        }

        #endregion
    }
}