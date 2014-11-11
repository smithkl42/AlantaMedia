using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Alanta.Client.Common;
using Alanta.Client.Common.Loader;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;

namespace Alanta.Client.Ui.Controls.Cameras
{
    public partial class LocalCamera : UserControl
    {
        #region Constructors

        private string _roomId;
        private string _userId;

        public LocalCamera()
        {
            InitializeComponent();
            if (!DesignerProperties.IsInDesignTool)
            {
                Loaded += LocalCamera_Loaded;
            }
        }

        private void LocalCamera_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCaptureButtonsTimer();
            ThreadingHelper.DispatcherSleepAsync(TimeSpan.FromSeconds(3), () => ButtonsZoomStoryboard.Begin());
        }

        public void Initialize(MediaElement mediaElement, string userId, string roomId)
        {
            _mediaElement = mediaElement;
            _userId = userId;
            _roomId = roomId;
            try
            {
                InitializeMedia();
                UpdatePanel();
                ConnectToMediaServer();
                InitializeConnectTimer();

                // This may not succeed (for instance, if the user hasn't authorized the Alanta client to access his webcam)
                // but if it does, it's the best way, since it spins up the webcam automatically. If it fails, it will fail silently.
                // Note: we need to call BeginInvoke here, to avoid showing the "Access for camera and microphone" popup:
                // because we get here when a user on configuration screen has clicked "Leave a message" 
                Dispatcher.BeginInvoke(Connect);
            }
            catch (Exception ex)
            {
                ClientLogger.ErrorException(ex);
                _messageService.ShowErrorMessage(ex.Message);
            }
        }

        #endregion

        #region Fields and Properties

        // private ErrorCollectionViewModel errors;
        private DispatcherTimer _captureButtonsTimer;
        private VisualStates _currentVisualState = VisualStates.Default;
        private bool _ignoreMediaRecommendation;
        private bool _isPnlMutedCanBeVisible;
        private DateTime _lastConnectMessageTime;
        private MediaController _mediaController;
        private MediaDevices _mediaDevices;
        private MediaElement _mediaElement;
        private IMessageService _messageService;
        private DispatcherTimer _reconnectTimer;
        public MediaStreamSource AudioStreamSource { get; private set; }
        // devices what user selected

        protected VisualStates CurrentVisualState
        {
            get { return _currentVisualState; }
            set
            {
                if (_currentVisualState == value)
                    return;
                OnVisualStateChanging(_currentVisualState, value);
                _currentVisualState = value;
                VisualStateManager.GoToState(this, _currentVisualState.ToString(), false);
            }
        }

        #endregion

        #region Event Handlers

        private void btnCaptureMicrophone_Click(object sender, RoutedEventArgs e)
        {
            _mediaDevices.CaptureSource.VideoCaptureDevice = null;
            Connect();
        }

        private void btnCaptureWebCam_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private void OnCapturedStarted()
        {
            mutedIconsPanel.Visibility = Visibility.Visible;
            _isPnlMutedCanBeVisible = true;
        }

        private void localMediaControl_DeviceCaptureChanged(object sender,
            EventArgs<AudioCaptureDevice, VideoCaptureDevice> e)
        {
            _mediaDevices.UseAutomaticSelection = false;
            _mediaDevices.ChangeCapturedDevices(e.Value1, e.Value2);
            UpdatePanel();
        }

        private void btnDisplayCapturePanel_Click(object sender, RoutedEventArgs e)
        {
            _ignoreMediaRecommendation = true;
            UpdatePanel();
        }

        #endregion

        #region Public Methods

        public void Connect()
        {
            try
            {
                CurrentVisualState = VisualStates.Progress;
                _mediaDevices.Start();
                if (_mediaDevices.CaptureSource.State == CaptureState.Started)
                {
                    OnCapturedStarted();
                }
            }
            catch (Exception ex)
            {
                ClientLogger.ErrorException(ex);
                _messageService.ShowErrorHint(ex.Message);
            }
            finally
            {
                UpdatePanel();
            }
        }

        public void Disconnect()
        {
            _mediaDevices.Stop();
        }

        #endregion

        #region Private Methods

