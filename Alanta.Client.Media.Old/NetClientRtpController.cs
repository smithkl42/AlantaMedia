using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text;
using System.Linq;
using System.Xml.Linq;

namespace Alanta.Client.Media
{
    // For testing purposes only.  It simulates locally the XML responses of the controller portion of our remote RTP-like media server.
    internal class NetClientRtpController : INetClient
    {

        private bool isConnected;
        private Action<byte[], int, int> processReceivedPacket;
        private const string Success = "Success";
        private const string Failed = "Failed";

        public string Host
        {
            get
            {
                return string.Empty;
            }
        }

        public int Port
        {
            get
            {
                return 0;
            }
        }


        #region INetClient Members

        public void Connect(Action<Exception> processConnection, Action<byte[], int, int> processReceivedPacket)
        {
            this.processReceivedPacket = processReceivedPacket;
            isConnected = true;
            processConnection(null);
        }

        public void Disconnect()
        {
            isConnected = false;
        }

        public bool IsConnected
        {
            get { return isConnected; }
        }

        public void Send(byte[] packet)
        {
            Send(packet, 0, packet.Length);
        }

        public void Send(byte[] packet, int offset, int length)
        {
            // Send back a message indicating the registration succeeded.
            string message = Encoding.UTF8.GetString(packet, offset, length).Split('\0')[0];
            XDocument commandDoc = XDocument.Parse(message);
            XDocument responseDoc = new XDocument();
            XElement responses = new XElement("Responses");
            responseDoc.AddFirst(responses);
            var commands = commandDoc.Root.Elements(); // .Descendants("Commands");
            foreach (XElement command in commands)
            {
                XElement response = new XElement("Response");
                response.Add(new XAttribute("Id", command.Attribute("Id").Value));
                response.Add(new XAttribute("Result", Success));
                responses.Add(response);
            }

            byte[] responsePacket = UTF8Encoding.UTF8.GetBytes(responseDoc.ToString());
            processReceivedPacket(responsePacket, 0, responsePacket.Length);
        }

        public void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
        }

        public void SetReceiveBuffer(byte[] buffer, int offset)
        {
            // No-op
        }

        public void SetReceiveBuffer(ByteStream buffer)
        {
            // No-op
        }

        #endregion
    }
}
