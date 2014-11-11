using System;
using System.Linq;
using Alanta.Client.Common.Loader;
using Alanta.Client.Data;
using Alanta.Client.Media;
using Alanta.Client.UI.Common.RoomView;
using Alanta.Client.UI.Desktop.RoomView;
using Alanta.Client.UI.Common.ViewModels;

namespace Alanta.Client.Test.Media
{
	public class DestinationRoomController : DesktopRoomController
	{
		public DestinationRoomController(MediaController mediaController, 
			IViewModelFactory viewModelFactory, 
			IRoomInfo roomInfo, 
			IConfigurationService configurationService, 
			RoomViewModel sourceRoomViewModel, 
			Guid sessionId)
			: base(viewModelFactory, roomInfo, configurationService)
		{
			RoomVm.MediaController = mediaController;
			LocalUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			var sessionCollectionViewModel = viewModelFactory.GetViewModel<SessionCollectionViewModel>();
			sourceRoomViewModel.RoomName = sourceRoomViewModel.RoomName;
			sourceRoomViewModel.SessionId = sessionId;
			sourceRoomViewModel.UserTag = sourceRoomViewModel.UserTag;
			sourceRoomViewModel.Model = sourceRoomViewModel.Model;
			sourceRoomViewModel.SessionVm = sessionCollectionViewModel.ViewModels.First(s => s.Model.SessionId == sessionId);
		}

	}
}
