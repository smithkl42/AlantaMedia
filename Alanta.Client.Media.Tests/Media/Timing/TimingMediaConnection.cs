using System;
using System.Collections.Generic;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media.Timing
{
	public class TimingMediaConnection : IMediaConnection
	{

		public TimingMediaConnection(ushort localSsrcId)
		{
			ClientLogger.Debug("TimingMediaConnection created");
			_localSsrcId = localSsrcId;
		}

		private List<IMediaPacket> RecordedFrames { get; set; }
		private readonly ushort _localSsrcId;
		private ushort _sequenceNumber;
		private const int packetsToBuffer = 100;

		public void Dispose()
		{
			Disconnect();
		}

		public void Connect(string roomId, Action<Exception> callback = null)
		{
			ClientLogger.Debug("TimingMediaConnection.Connect() called");
			RecordedFrames = new List<IMediaPacket>();
			if (callback != null)
			{
				callback(null);
			}
		}

		public void Disconnect()
		{
			RecordedFrames = null;
		}

		private int _audioPacketsSent;

		public void SendAudioPacket(short[] audioBuffer, int length, AudioCodecType codecTypeType, bool isSilent, int localProcessorLoad)
		{
			if (++_audioPacketsSent % 200 == 0)
			{
				ClientLogger.Debug("{0} audio packets sent through the TimingMediaConnection", _audioPacketsSent);
			}

			if (!isSilent && length == 0)
			{
				ClientLogger.Debug("Entry has zero datalength!");
			}

			// Buffer 100 frames.
			if (RecordedFrames.Count < packetsToBuffer)
			{
				var packet = new TimingMediaPacket
				{
					Payload = audioBuffer,
					PayloadLength = (ushort)(length * sizeof(short)),
					SsrcId = _localSsrcId,
					ProcessorLoad = 10,
					AudioCodecType = codecTypeType,
					IsSilent = isSilent
				};
				RecordedFrames.Add(packet);
			}
			else
			{
				// Start playing them
				int index = _sequenceNumber % packetsToBuffer;
				if (AudioPacketHandler != null)
				{
					var packet = RecordedFrames[index];
					var copy = new TimingMediaPacket
					{
						Payload = new short[packet.Payload.Length],
						PayloadLength = packet.PayloadLength,
						SsrcId = packet.SsrcId,
						SequenceNumber = _sequenceNumber++,
						ProcessorLoad = packet.ProcessorLoad,
						AudioCodecType = packet.AudioCodecType,
						IsSilent = packet.IsSilent
					};
					Buffer.BlockCopy(packet.Payload, 0, copy.Payload, 0, copy.Payload.Length * sizeof(short));
					if (!copy.IsSilent && copy.PayloadLength == 0)
					{
						ClientLogger.Debug("Entry has zero datalength!");
					}

					AudioPacketHandler(copy);
				}
			}
		}

		public void SendVideoPacket(ByteStream videoChunk)
		{
			throw new NotImplementedException();
		}

		public Action<IMediaPacket> AudioPacketHandler { get; set; }
		public Action<IMediaPacket> VideoPacketHandler { get; set; }
		public bool IsConnecting { get { return false; } }
		public bool IsConnected { get { return RecordedFrames != null; } }
	}

	public class TimingMediaPacket : IMediaPacket
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
