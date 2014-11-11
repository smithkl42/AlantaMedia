using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;

namespace Alanta.Client.Ui.Controls
{
	/// <summary>
	/// Manages the configuration and control of local media devices (e.g., webcam and microphone), and allows them
	/// to be connected up to 
	/// </summary>
	public class MediaDevices : IDisposable
	{
		#region Constructors

		public MediaDevices(IMediaSinkFactory mediaSinkFactory)
		{
			UseAutomaticSelection = true;
			if (!DesignerProperties.IsInDesignTool)
			{
				_mediaSinkFactory = mediaSinkFactory;
				var helper = new ConfigurationHelper<MediaDeviceConfig>();
				_mediaDeviceConfig = helper.GetConfigurationObject(MediaDeviceConfig.DefaultFileName);
				CaptureSource = new CaptureSource();
				PossibleAudioDevices = new MediaDeviceList<AudioCaptureDevice>(_mediaDeviceConfig.SelectBestAudioCaptureDevice, CaptureDeviceConfiguration.GetAvailableAudioCaptureDevices());
				PossibleVideoDevices = new MediaDeviceList<VideoCaptureDevice>(_mediaDeviceConfig.SelectBestVideoCaptureDevice, CaptureDeviceConfiguration.GetAvailableVideoCaptureDevices());
				SelectedAudioDevice = PossibleAudioDevices.GetNextDevice();
				SelectedVideoDevice = PossibleVideoDevices.GetNextDevice();
				CaptureSource.AudioCaptureDevice = SelectedAudioDevice;
				CaptureSource.VideoCaptureDevice = SelectedVideoDevice;
			}
		}
		#endregion

		#region Fields and Properties
		private readonly MediaDeviceConfig _mediaDeviceConfig;
		private readonly IMediaSinkFactory _mediaSinkFactory;
		public CaptureSource CaptureSource { get; private set; }
		public AudioCaptureDevice SelectedAudioDevice { get; private set; }
		public VideoCaptureDevice SelectedVideoDevice { get; private set; }
		public AudioSinkAdapter AudioSink { get; private set; }
		public VideoSinkAdapter VideoSink { get; private set; }
		public bool CapturingAudio { get; private set; }
		public bool CapturingVideo { get; private set; }
		public bool UseAutomaticSelection { get; set; }
		private DispatcherTimer _captureTimer;
		public event EventHandler<EventArgs> CaptureStarted;
		public event EventHandler<EventArgs<bool, bool>> CaptureSucceeded;
		public event EventHandler<EventArgs<bool, bool>> CaptureFailed;
		public readonly MediaDeviceList<AudioCaptureDevice> PossibleAudioDevices;
		public readonly MediaDeviceList<VideoCaptureDevice> PossibleVideoDevices;
		#endregion

		#region Methods
		public void Start()
		{
			ConfigureAudioCaptureDevice(CaptureSource.AudioCaptureDevice);
			ConfigureVideoCaptureDevice(CaptureSource.VideoCaptureDevice);
			CaptureSelectedInputDevices();
		}

		public void Stop()
		{
			try
			{
				if (CaptureSource != null && CaptureSource.State == CaptureState.Started)
				{
					CaptureSource.Stop();
					ClientLogger.Debug("Local captureSource stopped");
				}
				else
				{
					ClientLogger.Debug("Local captureSource already stopped: {0}", CaptureSource == null ? "captureSource is null" : CaptureSource.State.ToString());
				}
				CaptureSource = null;
			}
			catch (Exception ex)
			{
				ClientLogger.Error(ex.ToString());
			}
		}

		public void ChangeCapturedDevices(AudioCaptureDevice audioDevice, VideoCaptureDevice videoDevice)
		{
			try
			{
				SelectedAudioDevice = audioDevice;
				SelectedVideoDevice = videoDevice;

				// Remember our initial capture state.
				bool wasCaptured = CaptureSource.State == CaptureState.Started;
				if (wasCaptured)
				{
					CaptureSource.Stop();
				}

				CaptureSource.AudioCaptureDevice = audioDevice;
				CaptureSource.VideoCaptureDevice = videoDevice ?? CaptureSource.VideoCaptureDevice;
				ConfigureAudioCaptureDevice(CaptureSource.AudioCaptureDevice);
				ConfigureVideoCaptureDevice(CaptureSource.VideoCaptureDevice);

				// Restore capture state to how it originally was.
				if (wasCaptured)
				{
					CaptureSelectedInputDevices();
				}
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Error updating captured devices");
				MessageService.Instance.ShowErrorHint(ex.Message);
			}
		}

