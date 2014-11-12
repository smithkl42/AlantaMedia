using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Alanta.Client.Common.Logging;
using Alanta.Client.Test.Media;
using Alanta.Client.Test.Media.MediaServer;
using Alanta.Client.Ui.Common;

namespace Alanta.Client.Media.Tests.Media.MediaServer
{
	public partial class MediaServerTest : Page
	{

		#region Constructors
		public MediaServerTest()
		{
			InitializeComponent();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			InitializeCaptureSource();
			lstClients.ItemsSource = _mediaServerVms;
			btnStop.IsEnabled = false;
			txtHost.Text = "localhost";
		}

		private void InitializeCaptureSource()
		{
			if (_captureSource != null) return;

			// Setup the capture source (for recording audio)
			_captureSource = new CaptureSource();
			_captureSource.AudioCaptureDevice = CaptureDeviceConfiguration.GetDefaultAudioCaptureDevice();
			if (_captureSource.AudioCaptureDevice != null)
			{
				MediaDeviceConfig.SelectBestAudioFormat(_captureSource.AudioCaptureDevice);
				if (_captureSource.AudioCaptureDevice.DesiredFormat != null)
				{
					var mediaStats = new MediaStatistics();
					var mediaEnvironment = new MediaEnvironment(mediaStats);
					_captureSource.AudioCaptureDevice.AudioFrameSize = AudioFormat.Default.MillisecondsPerFrame; // 20 milliseconds
					_audioSinkAdapter = new MultipleControllerAudioSinkAdapter(GetMediaConfig(), _captureSource, 2000);
					_mediaStreamSource = new MultipleControllerAudioMediaStreamSource(2000);
					ClientLogger.Debug("CaptureSource initialized.");
				}
				else
				{
					ClientLogger.Debug("No suitable audio format was found.");
				}
			}
			else
			{
				// Do something more here eventually, once we figure out what the user experience should be.
				ClientLogger.Debug("No audio capture device was found.");
			}
		}
		#endregion

		#region Fields and Properties

		private CaptureSource _captureSource;
		private MultipleControllerAudioSinkAdapter _audioSinkAdapter;
		private MultipleControllerAudioMediaStreamSource _mediaStreamSource;
		private readonly ObservableCollection<MediaServerViewModel> _mediaServerVms = new ObservableCollection<MediaServerViewModel>();
		private readonly Random _rnd = new Random();

		#endregion

		#region Methods

		private void btnStart_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!(CaptureDeviceConfiguration.AllowedDeviceAccess || CaptureDeviceConfiguration.RequestDeviceAccess()))
				{
					MessageBox.Show("Unable to capture microphone");
					return;
				}

				_audioSinkAdapter.UseGeneratedTone = chkUseGeneratedTone.IsChecked ?? true;
				_mediaStreamSource.UseGeneratedTone = chkUseGeneratedTone.IsChecked ?? true;

				// Create the context factory.
				var rootMediaConfig = GetMediaConfig();
				var connectionSelection = (ComboBoxItem) cboConnection.SelectedItem;
				var connectionType = (MediaConnectionType) Enum.Parse(typeof (MediaConnectionType), (string) connectionSelection.Content, true);
				var formatSelection = (ComboBoxItem)cboAudioFormat.SelectedItem;
				var audioFormat = new AudioFormat(int.Parse((string)formatSelection.Content));
				var enhancerSelection = (ComboBoxItem)cboEnhancer.SelectedValue;
				var enhancerType = (SpeechEnhancementStack)Enum.Parse(typeof(SpeechEnhancementStack), (string)enhancerSelection.Content, true);
				var encoderSelection = (ComboBoxItem)cboEncoder.SelectedValue;
				var encoderType = (AudioCodecType)Enum.Parse(typeof(AudioCodecType), (string)encoderSelection.Content, true);
				var ctxFactory = new TestAudioContextFactory(rootMediaConfig, _audioSinkAdapter.RawAudioFormat, audioFormat, enhancerType, encoderType);
				_audioSinkAdapter.RootAudioContext = ctxFactory.GetAudioContext();

