using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using Alanta.Client.Common;

namespace Alanta.Client.Media
{
	public interface IMediaController
	{
		MediaConfig MediaConfig { get; }
		ObservableCollection<AudioStatistics> AudioStats { get; }
		MediaStatistics MediaStats { get; }
		ICodecFactory CodecFactory { get; }
		bool IsConnected { get; }
		bool IsMicrophoneMuted { get; set; }
		bool IsSpeakerMuted { get; set; }
		bool IsRemoteVideoMuted { get; set; }
		bool IsLocalWebcamMuted { get; set; }
		bool IsConnecting { get; }
		ushort LocalSsrcId { get; }
		double MicrophoneVolume { get; set; }
		double SpeakerVolume { get; set; }
		event EventHandler<ExceptionEventArgs> ErrorOccurred;
		AudioSinkAdapter GetAudioSink(CaptureSource captureSource, bool resyncAudio);
		VideoSinkAdapter GetVideoSink(CaptureSource captureSource);
		event EventHandler<RemoteCameraChangedEventArgs> RemoteCameraChanged;

		/// <summary>
		/// Connects to the media server.
		/// </summary>
		/// <param name="roomId">The room on the media server</param>
		/// <param name="callback">An optional callback to be called when connection is finished.</param>
		/// <remarks>
		/// The connection process is complicated, due to the fact that we have to first connect to the control port (4521), 
		/// and then if that is successful, we then connect to the data port (4522).  And of course, both connection attempts are
		/// asynchronous. The result is that the logic in a successful connection attempt flows like this:
		///   Connect() -> controlClient.Connect() =>
		///   HandleControlConnect() -> RegisterClientOnServer() -> controlClient.Send() =>
		///   HandleControlData() -> rtpClient.Connect() =>
		///   HandleRtpConnect() -> rtpClient.Send() =>
		///   HandleRtpData() -> FinalizeConnection() -> connectionCallback()
		/// An error anywhere in that flow will result in control being transferred to FinalizeConnection().
		/// </remarks>
		void Connect(string roomId, Action<Exception> callback = null);

		/// <summary>
		/// Registers a media stream source listener for a given ssrcId.
		/// </summary>
		/// <param name="ssrcId">The ssrcId of the media stream source for which it should be listening.</param>
		/// <remarks>
		/// This method is called when the main client is notified (via the web service) that a new user has joined the room.
		/// This method tells the media controller to be prepared for video data tagged for the specified SsrcId 
		/// to start coming down the pipe.
		/// </remarks>
		void RegisterRemoteSession(ushort ssrcId);

		/// <summary>
		/// Unregister a media stream source listener for a given ssrcid
		/// </summary>
		/// <param name="ssrcId">The ssrcId of the media stream source for which it should stop listening.</param>
		void UnregisterRemoteSession(ushort ssrcId);

		/// <summary>
		/// Reads the next audio frame from the jitter buffer, decodes it, and presents it to the speaker.
		/// </summary>
		/// <returns>A MemoryStream object wrapping the most recently decoded frame.</returns>
		MemoryStream GetNextAudioFrame();

		/// <summary>
		/// Sets the audio frame to be processed before it is transmitted to the media server. Typically called by the RtpAudioSink class.
		/// </summary>
		/// <param name="frame">A byte[] array representing the samples received from the local microphone.</param>
		void SubmitRecordedFrame(byte[] frame);

		MemoryStream GetNextVideoFrame(ushort ssrcId);
		void SetVideoFrame(byte[] frame, int stride);
		void Dispose();
	}
}