		private static void ConfigureAudioCaptureDevice(AudioCaptureDevice device)
		{
			// Set the audio properties.
			if (device != null)
			{
				MediaDeviceConfig.SelectBestAudioFormat(device);
				device.AudioFrameSize = AudioConstants.MillisecondsPerFrame; // 20 milliseconds
				if (device.DesiredFormat == null)
				{
					ClientLogger.Error(CommonStrings.Media_NoAudioFormat);
					MessageService.Instance.ShowErrorHint(CommonStrings.Media_NoAudioFormat);
				}
			}
			else
			{
				// Only show an error if there really is no microphone attached.
				var audioDevices = CaptureDeviceConfiguration.GetAvailableAudioCaptureDevices();
				if (audioDevices.Count == 0)
				{
					ClientLogger.Debug(CommonStrings.Media_NoAudioDevice);
					MessageService.Instance.ShowErrorHint(CommonStrings.Media_NoAudioDevice);
				}
			}
		}

		private static void ConfigureVideoCaptureDevice(VideoCaptureDevice device)
		{
			// Configure the video capture device.
			// The weird thing about this is that sometimes (at least on a Macintosh), a video capture device
			// can have an empty device.SupportedFormats collection, but still be able to capture video.  
			// Generally that format seems to work, but I don't think we can guarantee that.
			if (device != null)
			{
				MediaDeviceConfig.SelectBestVideoFormat(device);
				if (device.DesiredFormat == null)
				{
					ClientLogger.Debug("No appropriate video format was found; current format = {0}.", device.DesiredFormat);

					// ks 12/13/10 - Since limited testing has shown that some cameras on the Mac that work fine have an empty SupportedFormats collection,
					// we'd rather not show this error on Mac platforms.  There may be other instances where we don't want to show an error,
					// and there may also be a better way of handling this situation in general.  But good enough for now.
					if (Environment.OSVersion.Platform != PlatformID.MacOSX)
					{
						ClientLogger.Error(CommonStrings.Media_NoVideoFormat);
						MessageService.Instance.ShowErrorHint(CommonStrings.Media_NoVideoFormat);
					}
				}
			}
			else
			{
				// Only show an error if there really is no webcam attached.
				var videoDevices = CaptureDeviceConfiguration.GetAvailableVideoCaptureDevices();
				if (videoDevices.Count == 0)
				{
					ClientLogger.Debug(CommonStrings.Media_NoVideoDevice);
					MessageService.Instance.ShowErrorHint(CommonStrings.Media_NoVideoDevice);
				}
			}
		}

		/// <summary>
		/// Captures the selected input devices.
		/// </summary>
		private void CaptureSelectedInputDevices()
		{
			try
			{

				// Temporary!!!
				// throw new Exception("Trying to simulate device not functioning exception");
				if (CaptureSource != null && CaptureSource.State != CaptureState.Started)
				{
					if (CaptureDeviceConfiguration.AllowedDeviceAccess || CaptureDeviceConfiguration.RequestDeviceAccess())
					{
						CapturingAudio = false;
						CapturingVideo = false;
						AudioSink = _mediaSinkFactory.GetAudioSink(CaptureSource);
						VideoSink = _mediaSinkFactory.GetVideoSink(CaptureSource);
						AudioSink.CaptureSuccessful += AudioSink_CaptureSuccessful;
						VideoSink.CaptureSuccessful += VideoSink_CaptureSuccessful;
						CaptureSource.Start();
						if (CaptureSource.State == CaptureState.Started)
						{
							RaiseCaptureStarted();
							InitializerCaptureTimer();
						}
						else
						{
							RaiseCaptureFailed();
						}
					}
					else
					{
						ClientLogger.Debug("Unable to obtain authorization to capture media device(s)");
					}
				}
			}
			catch (Exception ex)
			{
				HandleCaptureError(ex);
			}
		}

