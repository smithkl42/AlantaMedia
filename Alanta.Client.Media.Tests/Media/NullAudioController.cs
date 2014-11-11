using System;
using System.IO;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media
{
	public class NullAudioController : IAudioController
	{

		public void GetNextAudioFrame(Action<MemoryStream> callback)
		{
			callback(null);
		}

		public void SubmitRecordedFrame(AudioContext audioContext, byte[] frame)
		{
			// No-op
		}

		public bool IsConnected
		{
			get { return true; }
		}
	}
}
