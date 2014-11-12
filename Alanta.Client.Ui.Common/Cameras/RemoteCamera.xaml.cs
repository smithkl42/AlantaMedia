using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Ui.Common.Cameras
{
    public partial class RemoteCamera : UserControl, IDisposable
    {
        #region Constructors

        private MediaStreamSource _mediaStreamSource;

        public RemoteCamera()
        {
            InitializeComponent();
            Loaded += RemoteCamera_Loaded;
        }

        public void Initialize(MediaStreamSource mediaStreamSource)
        {
            _mediaStreamSource = mediaStreamSource;
        }

        private void RemoteCamera_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isLoaded)
                {
                    return;
                }
                _isLoaded = true;
                mediaElement.BufferingTime = TimeSpan.FromMilliseconds(100);
                mediaElement.SetSource(_mediaStreamSource);
                mediaElement.Play();
            }
            catch (Exception ex)
            {
                ClientLogger.ErrorException(ex);
            }
        }

        #endregion

        #region Fields and Properties

        private const double ratioWidth = 1.33; //1.42;
        private const double maxWidth = 640;
        private const double maxHeight = 480;
        private const double minWidth = 80;
        private const double minHeight = 60;
        private const double additionalHeight = 20; // 16 - first row, 4 - second row
        private bool _isLoaded;
        private IDisposable _userObserver;

        #endregion

        #region Methods

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = base.MeasureOverride(availableSize);
            return size;
        }

        private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var size = e.NewSize;
            var cornerRadius = 0;
            if (size.Height > 190)
            {
                cornerRadius = 5;
            }
            else if (size.Height > 150)
            {
                cornerRadius = 4;
            }
            else if (size.Height > 120)
            {
                cornerRadius = 3;
            }

            // round corners
            videoRectGeometry.Rect = new Rect(0, 0, size.Width, size.Height);
            videoRectGeometry.RadiusX = cornerRadius;
            videoRectGeometry.RadiusY = cornerRadius;
        }

        private void LayoutRoot_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        private void LayoutRoot_MouseLeave(object sender, MouseEventArgs e)
        {
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
            if (_disposed)
            {
                return;
            }
            try
            {
                if (_userObserver != null)
                {
                    _userObserver.Dispose();
                }
                if (mediaElement != null && mediaElement.CurrentState != MediaElementState.Closed)
                {
                    mediaElement.Stop();
                    mediaElement = null;
                }
            }
            catch (Exception ex)
            {
                ClientLogger.DebugException(ex, "Error on disposing");
            }
            _disposed = true;
        }

        #endregion
    }
}