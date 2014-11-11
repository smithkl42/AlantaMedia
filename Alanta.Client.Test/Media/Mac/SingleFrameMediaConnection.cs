using System;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media.Mac
{
	public class SingleFrameMediaConnection : LoopbackMediaConnection
	{
		public SingleFrameMediaConnection(ushort localSsrcId): base(localSsrcId)
		{
			ClientLogger.Debug("SingleFrameMediaConnection created");
		}

		private int _packets;

		public override void SendAudioPacket(short[] audioBuffer, int length, AudioCodecType codecTypeType, bool isSilent, int localProcessorLoad)
		{
			if (_packets++ == 0)
			{
				var data = new short[length];
				Buffer.BlockCopy(audioBuffer, 0, data, 0, length * sizeof(short));
				OnFirstPacketReceived(new EventArgs<short[]>(data));
			}
			base.SendAudioPacket(audioBuffer, length, codecTypeType, isSilent, localProcessorLoad);
		}

		public event EventHandler<EventArgs<short[]>> FirstPacketReceived;

		public void OnFirstPacketReceived(EventArgs<short[]> e)
		{
			EventHandler<EventArgs<short[]>> handler = FirstPacketReceived;
			if (handler != null)
			{
				handler(this, e);
			}
		}
	}
}
