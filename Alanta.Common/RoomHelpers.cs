using System;
using System.Text;

#if SILVERLIGHT
using System.Windows.Browser;
#else
using System.Web;
#endif

namespace Alanta.Common
{
	public static class RoomHelpers
	{
		private const StringComparison comp = StringComparison.OrdinalIgnoreCase;

		public static void AddRoomPath(this UriBuilder ub, string companyTag, string authenticationGroupTag, string ownerUserTag, string roomName = null, string invitationId = null)
		{
			var sb = new StringBuilder();
			sb.Append(ownerUserTag);
			if (!string.IsNullOrEmpty(roomName) && !Constants.DefaultRoomName.Equals(roomName, comp))
			{
				sb.Append("/" + HttpUtility.UrlEncode(roomName));
			}
			ub.Path = sb.ToString();
			AddQueryString(ub, companyTag, authenticationGroupTag, invitationId);
		}

		public static void AddRoutingGroupPath(this UriBuilder ub, string companyTag, string authenticationGroupTag, string routingGroupTag)
		{
			ub.Path = Constants.RoutingReference + "/" + HttpUtility.UrlEncode(routingGroupTag);
			AddQueryString(ub, companyTag, authenticationGroupTag);
		}

		public static Uri GetAbsoluteRoomUrlFromHost(string host, string companyTag, string authenticationGroupTag, string ownerUserTag, string roomName)
		{
			var ub = new UriBuilder(Constants.DefaultScheme, host);
			return GetAbsoluteRoomUrlFromBaseUrl(ub.Uri, companyTag, authenticationGroupTag, ownerUserTag, roomName);
		}

		public static Uri GetAbsoluteRoomUrlFromBaseUrl(Uri baseUri, string companyTag, string authenticationGroupTag, string ownerUserTag, string roomName)
		{
			var ub = new UriBuilder(baseUri);
			AddRoomPath(ub, companyTag, authenticationGroupTag, ownerUserTag, roomName);
			return ub.Uri;
		}

		public static Uri GetAbsoluteRoutingGroupUrlFromHost(string host, string companyTag, string authenticationGroupTag, string routingGroupTag)
		{
			var ub = new UriBuilder(Constants.DefaultScheme, host, 80);
			AddRoutingGroupPath(ub, companyTag, authenticationGroupTag, routingGroupTag);
			return ub.Uri;
		}

		public static Uri GetAbsoluteRoutingGroupUrlFromBaseUrl(Uri baseUri, string companyTag, string authenticationGroupTag, string routingGroupTag)
		{
			var ub = new UriBuilder(baseUri);
			AddRoutingGroupPath(ub, companyTag, authenticationGroupTag, routingGroupTag);
			return ub.Uri;
		}

		private static void AddQueryString(UriBuilder ub, string companyTag, string authenticationGroupTag, string invitationId = null)
		{
			var path = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(authenticationGroupTag) && !Constants.DefaultAuthenticationGroupTag.Equals(authenticationGroupTag, comp))
			{
				path.Append(Constants.AuthenticationGroupTagReference + "=" + HttpUtility.UrlEncode(authenticationGroupTag) + "&");
			}
			if (!string.IsNullOrWhiteSpace(companyTag) && !Constants.DefaultCompanyTag.Equals(companyTag, comp))
			{
				path.Append(Constants.CompanyTagReference + "=" + HttpUtility.UrlEncode(companyTag) + "&");
			}
			if (!string.IsNullOrWhiteSpace(invitationId))
			{
				path.Append(Constants.InvitationIdReference + "=" + HttpUtility.UrlEncode(invitationId));
			}
			ub.Query = path.ToString().TrimEnd('&');
		}

#if SILVERLIGHT
		public static string PathAndQuery(this Uri uri)
		{
			return uri.LocalPath + "?" + uri.Query;
		}
#endif
	}
}
