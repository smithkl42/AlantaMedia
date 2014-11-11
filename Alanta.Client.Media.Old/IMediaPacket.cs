using System;

namespace Alanta.Client.Media
{
	public interface IMediaPacket
	{
		/// <summary>
		/// A byte[] array containing the packet payload.
		/// </summary>
		Array Payload { get; set; }

		/// <summary>
		/// The length of the payload in bytes.
		/// </summary>
		ushort PayloadLength { get; set; }

		/// <summary>
		/// The SsrcId number identifying the source of the frame.
		/// </summary>
		ushort SsrcId { get; }

		/// <summary>
		/// The frame's sequence number.
		/// </summary>
		ushort SequenceNumber { get; set; }

		/// <summary>
		/// The machine's last processor load
		/// </summary>
		byte ProcessorLoad { get; set; }

		/// <summary>
		/// The payload type of the packet, whether audio, sending video or receiving video.
		/// </summary>
		RtpPayloadType PayloadType { get; set; }

		/// <summary>
		/// The audio codec in use (only relevant to audio packets, otherwise ignored)
		/// </summary>
		AudioCodecType AudioCodecType { get; set; }

		/// <summary>
		/// Whether the packet in question is silent (1) or not (0)
		/// </summary>
		bool IsSilent { get; set; }
	}
}