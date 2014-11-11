using System;

namespace Alanta.Client.Media
{
	/// <summary>
	/// The IMediaConnection interface abstracts the interface required to send and retrieve media packets. The default implementation is our
	/// proprietary version of the RTP protocol; if we ever switch to a real RTP protocol, that would be a secondary implementation; and we
	/// also have some implementations for use in testing when we don't want to bother the media server.
	/// </summary>
	public interface IMediaConnection : IDisposable
	{
		void Connect(string roomId, Action<Exception> callback = null);
		void Disconnect();
		void SendAudioPacket(short[] audioBuffer, int length, AudioCodecType codecTypeType, bool isSilent, int localProcessorLoad);
		void SendVideoPacket(ByteStream videoChunk);
		Action<IMediaPacket> AudioPacketHandler { get; set; }
		Action<IMediaPacket> VideoPacketHandler { get; set; }
		bool IsConnecting { get; }
		bool IsConnected { get; }
	}
}
