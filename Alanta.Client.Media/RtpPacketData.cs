using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	public enum RtpPayloadType
	{
		/// <summary>
		/// Audio (whether originating from client or the server)
		/// </summary>
		Audio = 0,

		/// <summary>
		/// Video (from client to the server)
		/// </summary>
		VideoFromClient = 1,

		/// <summary>
		/// video (from server to the client)
		/// </summary>
		VideoFromServer = 2
	}

	public class RtpPacketData : RtpPacket, IMediaPacket
	{
		#region Fields and Properties
		public const int HeaderLength = 7;
		public const int VideoInputHeaderLength = 2;
		public const int PayloadMaxLength = VideoConstants.MaxPayloadSize;
		public static int DataPacketMaxLength;

		/// <summary>
		/// A byte[] array containing the packet payload.
		/// </summary>
		public Array Payload { get; set; }

		/// <summary>
		/// A ByteStream containing the packet data.
		/// </summary>
		private readonly ByteStream _packetBuffer;

		/// <summary>
		/// The length of the payload in bytes.
		/// </summary>
		public ushort PayloadLength { get; set; }

		/// <summary>
		/// The SsrcId number identifying the source of the frame.
		/// </summary>
		public ushort SsrcId { get; private set; }

		/// <summary>
		/// The frame's sequence number.
		/// </summary>
		public ushort SequenceNumber { get; set; }

		/// <summary>
		/// The machine's last processor load
		/// </summary>
		public byte ProcessorLoad { get; set; }

		/// <summary>
		/// The payload type of the packet, whether audio, sending video or receiving video.
		/// </summary>
		public RtpPayloadType PayloadType { get; set; }

		/// <summary>
		/// The audio codec in use (only relevant to audio packets, otherwise ignored)
		/// </summary>
		public AudioCodecType AudioCodecType { get; set; }

		/// <summary>
		/// Whether the packet in question is silent (1) or not (0)
		/// </summary>
		public bool IsSilent { get; set; }
		#endregion

		#region Constructors

		static RtpPacketData()
		{
			DataPacketMaxLength = Preamble.Length + HeaderLength + VideoInputHeaderLength + PayloadMaxLength + Suffix.Length;
		}

		public RtpPacketData()
		{
			PacketType = RtpPacketType.Stream;
			Payload = new byte[PayloadMaxLength];
			_packetBuffer = new ByteStream(DataPacketMaxLength);
		}

		public static List<RtpPacketData> GetPacketsFromData(ByteStream buffer, IObjectPool<List<RtpPacketData>> rtpPacketDataListPool, IObjectPool<RtpPacketData> rtpPacketDataPool)
		{
			// Retrieve the list from an object pool (rather than creating a new one each time that has to be garbage-collected).
			var packets = rtpPacketDataListPool.GetNext();

			while (buffer.CurrentOffset < buffer.EndOffset)
			{
				RtpPacketData packet = rtpPacketDataPool.GetNext();
				RtpParseResult result = packet.ParsePacket(buffer);

				switch (result)
				{
					case RtpParseResult.Success:
						// Add the packet to the list and continue processing.
						packets.Add(packet);
						break;
					case RtpParseResult.DataIncomplete:
						// Tell the calling function that it can reuse the buffer after position <length>.
						// Move the data to the beginning of the buffer (so that we can get it next time) and stop parsing.
						Debug.Assert(buffer.CurrentOffset >= 0, "The current offset cannot be negative.");
						Debug.Assert(buffer.RemainingBytes >= 0, "The remaining bytes cannot be negative.");
						Buffer.BlockCopy(buffer.Data, buffer.CurrentOffset, buffer.Data, 0, buffer.RemainingBytes);
						buffer.DataLength = buffer.RemainingBytes;
						buffer.DataOffset = 0;
						buffer.CurrentOffset = 0;
						return packets;
					case RtpParseResult.DataInvalid:
						// Don't add the current packet to the list, but continue processing at the current position.
						break;
				}
			}
			// If this is our exit point, tell the calling function that it can reuse the entire buffer.
			buffer.DataLength = 0;
			buffer.CurrentOffset = 0;
			return packets;
		}

		#endregion

		/// <summary>
		/// Parses a byte array and fills in the packet details.
		/// </summary>
		/// <param name="data">The ByteStream from which to pull the packet details.</param>
		/// <returns>
		/// RtpParseResult.Success if successful, 
		/// RtpParseResult.DataIncomplete if the data runs out before the packet has been parsed, 
		/// RtpParseResult.DataInvalid if it appears that the data has been corrupted in some fashion.
		/// </returns>
		public override RtpParseResult ParsePacket(ByteStream data)
		{
			// var packet = this;
			int startOffset = data.CurrentOffset;

			// Find and validate the preamble so we know we've got a real packet.
			if (data.RemainingBytes < Preamble.Length)
			{
				return RtpParseResult.DataIncomplete;
			}
			if (!FindNextPreamble(data.Data, ref data.CurrentOffset))// position normally changes from 0 to 4
			{
				return RtpParseResult.DataInvalid;
			}

			// Read and parse the header.
			int headerOffset = data.CurrentOffset;
			if (data.RemainingBytes < HeaderLength)
			{
				// If the amount of data remaining is less than absolutely required for a real packet, 
				// signal that we need to return the remaining data to the buffer.
				data.CurrentOffset = startOffset;
				return RtpParseResult.DataIncomplete;
			}
			PacketType = (RtpPacketType)data.ReadByte();
			PayloadType = (RtpPayloadType)data.ReadByte();

			byte audioData = data.ReadByte();
			AudioCodecType = (AudioCodecType)(audioData >> 4);
			IsSilent = (audioData & 0x0f) != 0;

			if (PayloadType == RtpPayloadType.Audio && !IsSilent && AudioCodecType != AudioCodecType.G711M && AudioCodecType != AudioCodecType.Speex)
			{
				ClientLogger.Debug("Unexpected codec type: {0}", AudioCodecType);
			}

			PayloadLength = data.ReadUInt16Network();
			SequenceNumber = data.ReadUInt16Network();

			// Parse and read values that may or may not be there, depending on packet type.
			switch (PayloadType)
			{
				case RtpPayloadType.VideoFromServer:
					if (data.RemainingBytes < sizeof(ushort))
					{
						data.CurrentOffset = startOffset;
						return RtpParseResult.DataIncomplete;
					}
					SsrcId = data.ReadUInt16Network();
					break;
				case RtpPayloadType.Audio:
					if (data.RemainingBytes < sizeof(byte))
					{
						data.CurrentOffset = startOffset;
						return RtpParseResult.DataIncomplete;
					}
					ProcessorLoad = data.ReadByte();
					break;
			}

			// Read the payload.
			if (data.RemainingBytes < PayloadLength + PacketSuffix.Length)
			{
				data.CurrentOffset = startOffset;
				return RtpParseResult.DataIncomplete;
			}
			data.TryReadBytes(Payload, 0, PayloadLength);

			// Check whether the packet appears to be valid.
			data.TryReadBytes(PacketSuffix, 0, Suffix.Length);
			if (IsValid())
			{
				return RtpParseResult.Success;
			}

			// If we weren't successful at parsing the current data, we need to start over, skipping the most recently found preamble.
			data.CurrentOffset = headerOffset;
			return RtpParseResult.DataInvalid;
		}

		public override bool IsValid()
		{
			bool isValid = base.IsValid() &&
				PacketType == RtpPacketType.Stream &&
				(PayloadType == RtpPayloadType.Audio || PayloadType == RtpPayloadType.VideoFromServer || PayloadType == RtpPayloadType.VideoFromClient) &&
				(PayloadLength > 0 || IsSilent) &&
				PayloadLength <= VideoConstants.MaxPayloadSize;
			if (!isValid)
			{
				ClientLogger.Debug("RtpPacketData is not valid!");
			}
			return isValid;
		}

		public override byte[] BuildPacket()
		{
			_packetBuffer.Reset();
			TryBuildPacket(_packetBuffer);
			if (_packetBuffer.DataLength < 0)
			{
				return null;
			}
			if (_packetBuffer.DataLength == _packetBuffer.Data.Length)
			{
				return _packetBuffer.Data;
			}
			var packet = new byte[_packetBuffer.DataLength];
			Buffer.BlockCopy(_packetBuffer.Data, 0, packet, 0, _packetBuffer.DataLength);
			return packet;
		}

		public override bool TryBuildPacket(ByteStream packetBuffer)
		{

			if (PayloadLength == 0 && !IsSilent)
			{
				throw new InvalidOperationException("If the payload length is zero, the packet must be flagged as silent");
			}

			// Make sure there's enough room.
			int startOffset = packetBuffer.CurrentOffset;
			if (startOffset + GetPacketSize() > packetBuffer.DataLength)
			{
				return false;
			}

			// Write the Preamble.
			packetBuffer.TryWriteBytes(Preamble, 0, Preamble.Length);

			// Write the header.
			packetBuffer.WriteByte((byte)PacketType);
			packetBuffer.WriteByte((byte)PayloadType);
			var audioData = (byte)((byte)AudioCodecType << 4 | Convert.ToByte(IsSilent));
			packetBuffer.WriteByte(audioData);
			packetBuffer.WriteUInt16Network(PayloadLength);
			packetBuffer.WriteUInt16Network(SequenceNumber);
			if (PayloadType == RtpPayloadType.Audio)
			{
				packetBuffer.WriteByte(ProcessorLoad);
			}

			// Write the payload.
			if (!packetBuffer.TryWriteBytes(Payload, 0, PayloadLength))
			{
				return false;
			}

			packetBuffer.TryWriteBytes(Suffix, 0, Suffix.Length);

			packetBuffer.DataLength = packetBuffer.CurrentOffset - startOffset; // size of bytes copied
			Debug.Assert(packetBuffer.DataLength < DataPacketMaxLength, "The packetBuffer was larger than the maximum size allowed.");
			return true;
		}

		public override int GetPacketSize()
		{
			int packetSize = Preamble.Length + HeaderLength + PayloadLength + Suffix.Length;
			if (PayloadType == RtpPayloadType.Audio)
			{
				packetSize += (sizeof(byte)); // The local processor load
			}
			else if (PayloadType == RtpPayloadType.VideoFromServer)
			{
				packetSize += sizeof(ushort);
			}
			return packetSize;
		}
	}
}
