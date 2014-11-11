using System;
using System.Collections.ObjectModel;
using System.Windows;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.SampleData;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Silverlight.Testing;
using Moq;
using System.Linq;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class RoomViewModelTests : ViewModelTestBase
	{
		[TestMethod]
		[Tag("room")]
		[Tag("viewmodel")]
		[Tag("roomviewmodel")]
		public void RoomViewModelTest()
		{
			var vm = viewModelFactory.GetViewModel<RoomViewModel>();
			var wvm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
			wvm.WorkspaceItems.RegisterObservedCollection(vm.SharedFileCollectionVm.ViewModels);
			wvm.WorkspaceItems.RegisterObservedCollection(vm.WhiteboardCollectionVm.ViewModels);
			vm.Model = room;
			Assert.AreEqual(room.SharedFiles.Count, vm.SharedFileCollectionVm.ViewModels.Count);
			Assert.AreEqual(room.Whiteboards.Count, vm.WhiteboardCollectionVm.ViewModels.Count);
			Assert.AreEqual(room.SharedFiles.Count + room.Whiteboards.Count, wvm.WorkspaceItems.Count);
		}

		[TestMethod]
		[Tag("room")]
		[Tag("viewmodel")]
		[Tag("roomviewmodel")]
		public void SampleRoomViewModelTest()
		{
			var vm = new SampleRoomViewModel();
			Assert.AreEqual(2, vm.ModerationRequestCollectionVm.ViewModelCount);
			Assert.AreEqual(4, vm.RoomPermissionGrantCollectionVm.ViewModelCount);
		}

		[TestMethod]
		[Tag("room")]
		[Tag("viewmodel")]
		[Tag("roomviewmodel")]
		public void StateTest_IsLoading()
		{
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			Assert.AreEqual(RoomViewModel.RoomVisualState.Unknown, roomVm.VisualState);
			roomVm.LoadingState = RoomViewModel.RoomLoadingState.Loaded;
			// roomVm.IsLoading = false;
			Assert.AreNotEqual(RoomViewModel.RoomVisualState.Unknown, roomVm.VisualState);
		}

		[TestMethod]
		[Tag("room")]
		[Tag("viewmodel")]
		[Tag("roomviewmodel")]
		public void StateTest_GuestUserNoWorkspaceItems()
		{
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			var workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
			roomVm.LoadingState = RoomViewModel.RoomLoadingState.Loaded;
			// roomVm.IsLoading = false;
			Assert.AreEqual(0, workspaceVm.OpenItemCount);
			localUserVm.Model = guestUser;
			Assert.AreEqual(RoomViewModel.RoomVisualState.EmptyGuestUserState, roomVm.VisualState);
		}

		[TestMethod]
		[Tag("room")]
		[Tag("viewmodel")]
		[Tag("roomviewmodel")]
		public void StateTest_RegisteredUserNoWorkspaceItems()
		{
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			var workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
			roomVm.LoadingState = RoomViewModel.RoomLoadingState.Loaded;
			// roomVm.IsLoading = false;
			Assert.AreEqual(0, workspaceVm.OpenItemCount);
			localUserVm.Model = user;
			Assert.AreEqual(RoomViewModel.RoomVisualState.EmptyRegisteredUserState, roomVm.VisualState);
		}

		[TestMethod]
		[Tag("room")]
		[Tag("viewmodel")]
		[Tag("roomviewmodel")]
		public void StateTest_GuestUserOneWorkspaceItem()
		{
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.LoadingState = RoomViewModel.RoomLoadingState.Loaded;
			// roomVm.IsLoading = false;
			var workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
			workspaceVm.OpenWorkspaceItems.Add(viewModelFactory.GetViewModel<SharedFileViewModel>());
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			localUserVm.Model = guestUser;
			Assert.AreEqual(RoomViewModel.RoomVisualState.GuestUserState, roomVm.VisualState);
		}

		[TestMethod]
		[Tag("room")]
		[Tag("viewmodel")]
		[Tag("roomviewmodel")]
		public void StateTest_RegisteredUserOneWorkspaceItem()
		{
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.LoadingState = RoomViewModel.RoomLoadingState.Loaded;
			// roomVm.IsLoading = false;
			roomVm.Model = room;
			var workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
			workspaceVm.OpenWorkspaceItems.Add(viewModelFactory.GetViewModel<SharedFileViewModel>());
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			localUserVm.Model = user;
			Assert.AreEqual(RoomViewModel.RoomVisualState.RegisteredUserState, roomVm.VisualState);
		}

		[TestMethod]
		[Tag("room")]
		[Tag("viewmodel")]
		[Tag("roomviewmodel")]
		public void UserIdTest()
		{
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			Assert.AreEqual(Guid.Empty, roomVm.UserId);
			roomVm.Model = room;
			Assert.AreNotEqual(Guid.Empty, roomVm.UserId);
			Assert.AreEqual(room.UserId, roomVm.UserId);
		}

		[TestMethod]
		[Tag("room")]
		[Tag("viewmodel")]
		[Tag("roomviewmodel")]
		public void DisconnectEveryoneTest()
		{
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			localUserVm.Model = user;
			localUserVm.LoginSession = new LoginSession();
			var remoteSessionIds = roomVm.SessionCollectionVm.ViewModels
				.Where(vm => vm.Model.SessionId != roomVm.SessionId)
				.Select(vm => vm.Model.SessionId);
			mockRoomService.Setup(rs => rs.Disconnect(
				It.IsAny<LoginSession>(),
				It.IsAny<Guid>(),
				It.IsAny<ObservableCollection<Guid>>(),
				It.IsAny<OperationCallback>())).
				Callback((LoginSession loginSession, Guid roomId, ObservableCollection<Guid> sessionIds, OperationCallback callback) =>
				{
					Assert.AreEqual(localUserVm.LoginSession, loginSession);
					Assert.AreEqual(roomVm.Model.RoomId, roomId);
					Assert.AreEqual(remoteSessionIds.Count(), sessionIds.Count);
					foreach (var sessionId in remoteSessionIds)
					{
						Assert.IsTrue(sessionIds.Any(s => s == sessionId));
					}
				});
			roomVm.DisconnectEveryone();
			mockRoomService.VerifyAll();
		}


		//[Tag("room")]
		//[Tag("viewmodel")]
		//[Tag("roomviewmodel")]
		//[TestMethod]
		//public void TipVisibilityTest()
		//{
		//    var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
		//    var workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
		//    Assert.AreEqual(Visibility.Visible, roomVm.TipVisibility);
		//    workspaceVm.OpenWorkspaceItems.Add(viewModelFactory.GetViewModel<SharedFileViewModel>());
		//    Assert.AreEqual(Visibility.Collapsed, roomVm.TipVisibility);
		//}

		//[Tag("room")]
		//[Tag("viewmodel")]
		//[Tag("roomviewmodel")]
		//[TestMethod]
		//public void GuestHintVisibilityTest()
		//{
		//    var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
		//    var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
		//    localUserVm.Model = guestUser;
		//    Assert.AreEqual(Visibility.Visible, roomVm.GuestHintVisibility);
		//    localUserVm.Model = user;
		//    Assert.AreEqual(Visibility.Collapsed, roomVm.GuestHintVisibility);
		//}
	}
}
