using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Alanta.Client.Common.Logging;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[SuppressMessage(
	"Microsoft.Performance",
	"CA1812:AvoidUninstantiatedInternalClasses"), TestClass]
	public class AssemblyInit : SilverlightTest
	{
		[AssemblyInitialize]
		public static void AssemblyInitalize()
		{
			if (!TestGlobals.Initialized)
			{
				DataGlobals.BaseServiceUri = new Uri("http://localhost:51151/");

				//var messageService = new TestMessageService();
				//var viewLocator = new ViewLocator();
				var roomService = new RoomServiceAdapter();
				roomService.CreateClient();
				//TestGlobals.ViewModelFactory = new ViewModelFactory(roomService, messageService, viewLocator);
				//TestGlobals.TestController = new TestController(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, TestGlobals.UserTag, Guid.NewGuid().ToString(), TestGlobals.ViewModelFactory);

				DeleteTestObjects(roomService, () => roomService.GetCompany(TestGlobals.CompanyTag, (e, c) =>
				{
					TestGlobals.Company = c;
					TestGlobals.AuthenticationGroup = c.AuthenticationGroups.Single(ag => ag.AuthenticationGroupTag == TestGlobals.AuthenticationGroupTag);

					// Create user
					var user = GetRegisteredUser(TestGlobals.UserTag, TestGlobals.UserName, TestGlobals.Password);
					roomService.CreateUser(user, (error, u) =>
					{
						TestGlobals.User = u as RegisteredUser;
						CheckInitialization(roomService);
					});

					// Create user2
					roomService.CreateGuestUser(TestGlobals.AuthenticationGroup.AuthenticationGroupId, (error, u) =>
					{
						TestGlobals.GuestUser = u;
						TestGlobals.GuestUserTag = u.UserTag;
						TestGlobals.GuestUserName = u.UserName;
						CheckInitialization(roomService);
					});

					// Create owner user and room.
					var ownerUser = GetRegisteredUser(TestGlobals.OwnerUserTag, TestGlobals.OwnerUserName, TestGlobals.Password);
					roomService.CreateUser(ownerUser, (error, u) =>
						roomService.Login(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, TestGlobals.OwnerUserTag, TestGlobals.Password, (ex, loginSession) =>
						{
							TestGlobals.Owner = u as RegisteredUser;
							roomService.CreatedNamedRoom(u.UserId, TestGlobals.RoomName, (createException, room) =>
							{
								TestGlobals.NamedRoom = room;
								room.IsPublic = true;
								roomService.UpdateRoom(loginSession, room, updateException =>
								{
									TestGlobals.LoginSession = loginSession;
									CheckInitialization(roomService);
								});
							});
							roomService.CreateDefaultRoom(u.UserId, (createException, room) =>
							{
								TestGlobals.DefaultRoom = room;
								room.IsPublic = true;
								roomService.UpdateRoom(loginSession, room, updateException =>
								{
									TestGlobals.LoginSession = loginSession;
									CheckInitialization(roomService);
								});
							});
						}));

					// Create owner2 user and room.
					roomService.CreateGuestUser(TestGlobals.AuthenticationGroup.AuthenticationGroupId, (error, u) => roomService.CreateDefaultRoom(u.UserId, (error1, room) =>
					{
						TestGlobals.GuestOwner = u;
						TestGlobals.GuestRoom = room;
						CheckInitialization(roomService);
					}));
				}));

			}
		}

		private static void CheckInitialization(IRoomServiceAdapter roomService)
		{
			if (TestGlobals.AuthenticationGroup != null &&
				TestGlobals.Company != null &&
				TestGlobals.User != null &&
				TestGlobals.GuestUser != null &&
				TestGlobals.NamedRoom != null &&
				TestGlobals.DefaultRoom != null &&
				TestGlobals.GuestRoom != null &&
				TestGlobals.LoginSession != null)
			{
				roomService.CloseClientAsync(e =>
				{
					TestGlobals.Initialized = true;
				});
			}
		}

		[AssemblyCleanup]
		public static void AssemblyCleanup()
		{
			// TestGlobals.TestController = new TestController(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, TestGlobals.UserTag, Guid.NewGuid().ToString(), TestGlobals.ViewModelFactory);
			var roomService = new RoomServiceAdapter();
			roomService.CreateClient();
			DeleteTestObjects(roomService, () => roomService.CloseClientAsync(e => ClientLogger.Debug(" -- Finished test cleanup.")));
		}

		protected static RegisteredUser GetRegisteredUser(string userTag, string userName, string password)
		{
			var user = new RegisteredUser
			{
				UserId = Guid.NewGuid(),
				AuthenticationGroupId = TestGlobals.AuthenticationGroup.AuthenticationGroupId,
				UserTag = userTag,
				UserName = userName,
				PlainTextPassword = password,
				FirstName = "first",
				LastName = "last"
			};
			return user;
		}

		protected static void DeleteTestObjects(IRoomServiceAdapter roomServiceAdapter, Action callback)
		{
			int expectedCalls = 4;
			int actualCalls = 0;
			OperationCallback handler = error =>
			{
				actualCalls++;
				if (actualCalls >= expectedCalls)
				{
					callback();
				}
			};
			roomServiceAdapter.DeleteUserByTag(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, TestGlobals.UserTag, handler);
			roomServiceAdapter.DeleteUserByTag(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, TestGlobals.GuestUserTag, handler);
			roomServiceAdapter.DeleteUserByTag(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, TestGlobals.OwnerUserTag, handler);
			roomServiceAdapter.DeleteUserByTag(TestGlobals.CompanyTag, TestGlobals.AuthenticationGroupTag, TestGlobals.GuestOwnerUserId, handler);
		}


	}
}
