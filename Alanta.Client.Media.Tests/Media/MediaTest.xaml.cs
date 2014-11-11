using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Alanta.Client.Common;
using Alanta.Client.Data;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.RoomView;
using Alanta.Client.UI.Common.ViewModels;
using Alanta.Client.UI.Desktop.RoomView;

namespace Alanta.Client.Test.Media
{
	public partial class MediaTest : Page
	{
		public MediaTest()
		{
			InitializeComponent();
			ActiveRemoteCameras = new Dictionary<ushort, RemoteCamera>();
		}

		private SourceRoomController _roomController;
		private IViewModelFactory _viewModelFactory;
		private IViewLocator _viewLocator;
		private TestRoomServiceAdapter _roomServiceAdapter;
		private TestMessageService _messageService;

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			// Create the source room controller (which will also create the remote sessions).
			_roomServiceAdapter = new TestRoomServiceAdapter();
			_messageService = new TestMessageService();
			_viewLocator = new ViewLocator();
			_viewModelFactory = new ViewModelFactory(_roomServiceAdapter, _messageService, _viewLocator);
			var configurationService = new NullConfigurationService();
			_roomController = new SourceRoomController(_viewModelFactory, new TestRoomInfo(), configurationService, this);

			// Create the local camera.
			localCamera.Initialize(_roomController.ViewModelFactory, _roomController.MediaElement);
			localCamera.Connect();

			var sessionCollectionVm = _viewModelFactory.GetViewModel<SessionCollectionViewModel>();
			
			foreach (Guid sessionId in _roomController._destinationMediaControllers.Keys)
			{
				// Create a "local" remoteCamera to display the remote session.
				var sessionVm = sessionCollectionVm.ViewModels.First(s => s.Model.SessionId == sessionId);
				var remoteCamera = new RemoteCamera();
				remoteCamera.Margin = new Thickness(2);
				sourceSessionStackPanel.Children.Add(remoteCamera);
				ActiveRemoteCameras[(ushort)(sessionVm.Model.SsrcId)] = remoteCamera;
				remoteCamera.DataContext = sessionVm;

				// Add the destination room pages to this page.
				remoteSessionsStackPanel.Children.Add(_roomController._destinationRoomPages[sessionId]);
			}

		}

		#region IRoomPage Members

		public DesktopRoomController RoomController
		{
			get { return _roomController; }
		}

		public void Load(string ownerUserId, string roomName, string userId, string loginSessionId)
		{
			throw new NotImplementedException();
		}

		public void GoToUserRegisteredState()
		{
			throw new NotImplementedException();
		}

		public event EventHandler<EventArgs<DesktopRoomController>> PageInitialized;

		public void OnPageInitialized(EventArgs<DesktopRoomController> e)
		{
			EventHandler<EventArgs<DesktopRoomController>> handler = PageInitialized;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		public Dictionary<ushort, RemoteCamera> ActiveRemoteCameras { get; set; }

		public void AddWorkspacePage(Tools tool)
		{
			throw new NotImplementedException();
		}

		#endregion

	}
}
