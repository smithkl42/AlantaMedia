
using Alanta.Client.Common.Logging;
namespace Alanta.Client.Media
{
	public class RtpPacketConnect : RtpPacket
	{
		#region Fields and Properties
		private const int HeaderLength = 5;
		public static int MaxPacketSize;
		public ushort Version { get; set; }
		public ushort SsrcId { get; set; }
		#endregion

		#region Constructors
		static RtpPacketConnect()
		{
			MaxPacketSize = Preamble.Length + HeaderLength + Suffix.Length;
		}
		public RtpPacketConnect()
		{
			PacketType = RtpPacketType.Connect;
			Version = RtpVersion;
		}
		#endregion

		#region Methods
		public override RtpParseResult ParsePacket(ByteStream packet)
		{
			// Find and validate the preamble so we know we've got a real packet.
			if (packet.RemainingBytes < Preamble.Length)
			{
				return RtpParseResult.DataIncomplete;
			}
			if (!FindNextPreamble(packet.Data, ref packet.CurrentOffset))
			{
				return RtpParseResult.DataIncomplete;
			}

			// Read the header.
			PacketType = (RtpPacketType)packet.ReadByte();
			Version = packet.ReadUInt16Network();
			SsrcId = packet.ReadUInt16Network();

			packet.TryReadBytes(PacketSuffix, 0, Suffix.Length);

			// Confirm that the packet is valid.
			return IsValid() ? RtpParseResult.DataInvalid : RtpParseResult.Success;
		}

		public override byte[] BuildPacket()
		{
			var packet = new ByteStream(new byte[GetPacketSize()]);
			return TryBuildPacket(packet) ? packet.Data : null;
		}

		public override bool TryBuildPacket(ByteStream packetBuffer)
		{
			// Make sure there's enough room.
			int start = packetBuffer.CurrentOffset;
			if (start + GetPacketSize() > packetBuffer.RemainingBytes)
			{
				return false;
			}

			// Write the preamble.
			packetBuffer.TryWriteBytes(Preamble, 0, Preamble.Length);

			// Write the header.
			packetBuffer.WriteByte((byte)PacketType);
			packetBuffer.WriteUInt16Network(Version);
			packetBuffer.WriteUInt16Network(SsrcId);

			// Write the Suffix.
			packetBuffer.TryWriteBytes(Suffix, 0, Suffix.Length);

			// Return the size of the data written.
			packetBuffer.DataLength = packetBuffer.CurrentOffset - start;
			return true;
		}

		public override int GetPacketSize()
		{
			return Preamble.Length + HeaderLength + PacketSuffix.Length;
		}

		public override bool IsValid()
		{
			bool isValid = base.IsValid() &&
				PacketType == RtpPacketType.Connect &&
				Version == RtpVersion;
			if (!isValid)
			{
				ClientLogger.Debug("RtpPacketConnect is not valid!");
			}
			return isValid;
		}
		#endregion
	}
}
