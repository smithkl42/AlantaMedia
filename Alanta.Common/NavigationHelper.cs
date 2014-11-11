using System;
using System.Text;

namespace Alanta.Common
{
	public static class NavigationHelper
	{
		public static string GetBookmark(string authenticationGroupTag, string ownerUserTag, string roomName, Guid invitationId)
		{
			var bookmark = new StringBuilder();
			if (!string.IsNullOrEmpty(authenticationGroupTag) && authenticationGroupTag != Constants.DefaultAuthenticationGroupTag)
			{
				bookmark.Append("/" + authenticationGroupTag);
			}
			bookmark.Append("/" + ownerUserTag);
			if (!string.IsNullOrEmpty(roomName) && roomName != Constants.DefaultRoomName)
			{
				bookmark.Append("/" + roomName);
			}
			if (invitationId != Guid.Empty)
			{
				bookmark.Append("/" + Constants.InvitationIdReference + "/" + invitationId);
			}
			return bookmark.ToString();
		}

	}
}
