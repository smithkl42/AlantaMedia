using System;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.SampleData;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class UserViewModelTests : ViewModelTestBase
	{

		private string loginSessionId;

		public UserViewModelTests()
		{
			loginSessionId = Guid.NewGuid().ToString();
		}

		[TestMethod]
		[Tag("user")]
		[Tag("viewmodel")]
		[Tag("userviewmodel")]
		public void ModelTest()
		{
			var vm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			var wvm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
			vm.Model = user;
			wvm.WorkspaceItems.RegisterObservedCollection(vm.UserSharedFileCollectionVm.ViewModels);
			Assert.AreEqual(user.SharedFiles.Count, vm.UserSharedFileCollectionVm.ViewModels.Count);
			Assert.AreEqual(user.SharedFiles.Count, wvm.WorkspaceItems.Count);
			Assert.AreEqual(user, vm.Model);
			Assert.AreEqual(user.UserId, vm.UserId);
			Assert.AreEqual(user.UserName, vm.Model.UserName);
		}

		[TestMethod]
		[Tag("user")]
		[Tag("viewmodel")]
		[Tag("userviewmodel")]
		public void LoginSuccessfulTest()
		{
			var companyVm = viewModelFactory.GetViewModel<CompanyViewModel>();
			companyVm.Model = TestGlobals.Company;
			var authenticationGroupVm = viewModelFactory.GetViewModel<AuthenticationGroupViewModel>();
			authenticationGroupVm.Model = TestGlobals.AuthenticationGroup;
			var vm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			vm.CompanyInfo = new TestCompanyInfo();
			var user = DesignTimeHelper.GetRegisteredUser();
			mockRoomService.Setup(rs => rs.Login(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OperationCallback<LoginSession>>()))
				.Callback((string companyTag, string authenticationGroupTag, string userTag, string password, OperationCallback<LoginSession> callback) =>
					callback(null, new LoginSession() { LoginSessionId = loginSessionId, UserId = Guid.NewGuid()}));
			mockRoomService.Setup(rs => rs.GetAuthenticatedUser(It.IsAny<LoginSession>(), It.IsAny<OperationCallback<User>>()))
				.Callback((LoginSession loginSession, OperationCallback<User> callback) => callback(null, user));
			vm.Login("smithkl42", "password", ex =>
				{
					Assert.AreEqual("smithkl42", vm.UserTag);
					Assert.AreEqual("password", vm.Password);
					Assert.AreEqual(loginSessionId, vm.LoginSession.LoginSessionId);
					Assert.AreEqual(user, vm.Model);
				});
		}

		[TestMethod]
		[Tag("user")]
		[Tag("viewmodel")]
		[Tag("userviewmodel")]
		public void LoginFailedTest()
		{
			var companyVm = viewModelFactory.GetViewModel<CompanyViewModel>();
			companyVm.Model = TestGlobals.Company;
			var authenticationGroupVm = viewModelFactory.GetViewModel<AuthenticationGroupViewModel>();
			authenticationGroupVm.Model = TestGlobals.AuthenticationGroup;
			var vm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			vm.CompanyInfo = new TestCompanyInfo();
			var exception = new Exception();
			mockRoomService.Setup(rs => rs.Login(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OperationCallback<LoginSession>>()))
				.Callback((string companyTag, string authenticationGroupTag, string userTag, string password, OperationCallback<LoginSession> callback) =>
					callback(exception, null));
			vm.Login("smithkl42", "password", ex =>
			{
				Assert.AreEqual("smithkl42", vm.UserTag);
				Assert.AreEqual("password", vm.Password);
				Assert.IsNull(vm.LoginSession);
				Assert.AreEqual(exception, ex);
				// mockMessageService.Verify(ms => ms.ShowErrorMessage(It.Is<string>(m => m.Contains("Exception"))), Times.Once());
			});
		}

		[TestMethod]
		[Tag("user")]
		[Tag("viewmodel")]
		[Tag("userviewmodel")]
		public void SetUserIdTest()
		{
			var vm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			Assert.IsNull(vm.UserSharedFileCollectionVm);
			vm.UserId = Guid.NewGuid();
			Assert.IsNotNull(vm.UserSharedFileCollectionVm);
		}

		[TestMethod]
		[Tag("user")]
		[Tag("viewmodel")]
		[Tag("userviewmodel")]
		public void UserChangesAndWorkspaceItemsChange()
		{
			// Arrange
			var vm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			var workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
			var user1 = DesignTimeHelper.GetGuestUser();
			var user2 = DesignTimeHelper.GetRegisteredUser();
			Assert.IsNull(vm.UserSharedFileCollectionVm);
			vm.Model = user1;
			Assert.IsNotNull(vm.UserSharedFileCollectionVm);
			var sharedFileVms = vm.UserSharedFileCollectionVm.ViewModels;
			workspaceVm.WorkspaceItems.RegisterObservedCollection(sharedFileVms);
			foreach (var sharedFile in user1.SharedFiles)
			{
				Assert.IsTrue(vm.UserSharedFileCollectionVm.ViewModels.Any(sfvm => sfvm.Model == sharedFile));
				Assert.IsTrue(workspaceVm.WorkspaceItems.Any(item => (item as SharedFileViewModel).Model == sharedFile));
			}

			// Act
			vm.Model = user2;

			// Assert
			Assert.AreEqual(sharedFileVms, vm.UserSharedFileCollectionVm.ViewModels);
			foreach (var sharedFile in user2.SharedFiles)
			{
				Assert.IsTrue(vm.UserSharedFileCollectionVm.ViewModels.Any(sfvm => sfvm.Model == sharedFile));
				Assert.IsTrue(workspaceVm.WorkspaceItems.Any(item => (item as SharedFileViewModel).Model == sharedFile));
			}
		}

		[TestMethod]
		[Tag("user")]
		[Tag("viewmodel")]
		[Tag("userviewmodel")]
		public void UserChangesAndClosedWorkspaceItemsChange()
		{
			// Arrange
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			var workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
			var user1 = DesignTimeHelper.GetGuestUser();
			var user2 = DesignTimeHelper.GetRegisteredUser();
			var wb1 = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = user2, IsClosed = true };
			var wbVm1 = viewModelFactory.GetViewModel<WhiteboardViewModel>(w => w.WorkspaceItemId == wb1.WhiteboardId);
			wbVm1.Model = wb1;
			var wb2 = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid(), User = user2, IsClosed = true };
			var wbVm2 = viewModelFactory.GetViewModel<WhiteboardViewModel>(w => w.WorkspaceItemId == wb2.WhiteboardId);
			wbVm2.Model = wb2;
			workspaceVm.ClosedWorkspaceItems.Triggers.Add(localUserVm);
			workspaceVm.WorkspaceItems.Add(wbVm1);
			workspaceVm.WorkspaceItems.Add(wbVm2);
			localUserVm.Model = user1;
			Assert.IsTrue(wbVm1.IsReady && !wbVm1.IsShareAvailable && !wbVm1.IsShared);
			Assert.IsTrue(wbVm2.IsReady && !wbVm2.IsShareAvailable && !wbVm2.IsShared);

			// The whiteboards shouldn't initially show up in the closed workspace items because
			// they aren't owned by the current local user.
			Assert.IsFalse(workspaceVm.ClosedWorkspaceItems.Contains(wbVm1));
			Assert.IsFalse(workspaceVm.ClosedWorkspaceItems.Contains(wbVm2));

			// Act
			localUserVm.Model = user2;

			// Assert
			// The whiteboards should now show up in the closed workspace items because
			// they're owned by the new local user.
			Assert.IsTrue(wbVm1.IsReady && wbVm1.IsShareAvailable && !wbVm1.IsShared);
			Assert.IsTrue(wbVm2.IsReady && wbVm2.IsShareAvailable && !wbVm2.IsShared);
			Assert.IsTrue(workspaceVm.ClosedWorkspaceItems.Contains(wbVm1));
			Assert.IsTrue(workspaceVm.ClosedWorkspaceItems.Contains(wbVm2));
		}
	}
}
