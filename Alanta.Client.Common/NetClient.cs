using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using SlideLinc.Client.Common.Logging;

namespace SlideLinc.Client.Common
{
    public delegate void ProcessReceivedPacket(byte[] packet);

    public class NetClient : IDisposable
    {
        #region Fields
        private Socket mSocket;
        private DnsEndPoint mEndPoint;
        private string mHostname;
        private int mPort;
        private TimeSpan timeout = new TimeSpan(0, 0, 60);
        private const int receiveBufferSize = 1500;
        public event EventHandler<GenericEventArgs<string>> DataReceivedAsString;
        public event EventHandler<GenericEventArgs<byte[]>> DataReceivedAsByteArray;
        #endregion

        #region Public Methods
        public NetClient(string hostname, int port)
        {
            mHostname = hostname;
            mPort = port;
            mEndPoint = new DnsEndPoint(mHostname, mPort);
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Connects to the server in question.
        /// </summary>
        /// <remarks>Note that this should never be called from a UI thread, as it blocks the thread while waiting.</remarks>
        public void Connect()
        {
            SocketError result = SocketError.Success;
            if (!IsConnected)
            {
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                SocketAsyncEventArgs args = GetArgs();
                args.Completed += (s, e) =>
                    {
                        result = e.SocketError;
                        if (e.SocketError == SocketError.Success)
                        {
                            Logger.LogDebugMessage("Connected to {0} at port {1}.", mHostname, mPort);
                        }
                        resetEvent.Set();
                    };

                mSocket.ConnectAsync(args);
                if (!resetEvent.WaitOne(timeout))
                {
                    result = SocketError.TimedOut;
                }
                if (result != SocketError.Success)
                {
                    throw new NetClientException("Error receiving data; see InnerException for details.", new SocketException((int)result));
                }
            }
        }

        public void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
        }

        public void Send(byte[] message)
        {
            SocketError result = SocketError.Success;
            if (IsConnected)
            {
                Logger.LogDebugMessage("Sending message '{0}' to {1} on port {2}", message, mHostname, mPort);
                // ManualResetEvent resetEvent = new ManualResetEvent(false);
                SocketAsyncEventArgs sendArgs = GetArgs();
                sendArgs.SetBuffer(message, 0, message.Length);
                sendArgs.Completed += (s, e) =>
                {
                    result = e.SocketError;
                    if (e.SocketError == SocketError.Success)
                    {
                        Logger.LogDebugMessage("Send operation completed successfully.");
                    }
                    // resetEvent.Set();
                };
                mSocket.SendAsync(sendArgs);
                //if (!resetEvent.WaitOne(timeout))
                //{
                //    result = SocketError.TimedOut;
                //}
                //if (result != SocketError.Success)
                //{
                //    throw new NetClientException("Error sending data; see InnerException for details.", new SocketException((int)result));
                //}
            }
        }

        public void Receive()
        {
            Receive(null);
        }

        public void Receive(Action<string> callback)
        {
            if (IsConnected)
            {
                SocketAsyncEventArgs receiveArgs = GetArgs();
                receiveArgs.SetBuffer(new byte[receiveBufferSize], 0, receiveBufferSize);
                receiveArgs.Completed += (s, e) =>
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        RaiseDataReceived(e.Buffer, callback);
                    }
                };
                mSocket.ReceiveAsync(receiveArgs);
            }
            else
            {
                if (callback != null)
                {
                    callback(string.Empty);
                }
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                mSocket.Shutdown(SocketShutdown.Both);
                mSocket.Close();
            }
        }

        public bool IsConnected
        {
            get
            {
                return mSocket.Connected;
            }
        }

        #endregion

        #region Private Methods
        private SocketAsyncEventArgs GetArgs()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.UserToken = mSocket;
            args.RemoteEndPoint = mEndPoint;
            return args;
        }

        private void RaiseDataReceived(byte[] message, Action<string> callback)
        {
            if (DataReceivedAsString != null || callback != null)
            {
                string messageAsString = Encoding.UTF8.GetString(message, 0, message.Length).Split('\0')[0];
                if (DataReceivedAsString != null)
                {
                    DataReceivedAsString(this, new GenericEventArgs<string>(messageAsString));
                }
                if (callback != null)
                {
                    callback(messageAsString);
                }
            }
            if (DataReceivedAsByteArray != null)
            {
                DataReceivedAsByteArray(this, new GenericEventArgs<byte[]>(message));
            }
        }
        #endregion

        #region IDisposable Members

        private bool disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Disconnect();
                }
                disposed = true;
            }
        }

        #endregion
    }

    public class NetClientException : Exception
    {
        public NetClientException()
        {
        }

        public NetClientException(string message)
            : base(message)
        {
        }

        public NetClientException(string message, SocketException socketException)
            : base(message, socketException)
        {
        }
    }
}
