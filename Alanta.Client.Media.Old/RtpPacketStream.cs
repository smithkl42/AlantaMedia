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
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Alanta.Client.Media
{
    public enum RtpPayloadType
    {
        Audio = 0,
        Video = 1
    }

    public class RtpPacketStream : RtpPacket
    {
        #region Fields and Properties
        private const int RtpVersion = 1;
        private const int RtpHeaderLength = 7;
        private const int RtpPayloadMaxLength = MediaConstants.MaxPayloadSize; // sizeof(RtpPayload) doesn't work
        private const int RtpPacketMaxLength = RtpPayloadMaxLength + RtpHeaderLength;
        private static int errorCount = 0;
        private int packetsProcessed = 0;
        private long totalPacketSum = 0;
        public byte[] Payload { get; set; }
        public ushort PayloadLength { get; set; }
        public ushort SsrcId { get; private set; }
        public ushort SequenceNumber { get; set; }
        public RtpPayloadType PayloadType { get; set; }
        #endregion

        #region Constructors
        public RtpPacketStream()
        {
            PacketType = RtpPacketType.Stream;
            Payload = new byte[RtpPayloadMaxLength];
        }

        public static List<RtpPacketStream> GetPacketsFromData(byte[] data)
        {
            try
            {
                var packets = new List<RtpPacketStream>(10);
                // Debug.Assert(data.Length <= RtpPacketMaxLength, "The length of the packet was larger than the max packet length.");
                int position = 0;
                while (position < data.Length)
                {
                    RtpPacketStream packet = new RtpPacketStream();
                    packet.ParsePacket(data, ref position);
                    packets.Add(packet);
                }
                return packets;
            }
            catch (System.Exception ex)
            {
                errorCount++;
                if (errorCount % 10 == 0)
                {
                    Debug.WriteLine(ex.ToString());
                }
                throw new InvalidOperationException("Unable to parse the media stream because unexpected data was encountered.", ex);
            }
        }

        #endregion

        public override void ParsePacket(byte[] data, ref int startPosition)
        {
            var packet = this;
            packet.PacketType = (RtpPacketType)ReadByteFromPacket(data, ref startPosition); // position changes from 0 to 1
            packet.PayloadType = (RtpPayloadType)ReadUShortFromPacket(data, ref startPosition); // position changes from 1 to 3
            packet.PayloadLength = ReadUShortFromPacket(data, ref startPosition); // position changes from 3 to 5

            if (packet.PacketType != RtpPacketType.Stream)
            {
                Debug.WriteLine("The PacketType must be a Stream packet.");
            }
            if (packet.PayloadType != RtpPayloadType.Audio && packet.PayloadType != RtpPayloadType.Video)
            {
                Debug.WriteLine("The PayloadType was the wrong type.");
            }
            if (packet.PayloadLength == 0 || packet.PayloadLength > MediaConstants.MaxPayloadSize || packet.PayloadLength + startPosition > data.Length)
            {
                Debug.WriteLine("The PayloadLength of {0} was outside the expected range.", packet.PayloadLength);
            }

            packet.SequenceNumber = ReadUShortFromPacket(data, ref startPosition); // position changes from 5 to 7
            if (packet.PayloadType == RtpPayloadType.Video)
            {
                packet.SsrcId = ReadUShortFromPacket(data, ref startPosition); // position changes from 7 to 9
            }
            packet.Payload = new byte[packet.PayloadLength];
            Buffer.BlockCopy(data, startPosition, packet.Payload, 0, packet.PayloadLength);
            startPosition += packet.PayloadLength;
        }

        public override byte[] BuildPacket()
        {
            byte[] packet = new byte[RtpHeaderLength + Payload.Length];
            int position = 0;
            WriteByteToPacket(packet, (byte)PacketType, ref position);
            WriteUShortToPacket(packet, (ushort)PayloadType, ref position);
            WriteUShortToPacket(packet, (ushort)Payload.Length, ref position);
            WriteUShortToPacket(packet, SequenceNumber, ref position);
            Buffer.BlockCopy(Payload, 0, packet, position, Payload.Length);
            #region Debug
            //#if DEBUG
            //            if (firstPayloadSent == null)
            //            {
            //                firstPayloadSent = rtpStreamPacket.Payload.Payload;
            //            }
            //            sentPackets++;
            //            if (sentPackets % 1000 == 0)
            //            {
            //                DisplayPayloadData(rtpStreamPacket.Payload.Payload, "Sending packet");
            //            }
            //#endif
            #endregion
            return packet;
        }

        private void DisplayPayloadData(byte[] payload, string description)
        {
            int total = 0;
            int nonZeroBytes = 0;
            packetsProcessed++;
            foreach (byte b in payload)
            {
                total += b;
                if (b != 0) nonZeroBytes++;
            }
            float percentNonZero = (float)nonZeroBytes / (float)payload.Length;
            totalPacketSum += total;
            float average = (float)totalPacketSum / (float)packetsProcessed;
            Debug.WriteLine("{0}: length={1}, sum={2}, average sum={3}, % nonzero bytes={4:0%}", description, payload.Length, total, average, percentNonZero);
        }
    }
}
