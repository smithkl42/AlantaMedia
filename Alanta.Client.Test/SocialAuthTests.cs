using System;
using System.Windows.Browser;
using Alanta.Client.Data;
using Alanta.Client.Data.Social;
using Alanta.Client.UI.Desktop.LoginView;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Alanta.Client.Common;

namespace Alanta.Client.Test
{
    //[TestClass]
    public class SocialAuthTests : SilverlightTest
    {
        private static OAuthDomProxy oauthPublisher;

        [TestInitialize]
        public void AuthInitialize()
        {
            if (oauthPublisher == null)
            {
                oauthPublisher = new OAuthDomProxy();
                HtmlPage.RegisterScriptableObject("oauthPublisher", oauthPublisher);
            }
        }

        [TestCleanup]
        public void AuthCleanup()
        {
        }

        [TestMethod]
        [Timeout(30000)]
        [Tag("auth")]
		[Tag("intervention")]
        [Asynchronous]
        public void FacebookLogin()
        {
            bool isCompleted = false;
            FacebookLogin fblogin = new FacebookLogin();
            fblogin.Login((errFB, res) =>
            {
                Assert.IsNull(errFB, "Authentication failed");
                Assert.IsFalse(string.IsNullOrEmpty(res.Idendifier), "User is logged, but FacebookId is null");
                Assert.IsNotNull(fblogin.User, "User is logged, but CurrentUser is null");
                Assert.IsFalse(string.IsNullOrEmpty(fblogin.User.UserName));
                Assert.IsFalse(string.IsNullOrEmpty(fblogin.User.Identifier));
                isCompleted = true;
            });

            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Timeout(30000)]
        [Asynchronous]
        [Tag("auth")]
		[Tag("intervention")]
        public void OpenIdLogin()
        {
            bool isCompleted = false;
            OpenIdLogin openidLogin = new OpenIdLogin();
            openidLogin.Login((err, arg) =>
            {
                Assert.IsNull(err);
                Assert.IsFalse(string.IsNullOrEmpty(arg.Idendifier), "Authentication failed");
                Assert.IsNotNull(openidLogin.OpenIdUser);
                Assert.IsFalse(string.IsNullOrEmpty(openidLogin.OpenIdUser.Identifier));
                isCompleted = true;
            });

            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Timeout(30000)]
        [Asynchronous]
        [Tag("auth")]
		[Tag("intervention")]
        public void OpenIdYahoo()
        {
            bool isCompleted = false;
            OpenIdRequest("yahoo", (args) => { isCompleted = true; });
            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Timeout(30000)]
        [Asynchronous]
        [Tag("auth")]
		[Tag("intervention")]
        public void OpenIdGoogle()
        {
            bool isCompleted = false;
            OpenIdRequest("google", (args) => { isCompleted = true; });
            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Timeout(30000)]
        [Asynchronous]
        [Tag("auth")]
		[Tag("intervention")]
        public void OpenIdMyOpenId()
        {
            bool isCompleted = false;
            OpenIdRequest("MyOpenId", (args) => { isCompleted = true; });
            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Timeout(30000)]
        [Asynchronous]
        [Tag("auth")]
		[Tag("intervention")]
        public void OpenIdAOL()
        {
            bool isCompleted = false;
            OpenIdRequest("AOL", (args) => { isCompleted = true; });
            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Timeout(30000)]
        [Asynchronous]
        [Tag("auth")]
		[Tag("intervention")]
        public void OpenIdLiveJournal()
        {
            bool isCompleted = false;
            OpenIdRequest("LiveJournal", TestConstants.AlantaTest, (args) => { isCompleted = true; });
            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Timeout(30000)]
        [Asynchronous]
        [Tag("auth")]
		[Tag("intervention")]
        public void OpenIdWordpress()
        {
            bool isCompleted = false;
            OpenIdRequest("wordpress", TestConstants.AlantaTest, (args) => { isCompleted = true; });
            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Timeout(30000)]
        [Asynchronous]
        [Tag("auth")]
		[Tag("intervention")]
        public void OpenIdBlogger()
        {
            bool isCompleted = false;
            OpenIdRequest("Blogger", TestConstants.AlantaTest, (args) => { isCompleted = true; });
            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Timeout(30000)]
        [Asynchronous]
        [Tag("auth")]
		[Tag("intervention")]
        public void OpenIdGoogleProfile()
        {
            bool isCompleted = false;
            OpenIdRequest("GoogleProfile", TestConstants.AlantaTest, (args) => { isCompleted = true; });
            EnqueueConditional(() => isCompleted);
            EnqueueTestComplete();
        }

		private static void OpenIdRequest(string provider, Action<OpenIdLoginArgs> callback)
        {
            OpenIdRequest(provider, string.Empty, callback);
        }

		private static void OpenIdRequest(string provider, string username, Action<OpenIdLoginArgs> callback)
        {
            EventHandler<OpenIdLoginArgs> OAuthDomProxy_OpenIdAuthenticated = null;
            OAuthDomProxy_OpenIdAuthenticated = new EventHandler<OpenIdLoginArgs>((obj, args) =>
                {
                    OAuthDomProxy.OpenIdAuthenticated -= OAuthDomProxy_OpenIdAuthenticated;
                    Assert.IsNotNull(args);
                    Assert.IsTrue(string.IsNullOrEmpty(args.Error));
                    Assert.IsNotNull(args.User);
                    Assert.IsFalse(string.IsNullOrEmpty(args.User.Identifier));
                    callback(args);
                });

            OAuthDomProxy.OpenIdAuthenticated += OAuthDomProxy_OpenIdAuthenticated;
			var uriBuilder = new UriBuilder(DataGlobals.BaseUri)
			{
				Path = "OpenId/LoginTestPage.aspx",
				Query = string.Format("testprovider={0}&testusername={1}", provider, username)
			};
            CommonHelper.ShowPopup(uriBuilder.Uri);
        }
    }
}
