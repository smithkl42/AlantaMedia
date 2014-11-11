using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Common.Testing
{
	[TestClass]
	public class RoomHelperTests
	{
		#region GetRelativeRoomPath()
		[TestMethod]
		public void GetRelativeRoomPathTest_AllDefaults()
		{
			var expected = "/ken";
			var ub = new UriBuilder();
			ub.AddRoomPath(Constants.DefaultCompanyTag, Constants.DefaultAuthenticationGroupTag, "ken", Constants.DefaultRoomName);
			Assert.AreEqual(expected, ub.Uri.PathAndQuery);
		}

		[TestMethod]
		public void GetRelativeRoomPathTest_SpecifiedCompany()
		{
			var companyTag = Guid.NewGuid().ToString();
			var expected = "/ken?companyTag=" + companyTag;
			var ub = new UriBuilder();
			ub.AddRoomPath(companyTag, Constants.DefaultAuthenticationGroupTag, "ken", Constants.DefaultRoomName);
			Assert.AreEqual(expected, ub.Uri.PathAndQuery);
		}

		[TestMethod]
		public void GetRelativeRoomPathTest_SpecifiedAuthenticationGroup()
		{
			var authenticationGroupTag = Guid.NewGuid().ToString();
			var expected = "/ken?authenticationGroupTag=" + authenticationGroupTag;
			var ub = new UriBuilder();
			ub.AddRoomPath(Constants.DefaultCompanyTag, authenticationGroupTag, "ken", Constants.DefaultRoomName);
			Assert.AreEqual(expected, ub.Uri.PathAndQuery);
		}

		[TestMethod]
		public void GetRelativeRoomPathTest_SpecifiedRoom()
		{
			var roomName = Guid.NewGuid().ToString();
			var expected = "/ken/" + roomName;
			var ub = new UriBuilder();
			ub.AddRoomPath(Constants.DefaultCompanyTag, Constants.DefaultAuthenticationGroupTag, "ken", roomName);
			Assert.AreEqual(expected, ub.Uri.PathAndQuery);
		}

		[TestMethod]
		public void GetRelativeRoomPathTest_AllSpecified()
		{
			var companyTag = Guid.NewGuid().ToString();
			var authenticationGroupTag = Guid.NewGuid().ToString();
			var roomName = Guid.NewGuid().ToString();
			var ub = new UriBuilder();
			var expected = "/ken/" + roomName + "?authenticationGroupTag=" + authenticationGroupTag + "&companyTag=" + companyTag;
			ub.AddRoomPath(companyTag, authenticationGroupTag, "ken", roomName);
			Assert.AreEqual(expected, ub.Uri.PathAndQuery);
		}
		#endregion

		#region GetRelativeRoutingGroupPath()
		[TestMethod]
		public void GetRelativeRoutingGroupPathTest_AllDefaults()
		{
			var routingGroupTag = Guid.NewGuid().ToString();
			var expected = "/routing/" + routingGroupTag;
			var ub = new UriBuilder();
			ub.AddRoutingGroupPath(Constants.DefaultCompanyTag, Constants.DefaultAuthenticationGroupTag, routingGroupTag);
			Assert.AreEqual(expected, ub.Uri.PathAndQuery);
		}

		[TestMethod]
		public void GetRelativeRoutingGroupPathTest_SpecifiedCompany()
		{
			var routingGroupTag = Guid.NewGuid().ToString();
			var companyTag = Guid.NewGuid().ToString();
			var expected = "/routing/" + routingGroupTag + "?companyTag=" + companyTag;
			var ub = new UriBuilder();
			ub.AddRoutingGroupPath(companyTag, Constants.DefaultAuthenticationGroupTag, routingGroupTag);
			Assert.AreEqual(expected, ub.Uri.PathAndQuery);
		}

		[TestMethod]
		public void GetRelativeRoutingGroupPathTest_SpecifiedAuthenticationGroup()
		{
			var routingGroupTag = Guid.NewGuid().ToString();
			var authenticationGroupTag = Guid.NewGuid().ToString();
			var expected = "/routing/" + routingGroupTag + "?authenticationGroupTag=" + authenticationGroupTag;
			var ub = new UriBuilder();
			ub.AddRoutingGroupPath(Constants.DefaultCompanyTag, authenticationGroupTag, routingGroupTag);
			Assert.AreEqual(expected, ub.Uri.PathAndQuery);
		}

		[TestMethod]
		public void GetRelativeRoutingGroupPathTest_AllSpecified()
		{
			var routingGroupTag = Guid.NewGuid().ToString();
			var companyTag = Guid.NewGuid().ToString();
			var authenticationGroupTag = Guid.NewGuid().ToString();
			var expected = "/routing/" + routingGroupTag + "?authenticationGroupTag=" + authenticationGroupTag + "&companyTag=" + companyTag;
			var ub = new UriBuilder();
			ub.AddRoutingGroupPath(companyTag, authenticationGroupTag, routingGroupTag);
			Assert.AreEqual(expected, ub.Uri.PathAndQuery);
		}
		#endregion

		#region Others
		[TestMethod]
		public void GetAbsoluteRoomUrlFromBaseUrlTest()
		{
			var baseUrl = new Uri("http://dev.alanta.com");
			var companyTag = Guid.NewGuid().ToString();
			var authenticationGroupTag = Guid.NewGuid().ToString();
			var userTag = Guid.NewGuid().ToString();
			var roomName = Guid.NewGuid().ToString();
			var expected = string.Format("{0}{1}/{2}?authenticationGroupTag={3}&companyTag={4}", baseUrl, userTag, roomName, authenticationGroupTag, companyTag);
			var actual = RoomHelpers.GetAbsoluteRoomUrlFromBaseUrl(baseUrl, companyTag, authenticationGroupTag, userTag, roomName);
			Assert.AreEqual(expected, actual.ToString());
		}

		[TestMethod]
		public void GetAbsoluteRoomUrlFromHostTest()
		{
			var host = "dev.alanta.com";
			var companyTag = Guid.NewGuid().ToString();
			var authenticationGroupTag = Guid.NewGuid().ToString();
			var userTag = Guid.NewGuid().ToString();
			var roomName = Guid.NewGuid().ToString();
			var expected = string.Format("http://{0}/{1}/{2}?authenticationGroupTag={3}&companyTag={4}", host, userTag, roomName, authenticationGroupTag, companyTag);
			var actual = RoomHelpers.GetAbsoluteRoomUrlFromHost(host, companyTag, authenticationGroupTag, userTag, roomName);
			Assert.AreEqual(expected, actual.ToString());
		}

		[TestMethod]
		public void GetAbsoluteRoutingGroupUrlFromBaseUrlTest()
		{
			var baseUrl = new Uri("http://dev.alanta.com");
			var companyTag = Guid.NewGuid().ToString();
			var authenticationGroupTag = Guid.NewGuid().ToString();
			var routingGroupTag = Guid.NewGuid().ToString();
			var expected = string.Format("{0}routing/{1}?authenticationGroupTag={2}&companyTag={3}", baseUrl, routingGroupTag, authenticationGroupTag, companyTag);
			var actual = RoomHelpers.GetAbsoluteRoutingGroupUrlFromBaseUrl(baseUrl, companyTag, authenticationGroupTag, routingGroupTag);
			Assert.AreEqual(expected, actual.ToString());
		}

		[TestMethod]
		public void GetAbsoluteRoutingGroupUrlFromHostTest()
		{
			var host = "dev.alanta.com";
			var companyTag = Guid.NewGuid().ToString();
			var authenticationGroupTag = Guid.NewGuid().ToString();
			var routingGroupTag = Guid.NewGuid().ToString();
			var expected = string.Format("http://{0}/routing/{1}?authenticationGroupTag={2}&companyTag={3}", host, routingGroupTag, authenticationGroupTag, companyTag);
			var actual = RoomHelpers.GetAbsoluteRoutingGroupUrlFromHost(host, companyTag, authenticationGroupTag, routingGroupTag);
			Assert.AreEqual(expected, actual.ToString());
		}
		#endregion

	}
}
