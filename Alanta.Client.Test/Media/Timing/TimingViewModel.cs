using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;
using Alanta.Client.Media.AudioCodecs;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Media.Dsp.WebRtc;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.Controls;
using ReactiveUI;
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Test.Media.Timing
{
	public class TimingViewModel : ReactiveObject
	{
		#region Constructors
		public TimingViewModel()
		{
			_captureSource = new CaptureSource();
			AudioContextCollection = new ObservableCollection<AudioContext>();
			CreateAudioContexts();
		}
		
		#endregion

		#region Fields and Properties
		public ObservableCollection<AudioContext> AudioContextCollection { get; private set; }

		private AudioContext _currentAudioContext;
		public AudioContext CurrentAudioContext
		{
			get { return _currentAudioContext; }
			set { this.RaiseAndSetIfChanged(x => x.CurrentAudioContext, ref _currentAudioContext, value); }
		}

		private string _status;
		public string Status
		{
			get { return _status; }
			set { this.RaiseAndSetIfChanged(x => x.Status, ref _status, value); }
		}

		private MediaController _mediaController;
		private MediaElement _mediaElement;
		private IMediaConnection _mediaConnection;
		private AudioMediaStreamSource _audioStreamSource;
		private AudioSinkAdapter _audioSink;
		private readonly CaptureSource _captureSource;
		public MediaStatistics MediaStatistics { get; private set; }
		#endregion

		#region Methods
		public void CreateAudioContexts()
		{

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
			var desiredFormat = _captureSource.AudioCaptureDevice.DesiredFormat;
			var rawAudioFormat = new AudioFormat(desiredFormat.SamplesPerSecond, AudioFormat.Default.MillisecondsPerFrame, desiredFormat.Channels, desiredFormat.BitsPerSample);

			var playedAudioFormat = new AudioFormat();
			var config = MediaConfig.Default;

			// Absolutely bare minimum processing - doesn't process sound at all.
			var nullAudioFormat = new AudioFormat();
			var nullResampler = new ResampleFilter(rawAudioFormat, nullAudioFormat);
			nullResampler.InstanceName = "Null resample filter";
			var nullEnhancer = new NullEchoCancelFilter(config.ExpectedAudioLatency, config.FilterLength, nullAudioFormat, playedAudioFormat);
			nullEnhancer.InstanceName = "Null";
			var nullDtx = new NullAudioInplaceFilter();
			var nullEncoder = new NullAudioEncoder();
			var nullAudioContext = new AudioContext(nullAudioFormat, nullResampler, nullDtx, nullEnhancer, nullEncoder);
			nullAudioContext.Description = "Null";

			// What we should use when there's only one other person, and CPU is OK: 
			// 16Khz, Speex, WebRtc at full strength
			var directAudioFormat = new AudioFormat();
			var directResampler = new ResampleFilter(rawAudioFormat, directAudioFormat);
			directResampler.InstanceName = "Direct high quality resample filter";
			var directEnhancer = new WebRtcFilter(config.ExpectedAudioLatency, config.FilterLength, directAudioFormat, playedAudioFormat, config.EnableAec, config.EnableDenoise, config.EnableAgc);
			directEnhancer.InstanceName = "High";
			var directDtx = new DtxFilter(directAudioFormat);
			var directEncoder = new SpeexEncoder(directAudioFormat);
			var directAudioContext = new AudioContext(directAudioFormat, directResampler, directDtx, directEnhancer, directEncoder);
			directAudioContext.Description = "High Quality Direct";

			// What we should use when there are multiple people (and hence the audio will need to be decoded and mixed), but CPU is OK:
			// 8Khz, G711, WebRtc at full strength
			var conferenceAudioFormat = new AudioFormat(AudioConstants.NarrowbandSamplesPerSecond);
			var conferenceResampler = new ResampleFilter(rawAudioFormat, conferenceAudioFormat);
			conferenceResampler.InstanceName = "Conference high quality resample filter";
			var conferenceEnhancer = new WebRtcFilter(config.ExpectedAudioLatency, config.FilterLength, conferenceAudioFormat, playedAudioFormat, config.EnableAec, config.EnableDenoise, config.EnableAgc);
			conferenceEnhancer.InstanceName = "Medium";
			var conferenceDtx = new DtxFilter(conferenceAudioFormat);
			var conferenceEncoder = new G711MuLawEncoder(conferenceAudioFormat);
			var conferenceAudioContext = new AudioContext(conferenceAudioFormat, conferenceResampler, conferenceDtx, conferenceEnhancer, conferenceEncoder);
			conferenceAudioContext.Description = "High Quality Conference";

			// What we should use when one or more remote CPU's isn't keeping up (regardless of how many people are in the room):
			// 8Khz, G711, WebRtc at full-strength
			var remoteFallbackAudioFormat = new AudioFormat(AudioConstants.NarrowbandSamplesPerSecond);
			var remoteFallbackResampler = new ResampleFilter(rawAudioFormat, remoteFallbackAudioFormat);
			remoteFallbackResampler.InstanceName = "Fallback remote high cpu resample filter";
			var remoteFallbackEnhancer = new WebRtcFilter(config.ExpectedAudioLatency, config.FilterLength, remoteFallbackAudioFormat, playedAudioFormat, config.EnableAec, config.EnableDenoise, config.EnableAgc);
			remoteFallbackEnhancer.InstanceName = "Medium";
			var remoteFallbackDtx = new DtxFilter(remoteFallbackAudioFormat);
			var remoteFallbackEncoder = new G711MuLawEncoder(remoteFallbackAudioFormat);
			var remoteFallbackAudioContext = new AudioContext(remoteFallbackAudioFormat, remoteFallbackResampler, remoteFallbackDtx, remoteFallbackEnhancer, remoteFallbackEncoder);
			remoteFallbackAudioContext.Description = "Fallback for remote high CPU";

			// What we should use when the local CPU isn't keeping up (regardless of how many people are in the room):
			// 8Khz, G711, WebRtc at half-strength
			var fallbackAudioFormat = new AudioFormat(AudioConstants.NarrowbandSamplesPerSecond);
			var fallbackResampler = new ResampleFilter(rawAudioFormat, fallbackAudioFormat);
			fallbackResampler.InstanceName = "Fallback resample filter";
			var fallbackEnhancer = new WebRtcFilter(config.ExpectedAudioLatencyFallback, config.FilterLengthFallback, fallbackAudioFormat, playedAudioFormat, config.EnableAec, false, false);
			fallbackEnhancer.InstanceName = "Low";
			var fallbackDtx = new DtxFilter(fallbackAudioFormat);
			var fallbackEncoder = new G711MuLawEncoder(fallbackAudioFormat);
			var fallbackAudioContext = new AudioContext(fallbackAudioFormat, fallbackResampler, fallbackDtx, fallbackEnhancer, fallbackEncoder);
			fallbackAudioContext.Description = "Fallback for local high CPU";

			AudioContextCollection.Clear();
			AudioContextCollection.Add(nullAudioContext);
			AudioContextCollection.Add(directAudioContext);
			AudioContextCollection.Add(conferenceAudioContext);
			AudioContextCollection.Add(remoteFallbackAudioContext);
			AudioContextCollection.Add(fallbackAudioContext);

			CurrentAudioContext = nullAudioContext;
		}

		public void SelectDevices()
		{
			try
			{
				var config = new ConfigureAudioVideo();
				config.AudioCaptureDevice = _captureSource == null ? CaptureDeviceConfiguration.GetDefaultAudioCaptureDevice() : _captureSource.AudioCaptureDevice;
				config.VideoCaptureDevice = null;
				config.Closed += config_Closed;
				// For some reason, MaxWidth and MaxHeight doesn't work here.
				config.Width = Math.Min(config.Width, Application.Current.Host.Content.ActualWidth);
				config.Height = Math.Min(config.Height, Application.Current.Host.Content.ActualHeight);
				config.Show();
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Showing configuration for AudioVideo failed");
			}

		}

		void config_Closed(object sender, EventArgs e)
		{
			try
			{
				var config = (ConfigureAudioVideo)sender;
				if (config.DialogResult.HasValue && config.DialogResult.Value)
				{
					if (_captureSource != null)
					{
						if (_captureSource.AudioCaptureDevice != config.AudioCaptureDevice || _captureSource.VideoCaptureDevice != config.VideoCaptureDevice)
						{
							_captureSource.AudioCaptureDevice = config.AudioCaptureDevice;
							CreateAudioContexts();
						}
					}
				}
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Closing windows of configuration AudioVideo failed");
			}
		}

		public void StartTimingTest()
		{
			// MessageBox.Show("Currently selected context = " + CurrentAudioContext.Description);
			Status = "Executing timing test for context '" + CurrentAudioContext.Description + "'";
			_mediaElement = new MediaElement();
			_audioStreamSource = new AudioMediaStreamSource(null, AudioFormat.Default);
			_captureSource.VideoCaptureDevice = null;

			// Make sure we can get at the devices.
			if (_captureSource.AudioCaptureDevice == null)
			{
				throw new InvalidOperationException("No audio capture device was found");
			}
			if (_captureSource.AudioCaptureDevice.DesiredFormat == null)
			{
				throw new InvalidOperationException("No suitable audio format was found");
			}
			if (!CaptureDeviceConfiguration.AllowedDeviceAccess && !CaptureDeviceConfiguration.RequestDeviceAccess())
			{
				throw new InvalidOperationException("Device access not granted.");
			}

			// Create the audio sink.
			MediaConfig.Default.LocalSsrcId = 1000;
			MediaStatistics = new TimingMediaStatistics();
			var mediaEnvironment = new TestMediaEnvironment();

			// Create the media controller
			_mediaConnection = new LoopbackMediaConnection(MediaConfig.Default.LocalSsrcId);
			var vqc = new VideoQualityController(MediaConfig.Default.LocalSsrcId);
			_mediaController = new MediaController(MediaConfig.Default, AudioFormat.Default, MediaStatistics, mediaEnvironment, _mediaConnection, vqc);

			// Create the audio sink to grab data from the microphone and send it to the media controller.
			_audioSink = new TimingAudioSinkAdapter(CurrentAudioContext, _captureSource, _mediaController, MediaConfig.Default, new TestMediaEnvironment(), CurrentAudioContext.AudioFormat);
			_audioSink.CaptureSuccessful += _audioSink_CaptureSuccessful;

			// Create the media stream source to play data from the media controller
			_audioStreamSource.AudioController = _mediaController;
			_mediaElement.SetSource(_audioStreamSource);

			// Connect.
			_mediaController.Connect("test", ex => Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (ex != null)
				{
					StopTimingTest();
					MessageBox.Show(ex.ToString());
				}
				else
				{
					ClientLogger.Debug("TimingViewModel connected to MediaController");
				}
			}));
			_mediaController.RegisterRemoteSession(1001);

			// Start capturing (which should trigger the audio sink).
			_captureSource.Start();
			if (_captureSource.State != CaptureState.Started)
			{
				throw new InvalidOperationException("Unable to capture microphone");
			}

			// Start playing.
			_mediaElement.Play();
			ClientLogger.Debug("CaptureSource initialized; captureSource.State={0}; captureSource.AudioCaptureDevice={1}; mediaElement.CurrentState={2}; ",
			                   _captureSource.State, _captureSource.AudioCaptureDevice.FriendlyName, _mediaElement.CurrentState);
		}

		void _audioSink_CaptureSuccessful(object sender, EventArgs e)
		{
			ClientLogger.Debug("The timing audio sink adapter successfully captured some data!");
		}

		public void StopTimingTest()
		{
			Status = "";
			_captureSource.Stop();
			if (_mediaElement != null)
			{
				_mediaElement.Stop();
				_mediaElement = null;
			}
			if (_mediaController != null)
			{
				_mediaController.Dispose();
			}
		}

		#endregion
	}

}
