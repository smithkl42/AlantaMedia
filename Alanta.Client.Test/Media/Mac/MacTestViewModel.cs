using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using Alanta.Client.Data;
using Alanta.Client.Media;
using Alanta.Client.Test.Media.Timing;
using Alanta.Client.UI.Common.Classes;
using Alanta.Common;
using ReactiveUI;
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Test.Media.Mac
{
	public class MacTestViewModel : ReactiveObject
	{
		private string _results;
		public string Results
		{
			get { return _results; }
			set { this.RaiseAndSetIfChanged(x => x.Results, ref _results, value); }
		}

		private static readonly Random rnd = new Random();
		private CaptureSource _captureSource;
		private MediaController _mediaController;
		private AudioSinkAdapter _audioSinkAdapter;

		public void AnalyzeSingleFrame(List<byte[]> testFrames)
		{
			// Create the audio context.
			var config = MediaConfig.Default;
			config.LocalSsrcId = 1000;
			var rawAudioFormat = new AudioFormat(); // This will be overwritten later
			var playedAudioFormat = new AudioFormat();
			var mediaEnvironment = new TestMediaEnvironment();
			var audioContextFactory = new AudioContextFactory(rawAudioFormat, playedAudioFormat, config, mediaEnvironment);
			var audioContext = audioContextFactory.HighQualityDirectCtx;

			// Create the media controller
			var mediaStatistics = new TimingMediaStatistics();
			var mediaConnection = new SingleFrameMediaConnection(MediaConfig.Default.LocalSsrcId);
			mediaConnection.FirstPacketReceived += mediaConnection_FirstPacketReceived;
			var vqc = new VideoQualityController(MediaConfig.Default.LocalSsrcId);
			var mediaController = new MediaController(MediaConfig.Default, playedAudioFormat, mediaStatistics, mediaEnvironment, mediaConnection, vqc);

			// Connect.
			mediaController.Connect("test", ex => Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (ex != null)
				{
					MessageBox.Show(ex.ToString());
				}
				else
				{
					ClientLogger.Debug("TimingViewModel connected to MediaController");
				}
			}));
			mediaController.RegisterRemoteSession(1001);

			foreach (var frame in testFrames)
			{
				mediaController.SubmitRecordedFrame(audioContext, frame);
			}

		}

		void mediaConnection_FirstPacketReceived(object sender, EventArgs<short[]> e)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				Results = DebugHelper.AnalyzeAudioFrame("mediaConnection_FirstPacketReceived", e.Value, 0, e.Value.Length);
			});
		}

		public void StartSendingAudioToRoom(string ownerUserTag, string host, List<byte[]> testFrames, bool sendLive, OperationCallback callback)
		{
			// What we should use when there's only one other person, and CPU is OK: 
			// 16Khz, Speex, WebRtc at full strength
			var config = MediaConfig.Default;
			config.LocalSsrcId = (ushort)rnd.Next(ushort.MinValue, ushort.MaxValue);
			config.AudioContextSelection = AudioContextSelection.HighQualityDirect;
			config.MediaServerHost = host;

			// Create the media controller
			var playedAudioFormat = new AudioFormat();
			var mediaStatistics = new TimingMediaStatistics();
			var mediaEnvironment = new TestMediaEnvironment();
			var mediaConnection = new RtpMediaConnection(config, mediaStatistics);
			var vqc = new VideoQualityController(MediaConfig.Default.LocalSsrcId);
			_mediaController = new MediaController(MediaConfig.Default, playedAudioFormat, mediaStatistics, mediaEnvironment, mediaConnection, vqc);

			// Create the audio sink adapter.
			_captureSource = new CaptureSource();
			_captureSource.VideoCaptureDevice = null;
			if (_captureSource.AudioCaptureDevice == null)
			{
				_captureSource.AudioCaptureDevice = CaptureDeviceConfiguration.GetDefaultAudioCaptureDevice();
				if (_captureSource.AudioCaptureDevice == null)
				{
					throw new InvalidOperationException("No suitable audio capture device was found");
				}
			}
			MediaDeviceConfig.SelectBestAudioFormat(_captureSource.AudioCaptureDevice);
			_captureSource.AudioCaptureDevice.AudioFrameSize = AudioFormat.Default.MillisecondsPerFrame; // 20 milliseconds
			_audioSinkAdapter = sendLive 
				? new AudioSinkAdapter(_captureSource, _mediaController, config, mediaEnvironment, playedAudioFormat) 
				: new FromFileAudioSinkAdapter(_captureSource, _mediaController, config, mediaEnvironment, playedAudioFormat, testFrames);

			var roomService = new RoomServiceAdapter();
			roomService.CreateClient();
			roomService.GetRoomId(Constants.DefaultCompanyTag, Constants.DefaultAuthenticationGroupTag, ownerUserTag, Constants.DefaultRoomName, (getRoomError, result) =>
			{
				if (getRoomError != null)
				{
					callback(getRoomError);
				}
				else
				{
					// Connect.
					_mediaController.Connect(result.RoomId.ToString(), connectError => Deployment.Current.Dispatcher.BeginInvoke(() =>
					{
						if (connectError == null)
						{
							ClientLogger.Debug("MacTestViewModel connected to MediaController");
							_captureSource.Start();
						}
						_mediaController.RegisterRemoteSession((ushort)(config.LocalSsrcId+1));
						callback(connectError);
					}));
				}
			});

		}

		public void StopSendingAudioToRoom()
		{
			_captureSource.Stop();
			_captureSource = null;
			_mediaController.Dispose();
			_mediaController = null;
			_audioSinkAdapter = null;
		}
	}
}
