using System;
using System.Linq;
using Alanta.Client.Common;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.Classes;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class CookieHelperTests : SilverlightTest
	{
		private const string cookie1 = "dev.alanta.com=authenticationGroupTag=_default&userTag=ken&loginSessionId=ad7a05ab-14df-47f8-8b62-ad2afd04395d; ";
		private const string cookie2 = "alanta.com=my%20other%20login&my%20other%20key=my%20other%20value;";
		private const string cookie3 = "extraneous%20cookie%20value; ";
		private const string cookie4 = "fbsetting_fad69f85a16bd1acaabe7f6b7b68ebe4=%7B%22connectState%22%3A2%2C%22oneLineStorySetting%22%3A3%2C%22shortStorySetting%22%3A3%2C%22inFacebook%22%3Afalse%7D; ";
		private const string cookie5 = "dev.alanta.com=authenticationGroupTag=acme&userTag=PuppetryVizsla&loginSessionId=12f48194-e130-481d-852c-38ebb67939b4";
		private const string cookies = cookie1 + cookie2 + cookie3 + cookie4 + cookie5;

		[TestMethod]
		[Tag("cookies")]
		public void ParseCookiesTest()
		{
			var ch = new CookieHelper(cookies);
			Assert.AreEqual(10, ch.CookieValues.Count);
			Assert.AreEqual("ken", ch.GetValue("dev.alanta.com", "userTag"));
			Assert.IsTrue(ch.CookieValues.Any(cv => cv.Key == "my other login"));
			Assert.IsTrue(string.IsNullOrEmpty(ch.GetValue("alanta.com", "my other login")));
			Assert.AreEqual("my other value", ch.GetValue("alanta.com", "my other key"));
			Assert.AreEqual("ad7a05ab-14df-47f8-8b62-ad2afd04395d", ch.GetValue("dev.alanta.com", "loginSessionId"));
			var loginSessionIds = ch.GetAllValues("dev.alanta.com", "loginSessionId");
			Assert.AreEqual(2, loginSessionIds.Count());
			Assert.IsTrue(loginSessionIds.Contains("ad7a05ab-14df-47f8-8b62-ad2afd04395d"));
			Assert.IsTrue(loginSessionIds.Contains("12f48194-e130-481d-852c-38ebb67939b4"));
			var excv = ch.CookieValues.FirstOrDefault(cv => cv.CookieId == "fbsetting_fad69f85a16bd1acaabe7f6b7b68ebe4");
			Assert.AreEqual("{\"connectState\":2,\"oneLineStorySetting\":3,\"shortStorySetting\":3,\"inFacebook\":false}", excv.Key);
			Assert.IsNotNull(ch.CookieValues.FirstOrDefault(cv => cv.CookieId == "extraneous cookie value"));
		}

		[TestMethod]
		[Tag("cookies")]
		public void SetValueAndReparseTest()
		{
			var ch = new CookieHelper(cookies);
			string newUserId = "Bob the Builder";
			ch.SetValue("login", "userid", newUserId);
			Assert.AreEqual(newUserId, ch.GetValue("login", "userid"));
			string updatedCookies = ch.GetAllCookiesAsString();
			var ch2 = new CookieHelper(updatedCookies);
			Assert.AreEqual(newUserId, ch2.GetValue("login", "userid"));
		}

		[TestMethod]
		[Tag("loginconfig")]
		public void LoginConfigTest()
		{
			string domain = Guid.NewGuid().ToString();
			string authenticationGroupTag = Guid.NewGuid().ToString();
			string userTag = Guid.NewGuid().ToString();
			var loginSession = new LoginSession() { LoginSessionId = Guid.NewGuid().ToString() };
			LoginConfig.SaveLoginConfig(domain, authenticationGroupTag, userTag, loginSession);
			var loginConfig = LoginConfig.GetLoginConfig(domain, authenticationGroupTag);
			Assert.AreEqual(userTag, loginConfig.UserTag);
			Assert.AreEqual(loginSession.LoginSessionId, loginConfig.LoginSession.LoginSessionId);
			LoginConfig.SaveLoginConfig(domain, authenticationGroupTag, string.Empty, null);
			loginConfig = LoginConfig.GetLoginConfig(domain, authenticationGroupTag);
			Assert.AreEqual(string.Empty, loginConfig.UserTag);
			Assert.AreEqual(null, loginConfig.LoginSession);
		}

	}
}
