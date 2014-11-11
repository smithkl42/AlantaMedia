namespace Alanta.Client.Media
{
	public interface IAudioContextFactory
	{
		AudioFormat RawAudioFormat { get; }
		AudioFormat PlayedAudioFormat { get; }
		MediaConfig MediaConfig { get; }
		IMediaEnvironment MediaEnvironment { get; }
		AudioContext GetAudioContext();
	}
}