using System;
using System.Linq;
using System.Windows;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.ModerationView;
using Alanta.Client.UI.Common.SampleData;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class ModerationRequestViewModelTests : ViewModelTestBase
	{
		/// <summary>
		/// When there are no pending moderation requests, ModerationRequestVisibility should be set to Collapsed.
		/// </summary>
		[TestMethod]
		[Tag("moderationrequest")]
		[Tag("viewmodel")]
		public void ModerationRequestsVisibilityTest()
		{
			// Arrange
			var roomVm = new SampleRoomViewModel();
			var requestsVm = roomVm.ModerationRequestCollectionVm;
			Assert.IsTrue(requestsVm.ViewModelCount > 0);
			Assert.IsTrue(requestsVm.PendingModerationRequests.Count > 0);
			Assert.AreEqual(Visibility.Visible, requestsVm.ModerationRequestsVisibility);

			// Act
			foreach (var requestvm in requestsVm.PendingModerationRequests.ToList())
			{
				requestvm.Model.RespondedOn = DateTime.Now;
			}

			// Assert
			Assert.AreEqual(0, requestsVm.PendingModerationRequests.Count);
			Assert.AreEqual(Visibility.Collapsed, requestsVm.ModerationRequestsVisibility);
		}

		/// <summary>
		/// When a moderation request has been responded to, it should be removed from the PendingModerationRequests collection.
		/// </summary>
		[TestMethod]
		[Tag("moderationrequest")]
		[Tag("viewmodel")]
		public void PendingModerationRequestsTest()
		{
			var roomVm = new SampleRoomViewModel();
			var requestsVm = roomVm.ModerationRequestCollectionVm;
			Assert.IsTrue(requestsVm.ViewModelCount > 0);
			Assert.AreEqual(requestsVm.ViewModelCount, requestsVm.PendingModerationRequests.Count);
			var requestVm = requestsVm.ViewModels.First();
			Assert.IsFalse(requestVm.Model.RespondedOn.HasValue);
			requestVm.Model.RespondedOn = DateTime.Now;
			Assert.AreEqual(requestsVm.ViewModelCount - 1, requestsVm.PendingModerationRequests.Count);
			requestVm.Model.RespondedOn = null;
			Assert.AreEqual(requestsVm.ViewModelCount, requestsVm.PendingModerationRequests.Count);
		}

		[TestMethod]
		[Tag("moderationrequest")]
		[Tag("viewmodel")]
		public void ModerationViewModel_Initialize()
		{
			var companyVm = viewModelFactory.GetViewModel<CompanyViewModel>();
			var agVm = viewModelFactory.GetViewModel<AuthenticationGroupViewModel>();
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			localUserVm.CompanyInfo = new TestCompanyInfo();
			localUserVm.LoginSession = new LoginSession
			{
				LoginSessionId = Guid.NewGuid().ToString(),
				InvitationId = Guid.NewGuid(),
				UserId = user.UserId
			};
			bool getAuthenticatedUserCalled = false;
			bool getRoomIdCalled = false;
			mockRoomService.Setup(rs => rs.GetAuthenticatedUser(It.IsAny<LoginSession>(), It.IsAny<OperationCallback<User>>()))
				.Callback((LoginSession loginSession, OperationCallback<User> callback) =>
				{
					getAuthenticatedUserCalled = true;
					callback(null, user);
				});
			mockRoomService.Setup(rs => rs.GetRoomId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OperationCallback<ShortRoomInfo>>()))
				.Callback((string companyTag, string authenticationGroupTag, string userTag, string roomTag, OperationCallback<ShortRoomInfo> callback) =>
				{
					getRoomIdCalled = true;
					var roomInfo = new ShortRoomInfo { RoomId = room.RoomId };
					callback(null, roomInfo);
				});
			var roomUnavailSetting = new RoomUnavailableSetting();
			roomUnavailSetting.Initialize();
			var moderationVm = viewModelFactory.GetViewModel<ModerationViewModel>();
			moderationVm.RoomUnavailableSetting = roomUnavailSetting;
			Assert.IsFalse(getAuthenticatedUserCalled);
			Assert.IsFalse(getRoomIdCalled);
			companyVm.Model = TestGlobals.Company;
			Assert.IsFalse(getAuthenticatedUserCalled);
			Assert.IsFalse(getRoomIdCalled);
			agVm.Model = TestGlobals.AuthenticationGroup;
			Assert.IsTrue(getAuthenticatedUserCalled);
			Assert.IsTrue(getRoomIdCalled);
			Assert.AreEqual(ModerationState.Start, moderationVm.ModerationState);
			Assert.AreEqual(Visibility.Visible, moderationVm.SendRequestVisibility);
		}

		[TestMethod]
		[Tag("moderationrequest")]
		[Tag("viewmodel")]
		public void ModerationViewModel_ModerationState_Start()
		{
			var roomUnavailSetting = new RoomUnavailableSetting();
			roomUnavailSetting.Initialize();
			var moderationVm = viewModelFactory.GetViewModel<ModerationViewModel>();
			moderationVm.RoomUnavailableSetting = roomUnavailSetting;
			Assert.AreEqual(ModerationState.Start, moderationVm.ModerationState);
			Assert.AreEqual(Visibility.Visible, moderationVm.SendRequestVisibility);
			Assert.AreEqual(Visibility.Collapsed, moderationVm.RequestDeniedVisibility);
			Assert.AreEqual(Visibility.Collapsed, moderationVm.WaitingForResponseVisibility);
		}

		[TestMethod]
		[Tag("moderationrequest")]
		[Tag("viewmodel")]
		public void ModerationViewModel_ModerationState_Sent()
		{
			var roomUnavailSetting = new RoomUnavailableSetting();
			roomUnavailSetting.Initialize();
			var moderationVm = viewModelFactory.GetViewModel<ModerationViewModel>();
			moderationVm.RoomUnavailableSetting = roomUnavailSetting;
			moderationVm.ModerationState = ModerationState.Sent;
			Assert.AreEqual(Visibility.Collapsed, moderationVm.SendRequestVisibility);
			Assert.AreEqual(Visibility.Collapsed, moderationVm.RequestDeniedVisibility);
			Assert.AreEqual(Visibility.Visible, moderationVm.WaitingForResponseVisibility);
		}

		[TestMethod]
		[Tag("moderationrequest")]
		[Tag("viewmodel")]
		public void ModerationViewModel_ModerationState_Denied()
		{
			var roomUnavailSetting = new RoomUnavailableSetting();
			roomUnavailSetting.Initialize();
			var moderationVm = viewModelFactory.GetViewModel<ModerationViewModel>();
			moderationVm.RoomUnavailableSetting = roomUnavailSetting;
			moderationVm.ModerationState = ModerationState.Denied;
			Assert.AreEqual(Visibility.Collapsed, moderationVm.SendRequestVisibility);
			Assert.AreEqual(Visibility.Visible, moderationVm.RequestDeniedVisibility);
			Assert.AreEqual(Visibility.Collapsed, moderationVm.WaitingForResponseVisibility);
		}

	}
}
