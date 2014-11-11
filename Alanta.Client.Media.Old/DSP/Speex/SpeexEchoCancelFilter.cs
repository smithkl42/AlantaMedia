
namespace Alanta.Client.Media.Dsp.Speex
{
	public class SpeexEchoCancelFilter : EchoCancelFilter
	{
		#region Constructors
		public SpeexEchoCancelFilter(int systemLatency, int filterLength, AudioFormat recordedAudioFormat, AudioFormat playedAudioFormat, IAudioFilter playedResampler = null, IAudioFilter recordedResampler = null) :
			base(systemLatency, filterLength, recordedAudioFormat, playedAudioFormat, playedResampler, recordedResampler)
		{
			speexEchoState = SpeexEchoCanceller.speex_echo_state_init(recordedAudioFormat.SamplesPerFrame, filterLength, _logger);
			object sampleRate = recordedAudioFormat.SamplesPerSecond;
			SpeexEchoCanceller.speex_echo_ctl(speexEchoState, EchoControlCommand.SetSamplingRate, ref sampleRate);
		}
		#endregion

		#region Fields and Properties
		private SpeexEchoState speexEchoState;
		#endregion

		#region Methods
		protected override void PerformEchoCancellation(short[] recorded, short[] played, short[] outFrame)
		{
			SpeexEchoCanceller.speex_echo_cancellation(speexEchoState, recorded, played, outFrame);
		}
		#endregion
	}
}
