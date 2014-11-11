using System;
using System.Collections.ObjectModel;
using System.Linq;
using Alanta.Client.Common;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.SampleData;
using Alanta.Client.UI.Common.ViewModels;
using Alanta.Model;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class RoomPermissionViewModelTests : ViewModelTestBase
	{
		[TestMethod]
		[Tag("roompermission")]
		[Tag("viewmodel")]
		public void RoomPermissionCollectionViewModel_LoadTest()
		{
			// Arrange
			var expectedPermissions = DesignTimeHelper.GetRoomPermissions();
			mockRoomService.Setup(rs => rs.GetRoomPermissions(It.IsAny<OperationCallback<ObservableCollection<RoomPermission>>>()))
				.Callback((OperationCallback<ObservableCollection<RoomPermission>> callback) => callback(null, expectedPermissions));
			var roomPermissionsVm = viewModelFactory.GetViewModel<RoomPermissionCollectionViewModel>();
			bool isBusyChanged = false;
			roomPermissionsVm.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == roomPermissionsVm.GetPropertyName(x => x.IsBusy))
				{
					isBusyChanged = true;
				}
			};

			// Act
			roomPermissionsVm.Load();

			// Assert
			Assert.AreEqual(expectedPermissions.Count, roomPermissionsVm.ViewModelCount);
			Assert.AreEqual(expectedPermissions.Count, roomPermissionsVm.ViewModels.Count);
			foreach (var expectedPermission in expectedPermissions)
			{
				var rp = expectedPermission;
				Assert.IsTrue(roomPermissionsVm.ViewModels.Any(vm => vm.Model.RoomPermissionTag.Equals(rp.RoomPermissionTag, StringComparison.OrdinalIgnoreCase)));
			}
			Assert.IsFalse(roomPermissionsVm.IsBusy);
			Assert.IsTrue(roomPermissionsVm.IsLoaded);
			Assert.IsTrue(isBusyChanged);
		}

		[TestMethod]
		[Tag("roompermission")]
		[Tag("viewmodel")]
		public void RoomPermissionGrantCollectionViewModel_LoadTest()
		{
			// Arrange
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			var expectedGrants = new ObservableCollection<RoomPermissionGrant>();
			expectedGrants.Add(new RoomPermissionGrant { RoomPermissionGrantId = Guid.NewGuid(), RoomPermissionTag = Guid.NewGuid().ToString(), UserId = user.UserId, User = user, RoomId = room.RoomId });
			expectedGrants.Add(new RoomPermissionGrant { RoomPermissionGrantId = Guid.NewGuid(), RoomPermissionTag = Guid.NewGuid().ToString(), UserId = user.UserId, User = user, RoomId = room.RoomId });
			mockRoomService.Setup(rs => rs.GetRoomPermissionGrants(It.IsAny<Guid>(), It.IsAny<OperationCallback<ObservableCollection<RoomPermissionGrant>>>()))
				.Callback((Guid roomId, OperationCallback<ObservableCollection<RoomPermissionGrant>> callback) => callback(null, expectedGrants));
			var actualGrants = viewModelFactory.GetViewModel<RoomPermissionGrantCollectionViewModel>();
			bool isBusyChanged = false;
			actualGrants.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == actualGrants.GetPropertyName(x => x.IsBusy))
				{
					isBusyChanged = true;
				}
			};

			// Act
			actualGrants.Load();

			// Assert
			Assert.AreEqual(expectedGrants.Count, actualGrants.ViewModelCount);
			Assert.AreEqual(expectedGrants.Count, actualGrants.ViewModels.Count);
			foreach (var expectedGrant in expectedGrants)
			{
				var grant = expectedGrant;
				Assert.IsTrue(actualGrants.ViewModels.Any(vm =>
					vm.Model.RoomPermissionGrantId == grant.RoomPermissionGrantId &&
					vm.Model.RoomPermissionTag.Equals(grant.RoomPermissionTag, StringComparison.OrdinalIgnoreCase) &&
					vm.Model.UserId == grant.UserId &&
					vm.Model.RoomId == grant.RoomId));
			}
			Assert.IsFalse(actualGrants.IsBusy);
			Assert.IsTrue(actualGrants.IsLoaded);
			Assert.IsTrue(isBusyChanged);
		}

		/// <summary>
		/// When the RoomService raises the RoomPermissionsUpdated event, the list of permissions granted to the local user should be updated.
		/// </summary>
		[TestMethod]
		[Tag("roompermission")]
		[Tag("viewmodel")]
		public void RoomPermissionsUpdatedTest()
		{
			// Arrange
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			var originalGrants = DesignTimeHelper.GetRoomPermissionGrants();
			var goodGrant1 = new RoomPermissionGrant { RoomPermissionGrantId = Guid.NewGuid(), RoomPermissionTag = Guid.NewGuid().ToString(), UserId = user.UserId, User = user, RoomId = room.RoomId };
			var goodGrant2 = new RoomPermissionGrant { RoomPermissionGrantId = Guid.NewGuid(), RoomPermissionTag = Guid.NewGuid().ToString(), UserId = user.UserId, User = user, RoomId = room.RoomId };
			var badGrant1 = new RoomPermissionGrant { RoomPermissionGrantId = Guid.NewGuid(), RoomPermissionTag = Guid.NewGuid().ToString(), UserId = Guid.NewGuid(), User = null, RoomId = room.RoomId };
			var badGrant2 = new RoomPermissionGrant { RoomPermissionGrantId = Guid.NewGuid(), RoomPermissionTag = Guid.NewGuid().ToString(), UserId = user.UserId, User = user, RoomId = Guid.NewGuid() };
			var newGrants = new ObservableCollection<RoomPermissionGrant> { goodGrant1, goodGrant2, badGrant1, badGrant2 };
			var localPerms = viewModelFactory.GetViewModel<SecurityPrincipalRoomPermissionGrantCollectionViewModel>();
			localPerms.SecurityPrincipalId = user.UserId;
			localPerms.Models = originalGrants;

			// Act
			mockRoomService.Raise(rs => rs.RoomPermissionsForUserUpdated += null, new EventArgs<ObservableCollection<RoomPermissionGrant>>(newGrants));

			// Assert
			Assert.AreEqual(2, localPerms.ViewModelCount);
			Assert.AreEqual(2, localPerms.ViewModels.Count);
			Assert.IsTrue(localPerms.ViewModels.Any(vm => vm.Model.RoomPermissionGrantId == goodGrant1.RoomPermissionGrantId));
			Assert.IsTrue(localPerms.ViewModels.Any(vm => vm.Model.RoomPermissionGrantId == goodGrant2.RoomPermissionGrantId));
		}

		[TestMethod]
		[Tag("roompermission")]
		[Tag("viewmodel")]
		public void SampleRoomViewModelTest()
		{
			var roomVm = new SampleRoomViewModel();
			Assert.IsNotNull(roomVm.RoomPermissionGrantCollectionVm);
			Assert.IsTrue(roomVm.RoomPermissionGrantCollectionVm.ViewModels.Count > 0);
			Assert.IsTrue(roomVm.RoomPermissionGrantCollectionVm.SecurityPrincipals.Count > 0);
			var securityUser = ((UserViewModel)(roomVm.RoomPermissionGrantCollectionVm.SecurityPrincipals[0])).Model;
			Assert.AreEqual(user.UserName, securityUser.UserName);
			Assert.IsTrue(roomVm.RoomPermissionGrantCollectionVm.ViewModels.Any(vm => vm.Model.RoomPermissionTag == RoomPermissionValues.Administer));
			Assert.AreEqual(2, roomVm.ModerationRequestCollectionVm.ViewModelCount);
			Assert.AreEqual(2, roomVm.ModerationRequestCollectionVm.PendingModerationRequests.Count);
		}


		/// <summary>
		/// When the list of permissions granted to a user is set, the associated (flattened) list of RoomPermissions should be updated.
		/// </summary>
		[TestMethod]
		[Tag("roompermission")]
		[Tag("viewmodel")]
		public void UserRoomPermissionGrantCollectionTest()
		{
			// Arrange
			var roomPermissionAdminister = new RoomPermission { RoomPermissionTag = "Administer", Name = "Administer", IsAvailable = true };
			var roomPermissionChat = new RoomPermission { RoomPermissionTag = "Chat", Name = "Chat", IsAvailable = true };
			var roomPermissionTest = new RoomPermission { RoomPermissionTag = "Test", Name = "Test", IsAvailable = true };
			var roomPermissions = new ObservableCollection<RoomPermission> { roomPermissionAdminister, roomPermissionChat, roomPermissionTest };
			mockRoomService.Setup(rs => rs.GetRoomPermissions(It.IsAny<OperationCallback<ObservableCollection<RoomPermission>>>()))
				.Callback((OperationCallback<ObservableCollection<RoomPermission>> callback) => callback(null, roomPermissions));
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			var grantsVm = viewModelFactory.GetViewModel<SecurityPrincipalRoomPermissionGrantCollectionViewModel>();
			grantsVm.RoomPermissionScopeTag = RoomPermissionScopeValues.User;
			grantsVm.SecurityPrincipalId = user.UserId;

			// Act - Assigning the Models property should build out the entire hierarchy for this particular collection of room-user-grants
			var grants = new ObservableCollection<RoomPermissionGrant>();
			grants.Add(new RoomPermissionGrant
			{
				UserId = user.UserId,
				RoomId = room.RoomId,
				RoomPermissionTag = roomPermissionAdminister.RoomPermissionTag,
				RoomPermissionScopeTag = RoomPermissionScopeValues.User
			});
			grantsVm.Models = grants;

			// Assert
			Assert.AreEqual(roomPermissions.Count, grantsVm.PossibleGrants.Count);
			ConfirmGrantSynchronization(grantsVm);

			// Act/Assert
			grantsVm.PossibleGrants.First(pg => pg.Model.RoomPermissionTag == roomPermissionChat.RoomPermissionTag).IsSelected = true;
			ConfirmGrantSynchronization(grantsVm);

			// Act/Assert
			grantsVm.PossibleGrants.First(pg => pg.Model.RoomPermissionTag == roomPermissionChat.RoomPermissionTag).IsSelected = false;
			ConfirmGrantSynchronization(grantsVm);

		}

		/// <summary>
		/// When we save the users' granted permissions, we should re-flatten all the permissions and submit them to the room service.
		/// </summary>
		[TestMethod]
		[Tag("roompermission")]
		[Tag("viewmodel")]
		public void RoomPermissionGrantCollectionSavePermissionsTest()
		{
			// Setup the mock room service
			mockRoomService.Setup(rs => rs.GetRoomPermissions(It.IsAny<OperationCallback<ObservableCollection<RoomPermission>>>()))
				.Callback((OperationCallback<ObservableCollection<RoomPermission>> callback) => callback(null, DesignTimeHelper.GetRoomPermissions()));

			// Setup the room and various users.
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			localUserVm.Model = user;
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			var user1 = user;
			var user1Vm = viewModelFactory.GetViewModel<UserViewModel>(u => u.UserId == user1.UserId);
			user1Vm.Model = user1;
			var user2 = new RegisteredUser
			{
				AuthenticationGroupId = user1.AuthenticationGroupId,
				UserId = Guid.NewGuid(),
				UserTag = Guid.NewGuid().ToString(),
				UserName = "User Two"
			};
			var user2Vm = viewModelFactory.GetViewModel<UserViewModel>(u => u.UserId == user2.UserId);
			user2Vm.Model = user2;
			var user3 = new RegisteredUser
			{
				AuthenticationGroupId = user1.AuthenticationGroupId,
				UserId = Guid.NewGuid(),
				UserTag = Guid.NewGuid().ToString(),
				UserName = "User Three"
			};
			var user3Vm = viewModelFactory.GetViewModel<UserViewModel>(u => u.UserId == user3.UserId);
			user3Vm.Model = user3;

			// Load the users into the grants container
			var grantsVm = viewModelFactory.GetViewModel<RoomPermissionGrantCollectionViewModel>();
			grantsVm.SecurityPrincipals.Add(user1Vm);
			grantsVm.SecurityPrincipals.Add(user2Vm);
			grantsVm.SecurityPrincipals.Add(user3Vm);

			// Establish their permissions.
			user1Vm.RoomPermissionGrants.Models = DesignTimeHelper.GetRoomPermissionGrants(user1).Where(g => g.UserId == user1.UserId).ToObservableCollection();
			user2Vm.RoomPermissionGrants.Models = DesignTimeHelper.GetRoomPermissionGrants(user2).Where(g => g.UserId == user2.UserId).ToObservableCollection();
			user3Vm.RoomPermissionGrants.Models = DesignTimeHelper.GetRoomPermissionGrants(user3).Where(g => g.UserId == user3.UserId).ToObservableCollection();

			// Confirm stuff is looking good so far and update grants
			// When this is done, each Join and Chat should be selected, and Administer and Share should not be.
			foreach (var userVm in grantsVm.SecurityPrincipals)
			{
				ConfirmGrantSynchronization(userVm.RoomPermissionGrants);
				userVm.RoomPermissionGrants.PossibleGrants.First(g => g.Model.RoomPermissionTag == "Administer").IsSelected = false;
				userVm.RoomPermissionGrants.PossibleGrants.First(g => g.Model.RoomPermissionTag == "Chat").IsSelected = true;
				ConfirmGrantSynchronization(userVm.RoomPermissionGrants);
			}

			// Setup the mock room service so we can handle validations.
			mockRoomService.Setup(rs => rs.UpdateRoomPermissions(It.IsAny<LoginSession>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<ObservableCollection<RoomPermissionGrant>>(), It.IsAny<OperationCallback>()))
				.Callback((LoginSession loginSession, Guid roomId, bool isPublic, ObservableCollection<RoomPermissionGrant> grants, OperationCallback callback) =>
				{
					Assert.AreEqual(6, grants.Count);
					Assert.AreEqual(0, grants.Count(g => g.RoomPermissionTag == "Administer"));
					Assert.AreEqual(0, grants.Count(g => g.RoomPermissionTag == "Share"));
					Assert.AreEqual(3, grants.Count(g => g.RoomPermissionTag == "Join"));
					Assert.AreEqual(3, grants.Count(g => g.RoomPermissionTag == "Chat"));
					callback(null);
				});

			// Save it and trigger the validation
			grantsVm.Save();
			mockRoomService.VerifyAll();
		}

		/// <summary>
		/// When we approve a moderation request from a registered user, we should create a new ISecurityPrincipal of the User type, and add the appropriate room permission grants to it.
		/// </summary>
		[TestMethod]
		[Tag("roompermission")]
		[Tag("viewmodel")]
		public void ApproveModerationTest_User()
		{
			// Arrange
			var requests = viewModelFactory.GetViewModel<ModerationRequestCollectionViewModel>();
			requests.Models = DesignTimeHelper.GetModerationRequests(user);
			var grantsVm = viewModelFactory.GetViewModel<RoomPermissionGrantCollectionViewModel>();
			var requestVm = requests.ViewModels[0];
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			mockRoomService.Setup(rs => rs.UpdateRoomPermissionsForUser(It.IsAny<LoginSession>(), It.IsAny<Guid>(), It.IsAny<ObservableCollection<RoomPermissionGrant>>(), It.IsAny<OperationCallback>()))
				.Callback((LoginSession loginSession, Guid moderationRequestId, ObservableCollection<RoomPermissionGrant> grants, OperationCallback callback) =>
				{
					Assert.AreEqual(2, grants.Count);
					Assert.AreEqual(1, grants.Count(g => g.RoomPermissionTag == "Share"));
					Assert.AreEqual(1, grants.Count(g => g.RoomPermissionTag == "Join"));
					callback(null);
				});

			// Affirm
			Assert.IsTrue(requestVm.Model.ModerationRequestedRoomPermissions.Count > 0);
			Assert.IsTrue(requestVm.Model.ModerationRequestedRoomPermissions.All(p => !string.IsNullOrEmpty(p.RoomPermissionTag)));

			// Act
			grantsVm.ApproveModerationRequest(requestVm);

			// Assert
			mockRoomService.VerifyAll();
			Assert.IsTrue(grantsVm.SecurityPrincipals.Count(p => p.SecurityPrincipalId == requestVm.Model.UserId) == 1, "The generated grant should have the same SecurityPrincipalId as the request's UserId");
			var uservm = (UserViewModel)grantsVm.SecurityPrincipals.First(p => p.SecurityPrincipalId == requestVm.Model.UserId);
			Assert.AreEqual(requestVm.Model.ModerationRequestedRoomPermissions.Count, uservm.RoomPermissionGrants.ViewModelCount);
			foreach (var grantvm in uservm.RoomPermissionGrants.ViewModels)
			{
				string roomPermissionTag = grantvm.Model.RoomPermissionTag;
				Assert.AreEqual(RoomPermissionScopeValues.User, grantvm.Model.RoomPermissionScopeTag);
				Assert.AreEqual(1, requestVm.Model.ModerationRequestedRoomPermissions.Count(p => p.RoomPermissionTag == roomPermissionTag));
			}
		}

		/// <summary>
		/// When we approve a moderation request from a guest user, we should create a new ISecurityPrincipal of the User type, and add the appropriate room permission grants to it.
		/// </summary>
		[TestMethod]
		[Tag("roompermission")]
		[Tag("viewmodel")]
		public void ApproveModerationTest_Invitation()
		{
			// Arrange dependencies
			var requests = viewModelFactory.GetViewModel<ModerationRequestCollectionViewModel>();
			requests.Models = DesignTimeHelper.GetModerationRequests(DesignTimeHelper.GetGuestUser());
			var grantsVm = viewModelFactory.GetViewModel<RoomPermissionGrantCollectionViewModel>();
			var requestVm = requests.ViewModels[0];
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			localUserVm.Model = user;

			// Configure mock RoomServiceAdapter.
			mockRoomService.Setup(rs => rs.CreateInvitation(It.IsAny<LoginSession>(), It.IsAny<Guid>(), It.IsAny<Invitation>(), It.IsAny<OperationCallback>()))
				.Callback((LoginSession loginSessionId, Guid roomId, Invitation invitation, OperationCallback callback) =>
				{
					Assert.IsNotNull(invitation);
					Assert.AreEqual(user.UserId, invitation.UserId);
					Assert.AreEqual(room.RoomId, invitation.RoomId);
					callback(null);
				});
			mockRoomService.Setup(rs => rs.UpdateRoomPermissionsForInvitation(It.IsAny<LoginSession>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ObservableCollection<RoomPermissionGrant>>(), It.IsAny<OperationCallback>()))
				.Callback((LoginSession loginSessionId, Guid moderationRequestId, Guid securityPrincipalId, ObservableCollection<RoomPermissionGrant> grants, OperationCallback callback) =>
				{
					Assert.AreEqual(2, grants.Count);
					Assert.AreEqual(1, grants.Count(g => g.RoomPermissionTag == "Share"));
					Assert.AreEqual(1, grants.Count(g => g.RoomPermissionTag == "Join"));
					callback(null);
				});

			// Affirm
			Assert.IsTrue(requestVm.Model.ModerationRequestedRoomPermissions.Count > 0);
			Assert.IsTrue(requestVm.Model.ModerationRequestedRoomPermissions.All(p => !string.IsNullOrEmpty(p.RoomPermissionTag)));

			// Act
			grantsVm.ApproveModerationRequest(requestVm);

			// Assert
			mockRoomService.VerifyAll();
			Assert.AreEqual(1, grantsVm.SecurityPrincipals.OfType<InvitationViewModel>().Count());
			var invitationVm = grantsVm.SecurityPrincipals.OfType<InvitationViewModel>().First();
			Assert.AreEqual(requestVm.Model.ModerationRequestedRoomPermissions.Count, invitationVm.RoomPermissionGrants.ViewModelCount);
			foreach (var grantvm in invitationVm.RoomPermissionGrants.ViewModels)
			{
				string roomPermissionTag = grantvm.Model.RoomPermissionTag;
				Assert.AreEqual(RoomPermissionScopeValues.Invitation, grantvm.Model.RoomPermissionScopeTag);
				Assert.AreEqual(1, requestVm.Model.ModerationRequestedRoomPermissions.Count(p => p.RoomPermissionTag == roomPermissionTag));
			}

		}


		#region Helper Methods
		private void ConfirmGrantSynchronization(SecurityPrincipalRoomPermissionGrantCollectionViewModel grants)
		{
			var roomPermissions = viewModelFactory.GetViewModel<RoomPermissionCollectionViewModel>();

			// (1) First, make sure that the counts are right.
			Assert.AreEqual(grants.ViewModelCount, grants.PossibleGrants.Count(g => g.IsSelected));

			// (2) Then make sure that the right items are present and with the right properties.
			// (2a) Each actual grant should be marked as IsSelected in the PossibleGrants collection.
			foreach (var grantVm in grants.ViewModels)
			{
				string roomPermissionTag = grantVm.Model.RoomPermissionTag;
				Assert.IsTrue(grants.PossibleGrants.First(g => g.Model.RoomPermissionTag == roomPermissionTag).IsSelected);
			}

			// (2b) And each grant that's selected in the PossibleGrants collection should be present in the ViewModels
			foreach (var grantVm in grants.PossibleGrants)
			{
				string roomPermissionTag = grantVm.Model.RoomPermissionTag;
				Assert.AreEqual(grantVm.IsSelected, grants.ViewModels.Any(g => g.Model.RoomPermissionTag == roomPermissionTag));
				Assert.AreEqual(roomPermissions.ViewModels.First(rp => rp.Model.RoomPermissionTag == roomPermissionTag).Model.Name, grantVm.Name);
				Assert.AreEqual(roomPermissions.ViewModels.First(rp => rp.Model.RoomPermissionTag == roomPermissionTag).Model.Description, grantVm.Description);
			}
		}
		#endregion
	}
}
