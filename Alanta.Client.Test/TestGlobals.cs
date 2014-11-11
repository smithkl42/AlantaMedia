using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Common;

namespace Alanta.Client.Test
{
	public class TestGlobals
	{
		private static string testPrefix = "SLTEST.";
		public static string CompanyTag;
		public static string AuthenticationGroupTag;
		public static string UserTag;
		public static string UserName;
		public static string OwnerUserTag;
		public static string OwnerUserName;
		public static string RoomName;
		public static string Password;
		public static string GuestUserTag;
		public static string GuestUserName;
		public static string GuestOwnerUserId;
		public static string GuestOwnerUserName;
		public static string GuestRoomName;
		public static bool Initialized;
		public static IViewModelFactory ViewModelFactory { get; set; }
		// public static TestController TestController { get; set; }
		public static Company Company { get; set; }
		public static AuthenticationGroup AuthenticationGroup { get; set; }
		public static Room DefaultRoom { get; set; }
		public static Room NamedRoom { get; set; }
		public static Room GuestRoom { get; set; }
		public static RegisteredUser User { get; set; }
		public static GuestUser GuestUser { get; set; }
		public static RegisteredUser Owner { get; set; }
		public static LoginSession LoginSession { get; set; }
		public static GuestUser GuestOwner { get; set; }
		public static string InitialPage { get; set; }

		static TestGlobals()
		{
			CompanyTag = "Alanta";
			AuthenticationGroupTag = Constants.DefaultAuthenticationGroupTag;
			UserTag = testPrefix + "SomeUserId";
			UserName = testPrefix + "Some UserName";
			OwnerUserTag = testPrefix + "someowneruserid";
			OwnerUserName = testPrefix + "Some OwnerUserName";
			RoomName = testPrefix + "SomeRoomName";
			Password = testPrefix + "somepassword";
			GuestUserTag = testPrefix + "SomeUserId2";
			GuestUserName = testPrefix + "Some GuestUserName";
			GuestOwnerUserId = testPrefix + "someowneruserid2";
			GuestOwnerUserName = testPrefix + "Some GuestOwnerUserName";
			GuestRoomName = Constants.DefaultRoomName;
		}

	}
}
