using System;
using System.Windows;
using System.Windows.Threading;
using Alanta.Client.Common.Logging;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	public class DataTestBase : SilverlightTest
	{
		protected string _testPrefix = "SLTEST.";
		protected string _userTag;
		protected string _userName;
		protected string _ownerUserTag;
		protected string _ownerUserName;
		protected string _roomName;
		protected string _password;
		protected string _userId2;
		protected string _userName2;
		protected string _guestOwnerUserTag;
		protected string _guestOwnerUserName;
		protected string _guestOwnerRoomName;
		protected const int Timeout = 10000;
		protected TestController _testController;
		protected CompanyViewModel _companyVm;
		protected AuthenticationGroupViewModel _authenticationGroupVm;
		protected LocalUserViewModel _localUserVm;
		protected RoomViewModel _roomVm;
		protected DataPublisher _dataPublisher;
		protected ContactData _contactData;
		protected IRoomServiceAdapter _roomService;
		protected IViewModelFactory _viewModelFactory;

		public DataTestBase()
		{
			_userTag = TestGlobals.UserTag;
			_userName = TestGlobals.UserName;
			_ownerUserTag = TestGlobals.OwnerUserTag;
			_ownerUserName = TestGlobals.OwnerUserName;
			_roomName = TestGlobals.RoomName;
			_password = TestGlobals.Password;
			_userId2 = TestGlobals.GuestUserTag;
			_userName2 = TestGlobals.GuestUserName;
			_guestOwnerUserTag = TestGlobals.GuestOwnerUserId;
			_guestOwnerUserName = TestGlobals.GuestOwnerUserName;
			_guestOwnerRoomName = TestGlobals.GuestRoomName;
		}

		/// <summary>
		/// Overloaded EnqueueConditional that supports timeouts.
		/// </summary>
		/// <param name="timeout">The length of time to wait.</param>
		/// <param name="conditionalDelegate">The func that is supposed return true before control returns.</param>
		public void EnqueueConditional(TimeSpan timeout, Func<bool> conditionalDelegate)
		{
			var timer = new DispatcherTimer();
			timer.Interval = timeout;
			timer.Tick += (sender, args) =>
			{
				// remember to stop timer or it'll tick again
				timer.Stop();
				throw new TimeoutException();
			};
			EnqueueCallback(timer.Start);
			base.EnqueueConditional(conditionalDelegate);
			EnqueueCallback(timer.Stop);
		}

		[TestInitialize]
		[Asynchronous]
		public void TestInitialize()
		{
			InterceptUnhandledExceptions = true;
			bool initializingCompleted = false;
			ClientLogger.Debug(" -- Beginning test initialize.");
			EnqueueConditional(() => TestGlobals.Initialized);
			EnqueueCallback(() =>
			{
				_roomService = new RoomServiceAdapter();
				var messageService = new TestMessageService();
				var viewLocator = new ViewLocator();
				_viewModelFactory = new ViewModelFactory(_roomService, messageService, viewLocator);
				_testController = new TestController(TestGlobals.UserTag, Guid.NewGuid().ToString(), _viewModelFactory, new TestCompanyInfo());
				_contactData = _testController.ContactData;
				_companyVm = _viewModelFactory.GetViewModel<CompanyViewModel>();
				_companyVm.Model = TestGlobals.Company;
				_authenticationGroupVm = _viewModelFactory.GetViewModel<AuthenticationGroupViewModel>();
				_authenticationGroupVm.Model = TestGlobals.AuthenticationGroup;
				_roomVm = _viewModelFactory.GetViewModel<RoomViewModel>();
				_localUserVm = _viewModelFactory.GetViewModel<LocalUserViewModel>();
				_roomService.CreateClient();

				TestInitializing(() => initializingCompleted = true);
			});
			EnqueueConditional(() => initializingCompleted);
			EnqueueTestComplete();
		}

		public virtual void TestInitializing(Action callback)
		{
			callback();
		}

		public virtual void TestCleaning(Action callback)
		{
			callback();
		}

		[TestCleanup]
		[Asynchronous]
		public virtual void TestCleanup()
		{
			ClientLogger.Debug(" -- Beginning test cleanup.");
			bool cleanupFinished = false;
			TestCleaning(() => _roomService.LeaveRoom(_roomVm.SessionId, leaveRoomError => _roomService.CloseClientAsync(clientCloseError => cleanupFinished = true)));
			EnqueueConditional(() => cleanupFinished);
			EnqueueTestComplete();
		}

		protected RegisteredUser GetRegisteredUser()
		{
			return GetRegisteredUser(_userTag, _userName, _password);
		}

		protected RegisteredUser GetRegisteredUser(string userTag, string userName, string password)
		{
			var user = new RegisteredUser
			{
				UserId = Guid.NewGuid(),
				UserTag = userTag,
				UserName = userName,
				PlainTextPassword = password,
				FirstName = "first",
				LastName = "last"
			};
			return user;
		}

		#region Helper Methods

		public void CreateUser(string userId, string userName, string password, OperationCallback<User> callback)
		{
			var user = GetRegisteredUser(userId, userName, password);
			_roomService.CreateUser(user, callback);
		}

		protected void ExecuteTestFramework(OperationCallback callback)
		{
			_roomVm.SessionId = Guid.NewGuid();
			JoinRoom(callback);
		}

		public void UpdateRepository(Room room, User user)
		{
			_roomVm.UserTag = room.User.UserTag;
			_roomVm.RoomName = room.Name;
			_roomVm.ApplicationState = ApplicationState.Initializing;
			_localUserVm.Model = user;
			_localUserVm.UserId = user.UserId;
		}

		protected void JoinRoom(OperationCallback callback)
		{
			UpdateRepository(TestGlobals.NamedRoom, TestGlobals.User);
			_localUserVm.Login(_userTag, _password, loginError =>
			{
				Assert.IsNull(loginError);
				_roomVm.SessionId = Guid.NewGuid();
				_roomVm.JoinRoom(joinError =>
					{
						if (callback != null)
						{
							callback(joinError);
						}
					});
			});
		}

		public static void Try(Action action)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					if (ex is AssertFailedException)
					{
						throw;
					}
					Assert.Fail("Unhandled testing exception: " + ex);
				}
			}
);
		}
		#endregion
	}
}