        private bool IsPlaying
        {
            get
            {
                return _mediaElement != null
                       &&
                       (_mediaElement.CurrentState == MediaElementState.Opening ||
                        _mediaElement.CurrentState == MediaElementState.Playing);
            }
        }

        private void ConnectToMediaServer()
        {
            if (_mediaController.IsConnected || _mediaController.IsConnecting || string.IsNullOrWhiteSpace(_userId)) return;
            ClientLogger.Debug("Attempting to connect to media server.");
            _mediaController.Connect(_roomId, OnRtpAudioManagerConnected);
        }

        private void InitializeMedia()
        {
            // Start playing whatever we can get from the media controller
            AudioStreamSource = new AudioMediaStreamSource(_mediaController, _mediaController.PlayedAudioFormat);
            if (!IsPlaying)
            {
                _mediaElement.SetSource(AudioStreamSource);
                _mediaElement.Play();
            }
        }

        private void OnVisualStateChanging(VisualStates oldState, VisualStates newState)
        {
            if (oldState == VisualStates.Video)
            {
                videoCapture.Fill = null;
            }

            if (newState == VisualStates.Video)
            {
                var videoBrush = new VideoBrush();
                videoBrush.SetSource(_mediaDevices.CaptureSource);
                videoCapture.Fill = videoBrush;
            }
        }

        private void UpdatePanel()
        {
            if (_mediaDevices.CaptureSource != null && _mediaDevices.CaptureSource.State == CaptureState.Started)
            {
                // If we're capturing, start playing the resulting audio and video (or avatar).
                StopCaptureButtonsTimer();
            }
            else
            {
                // If we're not capturing, display the best options for doing so.
                CurrentVisualState = VisualStates.Default;
            }
        }

        private void InitializeCaptureButtonsTimer()
        {
            if (_captureButtonsTimer == null)
            {
                _captureButtonsTimer = new DispatcherTimer();
                _captureButtonsTimer.Interval = TimeSpan.FromSeconds(15);
                _captureButtonsTimer.Tick += captureButtonsTimer_Tick;
            }
            _captureButtonsTimer.Start();
        }

        private void StopCaptureButtonsTimer()
        {
            if (_captureButtonsTimer != null)
                _captureButtonsTimer.Stop();
        }

        private void captureButtonsTimer_Tick(object sender, EventArgs e)
        {
            // Slow down the interval at which the animation occurs, so that folks don't get too annoyed.
            _captureButtonsTimer.Interval += TimeSpan.FromSeconds(15);
            ButtonsZoomStoryboard.Begin();
        }

        private void InitializeConnectTimer()
        {
            _reconnectTimer = new DispatcherTimer();
            _reconnectTimer.Interval = TimeSpan.FromSeconds(MediaConstants.MediaServerReconnectInterval);
            _reconnectTimer.Tick += reconnectTimer_Tick;
            _reconnectTimer.Start();
        }


        private void reconnectTimer_Tick(object sender, EventArgs e)
        {
            ConnectToMediaServer();
        }

        protected void OnRtpAudioManagerConnected(Exception exception)
        {
            if (exception != null && !_disposed)
            {
                _lastConnectMessageTime = DateTime.Now;
                Deployment.Current.Dispatcher.BeginInvoke(() => _messageService.ShowErrorHint(exception.Message));
            }
            else
            {
                ClientLogger.Debug("Successfully connected to the media server.");
            }
        }

        #endregion

        #region IDisposable Members

        private bool _disposed;

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
                    if (_reconnectTimer != null)
                    {
                        _reconnectTimer.Stop();
                    }
                    Disconnect();
                    if (_mediaElement != null)
                    {
                        _mediaElement.Stop();
                    }
                    if (_captureButtonsTimer != null)
                    {
                        _captureButtonsTimer.Stop();
                    }
                    AudioStreamSource = null;
                    _mediaElement = null;
                    _reconnectTimer = null;
                }
                _disposed = true;
            }
        }

        #endregion

        #region IAlantaControl Members

        public void GoToInitialState()
        {
            // No-op
        }

        #endregion

        public enum VisualStates
        {
            Default,
            MediaNotRecommended,
            AudioNoImage,
            AudioAndImage,
            Video,
            Progress
        }
    }
}