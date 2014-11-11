using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Alanta.Client.Common.Logging;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.ViewModels;
using ReactiveUI;

namespace Alanta.Client.UI.Common.RoomView
{
	public partial class RemoteCamera : UserControl, IDisposable
	{
		#region Constructors

		public RemoteCamera()
		{
			InitializeComponent();
			Loaded += RemoteCamera_Loaded;
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
				_sessionVm = DataContext as SessionViewModel;
				if (_sessionVm == null)
				{
					ClientLogger.ErrorException(new InvalidOperationException("The DataContext for the RemoteCamera was not set"));
					return;
				}
				_roomVm = _sessionVm.ViewModelFactory.GetViewModel<RoomViewModel>();
				// errors = sessionVm.ViewModelFactory.GetViewModel<ErrorCollectionViewModel>();
				try
				{
					_userObserver = _sessionVm.UserVm.ObservableForProperty(x => x.Model).Value()
						.CombineLatest(_sessionVm.UserVm.RegisteredUser.ObservableForProperty(x => x.AvatarLocation), (user, avatar) => user)
						.Subscribe(OnUserUpdated);
					OnUserUpdated(_sessionVm.UserVm.Model);
				}
				catch (Exception ex)
				{
					ClientLogger.ErrorException(ex);
				}

				if (DesignerProperties.IsInDesignTool)
				{
					return;
				}
				mediaElement.BufferingTime = TimeSpan.FromMilliseconds(100);
				try
				{
					mediaElement.SetSource(_sessionVm.MediaStreamSource);
				}
				catch (Exception ex)
				{
					// ks 10/13/11 - This can happen (in SL5?) if the MediaStreamSource is still associated with a previous mediaElement.
					// That can happen when the remote client leaves the room and then rejoins with the same SessionId, for instance,
					// when the WCF session faults.
					ClientLogger.Debug("An error occurred setting the MediaStreamSource for ssrcId {0}; recreating MediaStreamSource and trying again; error={1}", 
						_sessionVm.Model.SsrcId, ex.ToString());
					_sessionVm.CreateVideoMediaStreamSource(_roomVm.MediaController, _roomVm.MediaController.VideoQualityController);
					mediaElement.SetSource(_sessionVm.MediaStreamSource);
					ClientLogger.Debug("Successfully set MediaStreamSource for ssrcId {0}", _sessionVm.Model.SsrcId);
				}
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
		private RoomViewModel _roomVm;
		private SessionViewModel _sessionVm;
		// private ErrorCollectionViewModel errors;
		private IDisposable _userObserver;

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			var size = base.MeasureOverride(availableSize);

			//// set width of audio progress bar relative to camera size
			//if (!double.IsNaN(availableSize.Width) && !double.IsInfinity(availableSize.Width))
			//{
			//    double audioProgressBarWidth = Math.Round(availableSize.Width / 4, 0);
			//    audioProgressBarWidth = Math.Max(audioProgressBarWidth, 25);
			//    audioProgressBar.Width = audioProgressBarWidth;
			//    txtUserName.Margin = new Thickness(0, 0, audioProgressBarWidth + 10, 0);
			//}

			return size;
		}

		private void OnUserUpdated(User user)
		{
			var registeredUser = user as RegisteredUser;
			if (registeredUser != null && !string.IsNullOrEmpty(registeredUser.AvatarLocation))
			{
				string avatarUrl = ClientHelper.GetAvatarUrl(registeredUser);
				imgAvatar.Source = new BitmapImage(new Uri(avatarUrl, UriKind.Absolute));
				imgNoAvatar.Visibility = Visibility.Collapsed;
				imgAvatar.Visibility = Visibility.Visible;
			}
			else
			{
				imgNoAvatar.Visibility = Visibility.Visible;
				imgAvatar.Visibility = Visibility.Collapsed;
			}
		}

		public static Size GetMeasuredSize(Size availableSize)
		{
			double additionalHeight = RemoteCamera.additionalHeight;
			availableSize = new Size(availableSize.Width, availableSize.Height - additionalHeight);
			bool bwidth = availableSize.Width / ratioWidth < availableSize.Height;
			double width;
			double height;
			// calculate webcamera size
			if (bwidth)
			{
				width = Math.Max(minWidth, Math.Min(availableSize.Width, maxWidth));
				height = width / ratioWidth;
			}
			else
			{
				height = Math.Max(minHeight, Math.Min(availableSize.Height, maxHeight));
				width = height * ratioWidth;
			}

			// add additional needed vertical space (UserName etc)
			height = height + additionalHeight;
			// return needed space
			return new Size(width, height);
		}

		private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var size = e.NewSize;
			int cornerRadius = 0;
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
			if (_sessionVm.CanBeManaged)
			{
				remoteSessionControl.Visibility = Visibility.Visible;
			}
		}

		private void LayoutRoot_MouseLeave(object sender, MouseEventArgs e)
		{
			if (remoteSessionControl.CanBeHidden)
			{
				remoteSessionControl.Visibility = Visibility.Collapsed;
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