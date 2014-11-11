using System;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media
{
	public class LoopbackMediaConnection : IMediaConnection
	{

		public LoopbackMediaConnection(ushort localSsrcId)
		{
			ClientLogger.Debug("TimingMediaConnection created");
			_localSsrcId = localSsrcId;
		}

		private readonly ushort _localSsrcId;
		private ushort _sequenceNumber;

		public void Dispose()
		{
			Disconnect();
		}

		public void Connect(string roomId, Action<Exception> callback = null)
		{
			ClientLogger.Debug("TimingMediaConnection.Connect() called");
			IsConnected = true;
			if (callback != null)
			{
				callback(null);
			}
		}

		public void Disconnect()
		{
			IsConnected = false;
		}

		private int _audioPacketsSent;

		public virtual void SendAudioPacket(short[] audioBuffer, int length, AudioCodecType codecTypeType, bool isSilent, int localProcessorLoad)
		{
			if (++_audioPacketsSent % 200 == 0)
			{
				ClientLogger.Debug("{0} audio packets sent through the LoopbackMediaConnection", _audioPacketsSent);
			}

			if (!isSilent && length == 0)
			{
				ClientLogger.Debug("Entry has zero datalength!");
			}

			var packet = new LoopbackMediaPacket
			{
				Payload = audioBuffer,
				PayloadLength = (ushort)(length * sizeof(short)),
				SsrcId = _localSsrcId,
				ProcessorLoad = 10,
				AudioCodecType = codecTypeType,
				IsSilent = isSilent,
				SequenceNumber = _sequenceNumber++
			};

			AudioPacketHandler(packet);
		}

		public void SendVideoPacket(ByteStream videoChunk)
		{
			throw new NotImplementedException();
		}

		public Action<IMediaPacket> AudioPacketHandler { get; set; }
		public Action<IMediaPacket> VideoPacketHandler { get; set; }
		public bool IsConnecting { get { return false; } }
		public bool IsConnected { get; private set; }
	}

	public class LoopbackMediaPacket : IMediaPacket
	{
		public Array Payload { get; set; }
		public ushort PayloadLength { get; set; }
		public ushort SsrcId { get; set; }
		public ushort SequenceNumber { get; set; }
		public byte ProcessorLoad { get; set; }
		public RtpPayloadType PayloadType { get; set; }
		public AudioCodecType AudioCodecType { get; set; }
		public bool IsSilent { get; set; }
	}
}
