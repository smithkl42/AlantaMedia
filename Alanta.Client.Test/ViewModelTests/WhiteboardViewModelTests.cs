using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Threading;
using System.Windows;
using Alanta.Client.Common.Logging;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.ViewModels;
using Alanta.Common;
using Alanta.Model;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Alanta.Client.Common;
using ReactiveUI.Testing;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class WhiteboardViewModelTests : ViewModelTestBase
	{

		[Tag("viewmodel")]
		[Tag("whiteboard")]
		[Tag("whiteboardviewmodel")]
		[TestMethod]
		public void CalculatedPropertiesTest()
		{
			// Arrange
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			localUserVm.Model = guestUser;
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			var stopSharingGrant = new RoomPermissionGrant()
			{
				RoomPermissionGrantId = Guid.NewGuid(),
				RoomPermissionScopeTag = RoomPermissionScopeValues.User,
				RoomPermissionTag = RoomPermissionValues.StopSharingOwnItem,
				UserId = guestUser.UserId,
				RoomId = room.RoomId
			};
			var stopSharingGrantVm = viewModelFactory.GetViewModel<RoomPermissionGrantViewModel>(vm => vm.Model.RoomPermissionGrantId == stopSharingGrant.RoomPermissionGrantId);
			stopSharingGrantVm.Model = stopSharingGrant;
			localUserVm.RoomPermissionGrants.ViewModels.Add(stopSharingGrantVm);

			// Act and Assert
			var unownedWb = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = user };
			var unownedWbVm = viewModelFactory.GetViewModel<WhiteboardViewModel>(wvm => wvm.WorkspaceItemId == unownedWb.WhiteboardId);
			Assert.IsFalse(unownedWbVm.IsReady);

			Assert.AreEqual(Guid.Empty, unownedWbVm.WorkspaceItemId);
			Assert.AreEqual(Guid.Empty, unownedWbVm.OwnerUserId);
			Assert.AreEqual(string.Empty, unownedWbVm.OwnerUserName);
			unownedWbVm.Model = unownedWb;
			Assert.AreEqual(unownedWb.WhiteboardId, unownedWbVm.WorkspaceItemId);
			Assert.AreEqual(unownedWb.User.UserId, unownedWbVm.OwnerUserId);
			Assert.AreEqual(unownedWb.User.UserName, unownedWbVm.OwnerUserName);

			unownedWb.IsClosed = null;
			Assert.IsTrue(unownedWbVm.IsShared);
			unownedWbVm.IsShared = true;
			Assert.AreEqual(true, unownedWbVm.IsShared);
			Assert.AreEqual(false, unownedWb.IsClosed);
			unownedWbVm.IsShared = false;
			Assert.AreEqual(false, unownedWbVm.IsShared);
			Assert.AreEqual(true, unownedWb.IsClosed);

			Assert.IsTrue(unownedWbVm.IsReady);
			Assert.IsFalse(unownedWbVm.IsShareAvailable);
			Assert.IsFalse(unownedWbVm.IsUnshareAvailable);
			Assert.IsFalse(unownedWbVm.IsDeleteAvailable);

			var ownedWb = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = guestUser };
			var ownedWbVm = viewModelFactory.GetViewModel<WhiteboardViewModel>(wvm => wvm.WorkspaceItemId == ownedWb.WhiteboardId);
			Assert.IsFalse(ownedWbVm.IsReady);
			Assert.AreEqual(Guid.Empty, ownedWbVm.WorkspaceItemId);
			Assert.AreEqual(Guid.Empty, ownedWbVm.OwnerUserId);
			Assert.AreEqual(string.Empty, ownedWbVm.OwnerUserName);
			ownedWbVm.Model = ownedWb;
			Assert.AreEqual(ownedWb.WhiteboardId, ownedWbVm.WorkspaceItemId);
			Assert.AreEqual(ownedWb.User.UserId, ownedWbVm.OwnerUserId);
			Assert.AreEqual(ownedWb.User.UserName, ownedWbVm.OwnerUserName);
			Assert.IsTrue(ownedWbVm.IsReady);
			Assert.IsTrue(ownedWbVm.IsShareAvailable);
			Assert.IsTrue(ownedWbVm.IsUnshareAvailable);
			Assert.IsTrue(ownedWbVm.IsDeleteAvailable);
		}

		[Tag("viewmodel")]
		[Tag("whiteboard")]
		[Tag("whiteboardviewmodel")]
		[TestMethod]
		public void IWorkspaceItemPropertiesTest()
		{
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			localUserVm.Model = guestUser;
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			var now = DateTime.Now;
			var wb = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = guestUser, CreatedOn = now };
			var wbvm = viewModelFactory.GetViewModel<WhiteboardViewModel>(wvm => wvm.WorkspaceItemId == wb.WhiteboardId);
			wbvm.Model = wb;

			Assert.AreEqual("Whiteboard", wbvm.Title);
			Assert.AreEqual(Constants.WhiteboardIconLocation, wbvm.IconLocation);
			Assert.AreEqual(wbvm.Model.WhiteboardId, wbvm.WorkspaceItemId);
			Assert.AreEqual(guestUser.UserId, wbvm.OwnerUserId);
			Assert.AreEqual(guestUser.UserName, wbvm.OwnerUserName);
			Assert.AreEqual(now, wbvm.CreatedOn);
		}

		[Tag("viewmodel")]
		[Tag("whiteboard")]
		[Tag("whiteboardviewmodel")]
		[TestMethod]
		public void AddWhiteboardTest()
		{
			var now = DateTime.Now;
			var whiteboardViewModelCollection = viewModelFactory.GetViewModel<WhiteboardCollectionViewModel>();
			var whiteboards = new ObservableCollection<Data.RoomService.Whiteboard>();
			var wb1 = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = user, CreatedOn = now };

			whiteboards.Add(wb1);
			whiteboardViewModelCollection.Models = whiteboards;
			Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.Model == wb1));
			Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.WorkspaceItemId == wb1.WhiteboardId));

			var wb2 = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = guestUser, CreatedOn = now };
			whiteboards.Add(wb2);
			Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.Model == wb2));
			Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.WorkspaceItemId == wb2.WhiteboardId));
			Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.Model == wb1));
			Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.WorkspaceItemId == wb1.WhiteboardId));
		}

		/// <summary>
		/// Pop back and forth on various different threads to see if that makes any difference.
		/// </summary>
		[Tag("viewmodel")]
		[Tag("whiteboard")]
		[Tag("whiteboardviewmodel")]
		[Asynchronous]
		[TestMethod]
		public void ThreadedAddWhiteboardTest()
		{
			bool isFinished = false;
			var now = DateTime.Now;

			// Do some stuff on a threadpool.
			EnqueueCallback(() => Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					var whiteboardViewModelCollection = viewModelFactory.GetViewModel<WhiteboardCollectionViewModel>();
					var whiteboards = new ObservableCollection<Data.RoomService.Whiteboard>();
					var wb1 = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = user, CreatedOn = now };
					var wb2 = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = guestUser, CreatedOn = now };
					var wb3 = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = guestUser, CreatedOn = now };

					ThreadPool.QueueUserWorkItem(o =>
					{
						whiteboards.Add(wb1);
						whiteboardViewModelCollection.Models = whiteboards;
						Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.Model == wb1));
						Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.WorkspaceItemId == wb1.WhiteboardId));

						Deployment.Current.Dispatcher.BeginInvoke(() =>
						{
							whiteboards.Add(wb2);
							Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.Model == wb2));
							Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.WorkspaceItemId == wb2.WhiteboardId));

							ThreadPool.QueueUserWorkItem(o2 =>
							{
								whiteboards.Add(wb3);
								Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.Model == wb3));
								Assert.IsTrue(whiteboardViewModelCollection.ViewModels.Any(vm => vm.WorkspaceItemId == wb3.WhiteboardId));
								isFinished = true;
							});
						});
					});
				}));
			EnqueueConditional(() => isFinished);
			EnqueueTestComplete();
		}

		[Tag("viewmodel")]
		[Tag("whiteboard")]
		[Tag("whiteboardviewmodel")]
		[TestMethod]
		public void WorkspaceItemIdTest()
		{
			(new EventLoopScheduler()).With(_ =>
			{
				bool workspaceItemIdPropertyChanged = false;
				var wb = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = user };
				var wbVm = viewModelFactory.GetViewModel<WhiteboardViewModel>(vm => vm.WorkspaceItemId == wb.WhiteboardId);

				wbVm.PropertyChanged += (s, e) =>
				{
					if (e.PropertyName == wbVm.GetPropertyName(x => x.WorkspaceItemId))
					{
						workspaceItemIdPropertyChanged = true;
					}
				};

				Assert.AreEqual(Guid.Empty, wbVm.WorkspaceItemId);
				Assert.AreEqual(Guid.Empty, wbVm.OwnerUserId);
				Assert.IsTrue(string.IsNullOrEmpty(wbVm.OwnerUserName));
				wbVm.Model = wb;

				Thread.Sleep(1000);
				Assert.AreEqual(wb.WhiteboardId, wbVm.WorkspaceItemId);
				Assert.AreEqual(wb.User.UserId, wbVm.OwnerUserId);
				Assert.AreEqual(wb.User.UserName, wbVm.OwnerUserName);
				Assert.IsTrue(workspaceItemIdPropertyChanged);
			});
		}

		[Tag("viewmodel")]
		[Tag("dispatcher")]
		[TestMethod]
		public void DispatcherTest()
		{
			Deployment.Current.Dispatcher.BeginInvoke(() => ClientLogger.Debug("Dispatcher.BeginInvoke() called."));
		}

	}
}
