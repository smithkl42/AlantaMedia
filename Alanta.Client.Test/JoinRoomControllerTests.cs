using System;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.Navigation;
using Alanta.Common;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	/// <summary>
	///This is a test class for NavigationCommandFactoryTest and is intended
	///to contain all JoinRoomController Unit Tests
	///</summary>
	[TestClass]
	public class JoinRoomControllerTests : DataTestBase
	{
		#region Fields and Properties
		enum TestExecResult
		{
			ExecutionCompleted,
			RenavigationRequired
		}

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext { get; set; }
		#endregion

		#region Test Methods

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerGuest_RoomNull_UserGuest()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.GuestUserTag, TestGlobals.Password, logExc =>
				GetNavigationCommandFactory(TestGlobals.GuestUserTag, string.Empty, TestGlobals.GuestUserTag, _localUserVm.LoginSession, execResult =>
					{
						Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
						Assert.AreNotEqual(Guid.Empty, _roomVm.UserVm.Model.UserId);
						Assert.AreEqual(_roomVm.UserVm.Model.UserId, _localUserVm.UserId, "The Owner and the User should match.");
						Assert.AreEqual(_roomVm.UserVm.Model.UserId, _localUserVm.Model.UserId);
						complete = true;
					}));
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerGuest_RoomNull_UserNull()
		{
			bool complete = false;
			GetNavigationCommandFactory(string.Empty, string.Empty, string.Empty, null, execResult =>
				{
					Assert.AreEqual(TestExecResult.RenavigationRequired, execResult);
					// Assert.AreEqual(_localUserVm.Model.UserId, _roomVm.UserVm.Model.UserId, "The Owner and the User should match.");
					complete = true;
				});
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerGuest_RoomNull_UserRegistered()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.User.UserTag, TestGlobals.Password, logExc =>
				GetNavigationCommandFactory(string.Empty, string.Empty, TestGlobals.User.UserTag, _localUserVm.LoginSession, execResult =>
					{
						Assert.AreEqual(TestExecResult.RenavigationRequired, execResult);
						Assert.AreEqual(TestGlobals.User.UserId, _localUserVm.Model.UserId, "The User and the existing user should match.");
						complete = true;
					}));
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerGuest_RoomSpecified_UserGuest()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.GuestUserTag, string.Empty, loginException =>
				GetNavigationCommandFactory(TestGlobals.GuestRoom.User.UserTag, TestGlobals.GuestRoom.Name, TestGlobals.GuestUserTag, _localUserVm.LoginSession, execResult =>
					{
						Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
						Assert.IsTrue(_roomVm.UserVm.Model.UserId == TestGlobals.GuestRoom.User.UserId, "The Owner and the existing Owner must match.");
						Assert.IsTrue(_roomVm.Model.Name == TestGlobals.GuestRoom.Name, "The room and the existing room must match.");
						Assert.IsTrue(_localUserVm.Model.UserTag == TestGlobals.GuestUserTag, "The user and the existing user should match.");
						complete = true;
					}));
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerGuest_RoomSpecified_UserNull()
		{
			bool complete = false;
			GetNavigationCommandFactory(TestGlobals.GuestRoom.User.UserTag, TestGlobals.GuestRoom.Name, string.Empty, null, execResult =>
				{
					Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
					Assert.IsTrue(_roomVm.UserVm.Model.UserId == TestGlobals.GuestRoom.User.UserId, "The Owner and the existing Owner must match.");
					Assert.IsTrue(_roomVm.Model.Name == TestGlobals.GuestRoom.Name, "The room and the existing room must match.");
					Assert.IsInstanceOfType(_localUserVm.Model, typeof(GuestUser), "The user must be a guest.");
					complete = true;
				});
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerGuest_RoomSpecified_UserRegistered()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.User.UserTag, TestGlobals.Password,
				loginException => GetNavigationCommandFactory(TestGlobals.GuestRoom.User.UserTag, TestGlobals.GuestRoom.Name, TestGlobals.User.UserTag, _localUserVm.LoginSession,
					execResult =>
					{
						Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
						Assert.IsTrue(_roomVm.UserVm.Model.UserId == TestGlobals.GuestRoom.User.UserId, "The Owner and the existing Owner must match.");
						Assert.IsTrue(_roomVm.Model.Name == TestGlobals.GuestRoom.Name, "The room and the existing room must match.");
						Assert.IsTrue(_localUserVm.Model.UserId == TestGlobals.User.UserId, "The user and the existing user must match.");
						complete = true;
					}));
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerNull_RoomNull_UserGuest()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.GuestUserTag, string.Empty, loginException =>
				GetNavigationCommandFactory(string.Empty, string.Empty, TestGlobals.GuestUser.UserTag, _localUserVm.LoginSession, execResult =>
					{
						Assert.AreEqual(TestExecResult.RenavigationRequired, execResult);
						Assert.IsTrue(_localUserVm.Model.UserId == TestGlobals.GuestUser.UserId, "The user and the existing user must match.");
						complete = true;
					}));

			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerNull_RoomNull_UserNull()
		{
			bool complete = false;
			GetNavigationCommandFactory(string.Empty, string.Empty, string.Empty, null, execResult =>
				{
					Assert.AreEqual(TestExecResult.RenavigationRequired, execResult);
					// Assert.IsTrue(_roomVm.UserVm.Model.UserId == _localUserVm.Model.UserId, "The owner and the user must match.");
					complete = true;
				});
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerNull_RoomNull_UserRegistered()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.User.UserTag, TestGlobals.Password,
				logExc => GetNavigationCommandFactory(string.Empty, string.Empty, TestGlobals.UserTag, _localUserVm.LoginSession,
					execResult =>
					{
						Assert.AreEqual(TestExecResult.RenavigationRequired, execResult);
						Assert.IsTrue(_localUserVm.Model.UserTag == TestGlobals.UserTag, "The user and the existing user must match.");
						complete = true;
					}));
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerSpecified_RoomNull_UserGuest()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.GuestUserTag, string.Empty, loginException =>
				GetNavigationCommandFactory(TestGlobals.Owner.UserTag, string.Empty, TestGlobals.GuestUser.UserTag, _localUserVm.LoginSession, execResult =>
					{
						Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
						Assert.IsTrue(_roomVm.UserVm.Model.UserId == TestGlobals.Owner.UserId, "The owner and the existing owner must match.");
						Assert.IsTrue(_roomVm.Model.Name == Constants.DefaultRoomName, "The room name must be '" + Constants.DefaultRoomName + "'.");
						Assert.IsTrue(_localUserVm.Model.UserId == TestGlobals.GuestUser.UserId, "The user and the existing user must match.");
						complete = true;
					}));
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}
		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerSpecified_RoomNull_UserNull()
		{
			bool complete = false;
			GetNavigationCommandFactory(TestGlobals.Owner.UserTag, string.Empty, string.Empty, null, execResult =>
				{
					Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
					Assert.IsTrue(_roomVm.Model.Name == Constants.DefaultRoomName, "The room name must be '" + Constants.DefaultRoomName + "'.");
					Assert.IsTrue(_roomVm.UserVm.Model.UserId == TestGlobals.Owner.UserId, "The owner and the existing owner must match.");
					complete = true;
				});
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}
		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerSpecified_RoomNull_UserRegistered()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.User.UserTag, TestGlobals.Password,
				logExc => GetNavigationCommandFactory(TestGlobals.Owner.UserTag, string.Empty, TestGlobals.User.UserTag, _localUserVm.LoginSession,
					execResult =>
					{
						Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
						Assert.IsTrue(_roomVm.UserVm.Model.UserId == TestGlobals.Owner.UserId, "The owner and the existing owner must match.");
						Assert.IsTrue(_roomVm.Model.Name == Constants.DefaultRoomName, "The room name must be '" + Constants.DefaultRoomName + "'.");
						Assert.IsTrue(_localUserVm.Model.UserId == TestGlobals.User.UserId, "The user and the existing user must match.");
						complete = true;
					}));
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerSpecified_RoomSpecified_UserGuest()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.GuestUserTag, string.Empty, loginException =>
				GetNavigationCommandFactory(TestGlobals.Owner.UserTag, TestGlobals.NamedRoom.Name, TestGlobals.GuestUser.UserTag, _localUserVm.LoginSession, execResult =>
				{
					Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
					Assert.IsTrue(_roomVm.UserVm.Model.UserId == TestGlobals.Owner.UserId, "The Owner and the existing Owner must match.");
					Assert.IsTrue(_roomVm.Model.Name == TestGlobals.NamedRoom.Name, "The room and the existing room must match.");
					Assert.IsTrue(_localUserVm.Model.UserId == TestGlobals.GuestUser.UserId, "The user and the existing user must match.");
					complete = true;
				}));
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerSpecified_RoomSpecified_UserNull()
		{
			bool complete = false;
			GetNavigationCommandFactory(TestGlobals.Owner.UserTag, TestGlobals.NamedRoom.Name, string.Empty, null, execResult =>
				{
					Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
					Assert.IsTrue(_roomVm.UserVm.Model.UserId == TestGlobals.NamedRoom.User.UserId, "The Owner and the existing Owner must match.");
					Assert.IsTrue(_roomVm.Model.Name == TestGlobals.NamedRoom.Name, "The room and the existing room must match.");
					complete = true;
				});
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("joinRoom")]
		public void OwnerSpecified_RoomSpecified_UserRegistered()
		{
			bool complete = false;
			_localUserVm.Login(TestGlobals.User.UserTag, TestGlobals.Password,
				logExc => GetNavigationCommandFactory(TestGlobals.NamedRoom.User.UserTag, TestGlobals.NamedRoom.Name, TestGlobals.User.UserTag, _localUserVm.LoginSession,
					execResult =>
					{
						Assert.AreEqual(TestExecResult.ExecutionCompleted, execResult);
						Assert.IsTrue(_roomVm.UserVm.Model.UserId == TestGlobals.NamedRoom.User.UserId, "The Owner and the existing Owner must match.");
						Assert.IsTrue(_roomVm.Model.Name == TestGlobals.NamedRoom.Name, "The room and the existing room must match.");
						Assert.IsTrue(_localUserVm.Model.UserTag == TestGlobals.UserTag, "The user and the existing user must match.");
						complete = true;
					}));
			EnqueueConditional(() => complete);
			EnqueueCallback(() => Assert.IsTrue(complete));
			EnqueueTestComplete();
		}

		#endregion

		#region Helper Methods
		private void GetNavigationCommandFactory(string ownerUserTag, string roomName, string userTag, LoginSession loginSession, Action<TestExecResult> callback)
		{
			_localUserVm.UserTag = userTag;
			_localUserVm.LoginSession = loginSession;
			_roomVm.UserTag = ownerUserTag;
			_roomVm.RoomName = roomName;
			var navCommandFactory = new JoinRoomController(_roomService, _viewModelFactory);

			Action<string, string> navigationRequiredCallback = (cbOwnerUserTag, cbRoomName) => Try(() =>
			{
				Assert.IsFalse(string.IsNullOrEmpty(cbOwnerUserTag), "The Owner must not be null.");
				Assert.IsFalse(string.IsNullOrEmpty(cbRoomName), "The Room must not be null.");

				if (callback != null)
				{
					callback(TestExecResult.RenavigationRequired);
				}
			});

			OperationCallback joinRoomCallback = ex => Try(() =>
			{
					Assert.IsNull(ex, "The JoinRoomController threw an exception: " + ex);
					Assert.IsNotNull(_roomVm.UserVm.Model, "The Owner must not be null.");
					Assert.IsNotNull(_roomVm.Model, "The Room must not be null.");
					Assert.IsNotNull(_localUserVm.Model, "The User must not be null.");
					Assert.AreEqual(_roomVm.UserTag, _roomVm.UserVm.Model.UserTag);
					Assert.AreEqual(_roomVm.RoomName, _roomVm.Model.Name);
					Assert.AreEqual(_roomVm.UserVm.Model.UserId, _roomVm.Model.User.UserId);
					Assert.AreEqual(_localUserVm.UserId, _localUserVm.Model.UserId);
					if (callback != null)
					{
						callback(TestExecResult.ExecutionCompleted);
					}
			});

			navCommandFactory.Execute(navigationRequiredCallback, joinRoomCallback);
		}

		#endregion
	}
}
