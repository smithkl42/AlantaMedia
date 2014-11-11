using System.Diagnostics;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
    public enum RtpPacketType
    {
        Connect = 0,
        Stream = 1
    }

    public enum RtpParseResult
    {
        Success,
        DataIncomplete,
        DataInvalid
    }

    public abstract class RtpPacket
    {
        #region Fields and Properties
        public RtpPacketType PacketType { get; protected set; }
        public const int RtpVersion = 2;
        public static readonly byte[] Preamble = { 0xFF, 0x00, 0xFF, 0x00 };
        public static readonly byte[] Suffix = { 0xAA, 0xAA };
        public byte[] PacketSuffix { get; protected set; }
        #endregion

        #region Constructors

    	protected RtpPacket()
        {
            PacketSuffix = new byte[Suffix.Length];
        }
        #endregion

        #region Methods

        public abstract RtpParseResult ParsePacket(ByteStream buffer);
        public abstract byte[] BuildPacket();
        public abstract bool TryBuildPacket(ByteStream packetBuffer);
        public abstract int GetPacketSize();

        public virtual bool IsValid()
        {
            bool isValid = true;
            for (int i = 0; i < Suffix.Length; i++)
            {
                if (PacketSuffix[i] != Suffix[i])
                {
                    isValid = false;
                    break;
                }
            } 
            if (!isValid)
            {
                ClientLogger.Debug("Packet suffix doesn't match.");
            }
            return isValid;
        }

        protected static bool FindNextPreamble(byte[] data, ref int offset)
        {
            // This brute-force algorithm is likely to be the fastest, since most of the time the preamble will be the first four bytes starting at position.
            for (; offset < data.Length - Preamble.Length; offset++)
            {
                if (IsPreamble(data, offset))
                {
                    offset += Preamble.Length;
                    return true;
                }
            }
            return false;
        }

        protected static bool IsPreamble(byte[] data, int offset)
        {
            bool found = true;
            for (int j = 0; j < Preamble.Length; j++)
            {
                if (data[offset + j] != Preamble[j])
                {
                    found = false;
                    break;
                }
            }
            return found;
        }

        #endregion Methods
    }
}
