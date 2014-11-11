using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Alanta.Client.Common;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.RoomView;
using Alanta.Client.UI.Desktop.RoomView;

namespace Alanta.Client.Test.Media
{
	public partial class DestinationRoomPage : UserControl
	{

		private readonly Dictionary<ushort, RemoteCamera> _activeRemoteCameras = new Dictionary<ushort, RemoteCamera>();

		public DestinationRoomPage()
		{
			InitializeComponent();
		}

		public void Initialize(DesktopRoomController roomController)
		{
			// Create the remote cameras.
			RoomController = roomController;
			var roomVm = roomController.RoomVm;

			foreach (var sessionVm in roomVm.SessionCollectionVm.RemoteSessions)
			{
				roomVm.MediaController.RegisterRemoteSession((ushort)sessionVm.Model.SsrcId);
				var remoteCamera = new RemoteCamera();
				remoteCamera.Margin = new Thickness(4);
				LayoutRoot.Children.Add(remoteCamera);
				ActiveRemoteCameras[(ushort)sessionVm.Model.SsrcId] = remoteCamera;
				remoteCamera.DataContext = sessionVm;
			}
			OnPageInitialized(RoomController);
		}

		#region IRoomPage Members

		public DesktopRoomController RoomController { get; set; }

		public void GoToUserRegisteredState()
		{
			throw new NotImplementedException();
		}

		public event EventHandler<EventArgs<DesktopRoomController>> PageInitialized;

		private void OnPageInitialized(DesktopRoomController roomController)
		{
			EventHandler<EventArgs<DesktopRoomController>> handler = PageInitialized;
			if (handler != null)
			{
				handler(this, new EventArgs<DesktopRoomController>(roomController));
			}
		}

		public Dictionary<ushort, RemoteCamera> ActiveRemoteCameras
		{
			get { return _activeRemoteCameras; }
		}

		public void AddWorkspacePage(Tools tool)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
