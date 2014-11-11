using System;
using Alanta.Client.Common.Loader;
using Alanta.Client.UI.Common.RoomView;

namespace Alanta.Client.Test
{
	public class TestRoomInfo : TestCompanyInfo, IRoomInfo
	{

		public string OwnerUserTag
		{
			get { return TestGlobals.OwnerUserTag; }
		}

		public string RoomName
		{
			get { return TestGlobals.RoomName; }
		}

		public string UserTag
		{
			get { return TestGlobals.UserTag; }
		}

		public Guid InvitationId
		{
			get { return Guid.NewGuid(); }
		}
	}
}
