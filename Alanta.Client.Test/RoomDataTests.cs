using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using Alanta.Client.Common;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class RoomDataTests : DataTestBase
	{

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		public void LoginTest()
		{
			bool loginCompleted = false;
			UpdateRepository(TestGlobals.NamedRoom, TestGlobals.User);
			_roomService.Login(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, TestGlobals.UserTag, TestGlobals.Password, (error2, loginArgs) =>
			{
				Assert.IsNull(error2);
				Assert.IsFalse(string.IsNullOrEmpty(loginArgs.LoginSessionId));
				Assert.AreNotEqual(Guid.Empty, loginArgs.UserId);
				loginCompleted = true;
			});
			EnqueueConditional(() => loginCompleted);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		public void LeaveRoomTest()
		{
			bool userLeftRoom = false;
			ExecuteTestFramework(e =>
			{
				Assert.IsNull(e);
				_roomService.LeaveRoom(_roomVm.SessionId, leaveRoomError =>
					{
						Assert.IsNull(leaveRoomError);
						userLeftRoom = true;
					});
			});
			EnqueueConditional(() => userLeftRoom);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		public void JoinRoomTest()
		{
			bool userJoinedRoom = false;

			UpdateRepository(TestGlobals.NamedRoom, TestGlobals.User);
			_roomVm.SessionId = Guid.NewGuid();
			var session = new Session
			{
				SessionId = _roomVm.SessionId,
				SsrcId = _roomVm.SsrcId,
				DocumentUri = "http://alanta.com"
			};
			_roomService.Login(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, TestGlobals.User.UserTag, TestGlobals.Password, (loginException, loginSession) => 
				_roomService.JoinRoom(loginSession, TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, _ownerUserTag, _roomName, session, true,
			        (joinRoomException, room2) =>
			        {
						Assert.IsNull(joinRoomException);
						Assert.AreEqual(TestGlobals.NamedRoom.Name, room2.Name);
						userJoinedRoom = true;
			        }));
			EnqueueConditional(() => userJoinedRoom);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		public void CheckAuthenticationTest()
		{
			bool authenticationChecked = false;
			_localUserVm.UserTag = TestGlobals.UserTag;
			_localUserVm.Model = TestGlobals.User;
			_roomService.Login(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, _ownerUserTag, _password, (error2, loginSession) =>
				{
					_localUserVm.UserId = loginSession.UserId;
					_localUserVm.LoginSession = loginSession;
					Assert.IsNull(error2);
					_roomService.GetAuthenticatedUser(_localUserVm.LoginSession, (error3, user3) =>
						{
							Assert.IsNull(error3);
							Assert.IsNotNull(user3);
							authenticationChecked = true;
						});
				});
			EnqueueConditional(() => authenticationChecked);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		public void RoomUpdateReceivedTest()
		{
			bool roomUpdateReceived = false;
			ExecuteTestFramework(error =>
			{
				var newRoom = new Room
				{
					User = _roomVm.UserVm.Model,
					UserId = _roomVm.UserVm.Model.UserId,
					Name = _roomVm.RoomName,
					Sessions = new ObservableCollection<Session>(),
					SharedFiles = new ObservableCollection<SharedFile>()
				};

				_roomVm.PropertyChanged += (s, e) =>
					{
						if (e.PropertyName == _roomVm.GetPropertyName(x => x.Model))
						{
							Assert.AreEqual(newRoom, _roomVm.Model);
							Assert.AreEqual(newRoom.User.UserId, _roomVm.UserVm.Model.UserId);
							Assert.AreEqual(newRoom.Name, _roomVm.RoomName);
							roomUpdateReceived = true;
						}
					};

				_roomVm.Model = newRoom;
			});
			EnqueueConditional(() => roomUpdateReceived); //  && roomChangedReceived);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		public void SessionAddedReceivedTest()
		{
			bool sessionAddedReceived = false;
			ExecuteTestFramework(error =>
				{
					var sessionCollectionViewModel = _viewModelFactory.GetViewModel<SessionCollectionViewModel>();
					var user = new User
					{
						UserId = Guid.NewGuid(),
						UserTag = TestConstants.Facebook1UserId,
						UserName = TestConstants.Facebook1UserName
					};
					var session = new Session
					{
						SessionId = Guid.NewGuid(),
						SsrcId = (short)(new Random()).Next(short.MinValue, short.MaxValue),
						User = user
					};
					sessionCollectionViewModel.ViewModels.CollectionChanged += (s, e) =>
					{
						var vm = (SessionViewModel)e.NewItems[0];
						Assert.AreEqual(session, vm.Model);
						sessionAddedReceived = true;
					};
					_roomVm.Model.Sessions.Add(session);
				});
			EnqueueConditional(() => sessionAddedReceived);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		public void SessionRemovedReceivedTest()
		{
			bool sessionRemovedReceived = false;
			ExecuteTestFramework(error =>
			{
				var sessionCollectionViewModel = _viewModelFactory.GetViewModel<SessionCollectionViewModel>();
				var user = new User
				{
					UserId = Guid.NewGuid(),
					UserTag = TestConstants.Facebook1UserId,
					UserName = TestConstants.Facebook1UserName
				};
				var session = new Session
				{
					SessionId = Guid.NewGuid(),
					SsrcId = (short)(new Random()).Next(short.MinValue, short.MaxValue),
					User = user
				};
				sessionCollectionViewModel.ViewModels.CollectionChanged += (s, e) =>
				{
					if (e.Action == NotifyCollectionChangedAction.Add)
					{
						var vm = (SessionViewModel)e.NewItems[0] ;
						Assert.AreEqual(session, vm.Model);
						Deployment.Current.Dispatcher.BeginInvoke(() => _roomVm.Model.Sessions.Remove(vm.Model));
					}
					else if (e.Action == NotifyCollectionChangedAction.Remove)
					{
						var vm = (SessionViewModel)e.OldItems[0];
						Assert.AreEqual(session, vm.Model);
						sessionRemovedReceived = true;
					}
				};
				_roomVm.Model.Sessions.Add(session);
			});
			EnqueueConditional(() => sessionRemovedReceived);
			EnqueueTestComplete();
		}
	}
}