		private void HandleCaptureError(Exception ex)
		{
			ClientLogger.DebugException(ex, "Capture devices failed");
			RaiseCaptureFailed();
			if (CaptureSource != null)
			{
				if (CaptureSource.State == CaptureState.Started)
				{
					CaptureSource.Stop();
				}
				CaptureSource.VideoCaptureDevice = null;
			}
		}

		private void RaiseCaptureStarted()
		{
			var handlers = CaptureStarted;
			if (handlers != null)
			{
				handlers(this, EventArgs.Empty);
			}
		}

		private void RaiseCaptureSucceeded()
		{
			var handlers = CaptureSucceeded;
			if (handlers != null)
			{
				var args = new EventArgs<bool, bool>(CapturingAudio, CapturingVideo);
				handlers(this, args);
			}
		}

		private void RaiseCaptureFailed()
		{
			var handlers = CaptureFailed;
			if (handlers != null)
			{
				var args = new EventArgs<bool, bool>(CapturingAudio, CapturingVideo);
				handlers(this, args);
			}
		}

		/// <summary>
		/// Saves the selected audio capture source back to the local config file after it's successfully captured data.
		/// </summary>
		private void SaveAudioCaptureSource()
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				try
				{
					if (CaptureSource != null && _mediaDeviceConfig != null)
					{
						if (CaptureSource.AudioCaptureDevice != null)
						{
							_mediaDeviceConfig.AudioCaptureDeviceFriendlyName = CaptureSource.AudioCaptureDevice.FriendlyName;
						}
						var helper = new ConfigurationHelper<MediaDeviceConfig>();
						helper.SaveConfigurationObject(_mediaDeviceConfig, MediaDeviceConfig.DefaultFileName);
					}
				}
				catch (Exception ex)
				{
					ClientLogger.ErrorException(ex, "Unable to save audio capture source");
				}
			});
		}

		/// <summary>
		/// Saves the selected video capture source back to the local config file after it's successfully captured data.
		/// </summary>
		private void SaveVideoCaptureSource()
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				try
				{
					if (CaptureSource != null && _mediaDeviceConfig != null)
					{
						if (CaptureSource.VideoCaptureDevice != null)
						{
							_mediaDeviceConfig.VideoCaptureDeviceFriendlyName = CaptureSource.VideoCaptureDevice.FriendlyName;
						}
						var helper = new ConfigurationHelper<MediaDeviceConfig>();
						helper.SaveConfigurationObject(_mediaDeviceConfig, MediaDeviceConfig.DefaultFileName);
					}
				}
				catch (Exception ex)
				{
					ClientLogger.ErrorException(ex, "Unable to save video capture source");
				}
			});
		}

		private void InitializerCaptureTimer()
		{
			_captureTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
			_captureTimer.Tick += captureTimer_Tick;
			_captureTimer.Start();
		}

		void captureTimer_Tick(object sender, EventArgs e)
		{
			// If we've gone more than five seconds since capture, give the user a chance to configure their microphone and webcam.
			if (CaptureSource == null) return;
			_captureTimer.Stop();
			if (CaptureSource.AudioCaptureDevice != null && !CapturingAudio)
			{
				PossibleAudioDevices.SetFailed(CaptureSource.AudioCaptureDevice);
				var nextAudioDevice = PossibleAudioDevices.GetNextDevice();
				if (nextAudioDevice != null && UseAutomaticSelection)
				{
					ClientLogger.Debug("No audio coming in from device {0}; trying device {1}", CaptureSource.AudioCaptureDevice.FriendlyName, nextAudioDevice.FriendlyName);
					ChangeCapturedDevices(nextAudioDevice, CaptureSource.VideoCaptureDevice);
				}
				else
				{
					ClientLogger.Debug("No audio coming in from device {0}; giving up.", CaptureSource.AudioCaptureDevice.FriendlyName);
					RaiseCaptureFailed();
				}
			}
			if (CaptureSource.VideoCaptureDevice != null && !CapturingVideo)
			{
				PossibleVideoDevices.SetFailed(CaptureSource.VideoCaptureDevice);
				var nextVideoDevice = PossibleVideoDevices.GetNextDevice();
				if (nextVideoDevice != null && UseAutomaticSelection)
				{
					ClientLogger.Debug("No video coming in from device {0}; trying device {1}", CaptureSource.VideoCaptureDevice.FriendlyName, nextVideoDevice.FriendlyName);
					ChangeCapturedDevices(CaptureSource.AudioCaptureDevice, nextVideoDevice);
				}
				else
				{
					ClientLogger.Debug("No video coming in from device {0}; giving up.", CaptureSource.VideoCaptureDevice.FriendlyName);
					RaiseCaptureFailed();
				}
			}
		}

		void VideoSink_CaptureSuccessful(object sender, EventArgs e)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				try
				{
					CapturingVideo = true;
					PossibleVideoDevices.SetSucceeded(CaptureSource.VideoCaptureDevice);
					SaveVideoCaptureSource();
					RaiseCaptureSucceeded();
					ClientLogger.Debug("Successfully capturing video from device {0}", CaptureSource.VideoCaptureDevice);
				}
				catch (Exception ex)
				{
					ClientLogger.Debug(ex.ToString);
				}
			});
		}

		void AudioSink_CaptureSuccessful(object sender, EventArgs e)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				try
				{
					CapturingAudio = true;
					PossibleAudioDevices.SetSucceeded(CaptureSource.AudioCaptureDevice);
					SaveAudioCaptureSource();
					RaiseCaptureSucceeded();
					ClientLogger.Debug("Successfully capturing audio from device {0}", CaptureSource.AudioCaptureDevice);
				}
				catch (Exception ex)
				{
					ClientLogger.Debug(ex.ToString);
				}
			});
		}

		bool _disposed;
		public void Dispose()
		{
			Dispose(true);
		}
		protected void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					// Stop any active processes.
					if (_captureTimer != null)
					{
						_captureTimer.Stop();
					}

					Stop();
					if (CaptureSource != null)
					{
						CaptureSource.Stop();
					}

					// Release all references to help with the double-reference problem that keeps stuff from getting garbage collected.
					CaptureSource = null;
					AudioSink = null;
					VideoSink = null;
				}
				_disposed = true;
			}
		}
		#endregion

		#region Local Classes
		public enum CaptureStatus
		{
			Untried,
			Succeeded,
			Failed
		}

		public class MediaDevice<T> where T : CaptureDevice
		{
			public MediaDevice(T device)
			{
				Device = device;
				CaptureStatus = CaptureStatus.Untried;
			}

			public T Device { get; private set; }
			public CaptureStatus CaptureStatus { get; set; }
		}

		public class MediaDeviceList<T> : List<MediaDevice<T>> where T : CaptureDevice
		{
			public MediaDeviceList(Func<T> getBestDeviceFunc, IEnumerable<T> devices)
			{
				_getBestDeviceFunc = getBestDeviceFunc;
				foreach (var device in devices)
				{
					Add(new MediaDevice<T>(device));
				}
			}

			private T _lastDevice;
			private readonly Func<T> _getBestDeviceFunc;

			public T GetNextDevice()
			{
				if (_lastDevice == null)
				{
					_lastDevice = _getBestDeviceFunc();
				}
				else
				{
					var mediaDevice = this.FirstOrDefault(md => md.CaptureStatus == CaptureStatus.Untried);
					_lastDevice = mediaDevice == null ? null : mediaDevice.Device;
				}
				return _lastDevice;
			}

			public void SetSucceeded(T device)
			{
				var mediaDevice = this.FirstOrDefault(md => md.Device == device);
				if (mediaDevice != null)
				{
					mediaDevice.CaptureStatus = CaptureStatus.Succeeded;
				}
			}

			public void SetFailed(T device)
			{
				var mediaDevice = this.FirstOrDefault(md => md.Device == device);
				if (mediaDevice != null)
				{
					mediaDevice.CaptureStatus = CaptureStatus.Failed;
				}
			}

			public override string ToString()
			{
				var sb = new StringBuilder();
				foreach (var device in this)
				{
					sb.Append(device.Device.FriendlyName + " (" + device.CaptureStatus + ")\r\n");
				}
				return sb.ToString().TrimEnd();
			}

		}
		#endregion

	}
}