				_mediaServerVms.Clear();
				_audioSinkAdapter.AudioControllers.Clear();
				_audioSinkAdapter.AudioContexts.Clear();
				_mediaStreamSource.AudioControllers.Clear();
				var connections = int.Parse(txtConnections.Text);
				var rooms = int.Parse(txtRooms.Text);
				_audioSinkAdapter.Rooms = rooms;
				_audioSinkAdapter.ConnectionsPerRoom = connections;
				for (int room = 0; room < rooms; room++)
				{
					string roomId = string.Format("__alantaTestRoom{0}__", room);

					var mediaStats = new MediaStatistics();
					var mediaEnvironment = new MediaEnvironment(mediaStats);

					// Register each room on the remote server.
					for (int connection = 0; connection < connections; connection++)
					{
						var connectionMediaConfig = GetMediaConfig();
						IMediaConnection mediaConnection;
						if (connectionType == MediaConnectionType.MediaServer)
						{
							mediaConnection = new RtpMediaConnection(connectionMediaConfig, mediaStats);
						}
						else
						{
							mediaConnection = new LoopbackMediaConnection(connectionMediaConfig.LocalSsrcId);
						}
						var vqc = new VideoQualityController(connectionMediaConfig.LocalSsrcId);
						var vm = new MediaServerViewModel(connectionMediaConfig, AudioFormat.Default, mediaStats, mediaEnvironment, mediaConnection, vqc, roomId);
						_audioSinkAdapter.AudioControllers.Add(vm.MediaController);
						_audioSinkAdapter.AudioContexts.Add(ctxFactory.GetAudioContext());
						_mediaStreamSource.AudioControllers.Add(new AudioControllerEntry(vm.MediaController));
						_mediaServerVms.Add(vm);
						vm.Connect();
					}

					// Make sure each session knows about all the others in the same room.
					var localVms = _mediaServerVms.Where(x => x.RoomId == roomId).ToList();
					foreach (var localVm in localVms)
					{
						var vm = localVm;
						var remoteVms = localVms.Where(x => x.SsrcId != vm.SsrcId).ToList();
						foreach (var remoteVm in remoteVms)
						{
							vm.MediaController.RegisterRemoteSession(remoteVm.SsrcId);
						}
					}
				}

				if (mediaElement.CurrentState == MediaElementState.Closed)
				{
					mediaElement.SetSource(_mediaStreamSource);
				}
				_captureSource.Start();
				mediaElement.Play();
				btnStop.IsEnabled = true;
				btnStart.IsEnabled = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private MediaConfig GetMediaConfig()
		{
			var mediaConfig = new MediaConfig
			{
				MediaServerHost = txtHost.Text,
				MediaServerControlPort = MediaConstants.DefaultMediaServerControlPort,
				MediaServerStreamingPort = MediaConstants.DefaultMediaServerStreamingPort,
				CodecFactory = new CodecFactory(AudioFormat.Default),
				LocalSsrcId = (ushort)_rnd.Next(ushort.MaxValue),
				ExpectedAudioLatency = 200,
				FilterLength = 200,
				PlayEchoCancelledSound = true,
				ApplyVolumeFilterToPlayedSound = true,
				ApplyVolumeFilterToRecordedSound = true,
				EnableDenoise = chkEnableDenoise.IsChecked ?? true,
				EnableAgc = chkEnableAgc.IsChecked ?? true,
				EnableAec = chkEnableAec.IsChecked ?? true
			};
			return mediaConfig;
		}

		private void btnStop_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_captureSource.Stop();
				mediaElement.Stop();
				foreach (var vm in _mediaServerVms)
				{
					vm.Disconnect();
				}
				btnStart.IsEnabled = true;
				btnStop.IsEnabled = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		#endregion

		private void chkEnableDenoise_Checked(object sender, RoutedEventArgs e)
		{

		}

		private enum MediaConnectionType
		{
			MediaServer,
			Loopback
		}


	}
}
