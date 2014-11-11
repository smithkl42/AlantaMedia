using System;
using System.Collections.ObjectModel;
using System.Linq;
using Alanta.Client.Common;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class SharedDesktopClientViewModelTests : ViewModelTestBase
	{
		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopclientviewmodel")]
		[TestMethod]
		public void SharedDesktopClientCollection_SynchronizationTest()
		{
			var viewerCollectionVm = viewModelFactory.GetViewModel<SharedDesktopClientCollectionViewModel>();
			viewerCollectionVm.Models = room.Sessions;
			Assert.AreEqual(room.Sessions.Count, viewerCollectionVm.ViewModels.Count);
			var session = new Session() { SessionId = Guid.NewGuid() };
			room.Sessions.Add(session);
			Assert.IsTrue(viewerCollectionVm.ViewModels.Any(vm => vm.Model == session));
			room.Sessions.Remove(session);
			Assert.IsFalse(viewerCollectionVm.ViewModels.Any(vm => vm.Model == session));
			Assert.AreEqual(room.Sessions.Count, viewerCollectionVm.ViewModels.Count);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopclientviewmodel")]
		[TestMethod]
		public void SharedDesktopClient_SessionUpdated()
		{
			var sessionId = Guid.NewGuid();
			var originalSession = new Session() { SessionId = sessionId };
			Assert.IsFalse(originalSession.IsSharingDesktop);
			var updatedSession = new Session()
			{
				SessionId = sessionId, SharedDesktopHost = "localhost", SharedDesktopPassword = "password", User = user
			};
			Assert.IsTrue(updatedSession.IsSharingDesktop);
			var vm = new SharedDesktopClientViewModel();
			vm.Initialize(viewModelFactory);
			vm.Model = originalSession;
			Assert.IsFalse(vm.IsShared);
			mockRoomService.Raise(rs => rs.SessionUpdated += null, new EventArgs<Session>(updatedSession));
			Assert.IsTrue(vm.IsShared);
		}

		/// <summary>
		/// Confirm that when a session is added or updated in the SessionCollectionVm.Models collection, 
		/// its corresponding SharedDesktopClientViewModel is updated.
		/// </summary>
		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopclientviewmodel")]
		[TestMethod]
		public void SyncWithSessionCollectionTest()
		{
			// Create and populate the session collection.
			var sessionId = Guid.NewGuid();
			var originalSession = new Session() { SessionId = sessionId };
			Assert.IsFalse(originalSession.IsSharingDesktop);
			var sessionCollectionVm = viewModelFactory.GetViewModel<SessionCollectionViewModel>();
			var sessions = new ObservableCollection<Session>();
			sessions.Add(originalSession);
			sessionCollectionVm.Models = sessions;

			// Create the shared desktop client collection and make sure it looks right.
			var viewerCollectionVm = viewModelFactory.GetViewModel<SharedDesktopClientCollectionViewModel>();
			viewerCollectionVm.Models = sessions;
			Assert.AreEqual(sessions.Count, viewerCollectionVm.ViewModels.Count);
			var vm = viewerCollectionVm.ViewModels.First();
			Assert.AreEqual(originalSession, vm.Model);
			Assert.IsFalse(vm.IsShared);

			// Register the shared desktop client collection with the Workspace and make sure it looks right.
			var workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
			workspaceVm.WorkspaceItems.RegisterObservedCollection(viewerCollectionVm.ViewModels);
			Assert.AreEqual(sessions.Count, workspaceVm.WorkspaceItems.Count);
			Assert.AreEqual(0, workspaceVm.OpenWorkspaceItems.Count); // It's not in the open list because it's not shared.
			Assert.AreEqual(0, workspaceVm.ClosedWorkspaceItems.Count); // It's not in the closed list because it's not shareable.
			Assert.IsTrue(workspaceVm.WorkspaceItems.Any(item => item.WorkspaceItemId == sessionId));

			// Update the session, and confirm everything still looks right.
			var updatedSession = new Session() { SessionId = sessionId, SharedDesktopHost = "localhost", SharedDesktopPassword = "password" };
			Assert.IsTrue(updatedSession.IsSharingDesktop);
			mockRoomService.Raise(rs => rs.SessionUpdated += null, new EventArgs<Session>(updatedSession));
			Assert.AreEqual(sessions.Count, viewerCollectionVm.ViewModels.Count);
			Assert.IsTrue(vm.IsShared);
			Assert.AreEqual(sessions.Count, workspaceVm.WorkspaceItems.Count);
			Assert.AreEqual(1, workspaceVm.OpenWorkspaceItems.Count);
			Assert.AreEqual(0, workspaceVm.ClosedWorkspaceItems.Count);
			Assert.IsTrue(workspaceVm.WorkspaceItems.Any(item => item.WorkspaceItemId == sessionId));
			Assert.IsTrue(workspaceVm.OpenWorkspaceItems.Any(item => item.WorkspaceItemId == sessionId));
		}

	}
}
