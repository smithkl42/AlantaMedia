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
using System.Diagnostics;

namespace Alanta.Client.Media
{
    // For testing purposes only.  It simulates locally the responses of the media portion of our remote RTP-like server.
    internal class NetClientRtpReflector : INetClient
    {

        bool isConnected;
        private Action<byte[], int, int> processReceivedPacket;

        #region INetClient Members

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
            if (packet[4] != (byte)RtpPacketType.Connect)
            {
                // RtpPayloadType type = (RtpPayloadType)IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(packet, 1));

                RtpPayloadType type = (RtpPayloadType)BitConverter.ToUInt16(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(packet, 5))), 2);

                // If it's an audio packet, just return it unchanged.
                Debug.Assert(type == RtpPayloadType.VideoFromClient || type == RtpPayloadType.Audio, "The PayloadType must be either audio or video.");
                if (type == RtpPayloadType.Audio)
                {
                    processReceivedPacket(packet, 0, length);
                }
                else if (type == RtpPayloadType.VideoFromClient)
                {
                    // If it's a video packet, return three slightly modified copies of the packet.
                    for (ushort i = 0; i < VideoConstants.NumLoopbackCameras; i++)
                    {
                        byte[] updatedPacket = TransformClientVideoPacket(packet, offset, length, i);
                        processReceivedPacket(updatedPacket, 0, updatedPacket.Length);
                    }
                }
            }
        }

        private byte[] TransformClientVideoPacket(byte[] packet, int offset, int length, ushort ssrcId)
        {
            // Make a copy of the incoming packet.
            ByteStream buffer = new ByteStream(length);
            Buffer.BlockCopy(packet, offset, buffer.Data, buffer.CurrentOffset, buffer.DataLength);

            // Update the payload type.
            buffer.CurrentOffset = RtpPacket.Preamble.Length + sizeof(byte);
            buffer.WriteUInt16Network((ushort)RtpPayloadType.VideoFromServer);

            // Get the bytes to insert.
            ByteStream ssrcIdByteStream = new ByteStream(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(ssrcId)), 2, 2);

            // Figure out where to insert them.
            int totalHeaderSize = RtpPacket.Preamble.Length + RtpPacketData.HeaderLength; // 11 = 4 bytes for the preamble + 7 bytes for the header.
            buffer.CurrentOffset = totalHeaderSize;

            // Insert them.
            buffer.InsertBytes(ssrcIdByteStream);

            return buffer.Data;
        }

        public void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
        }

        #endregion
    }
}
