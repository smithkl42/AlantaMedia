using System;

namespace Alanta.Common
{
	/// <summary>
	/// The Constants class is shared between the client (Silverlight) and server-side (ASP.NET) applications.  
	/// It's used to make sure there's one place where key values are stored that need to be shared between
	/// the Silverlight client and the web services that support it.
	/// </summary>
	public static class Constants
	{
		#region References

		public const string AlantaCompanyTag = "alanta";
		public const string AdvertiserTagReference = "advertiserTag";
		public const string ApplicationName = "Alanta";
		public const string AuthenticationGroupTagReference = "authenticationGroupTag";
		public const string SpeechEnhancementStackReference = "AECCancelEcho";
		public const string CampaignNameReference = "utm_campaign";
		public const string CampaignContentReference = "utm_content";
		public const string CampaignSourceReference = "utm_source";
		public const string CampaignTermReference = "utm_term";
		public const string CampaignMediumReference = "utm_medium";
		public const string SampleGroupReference = "sampleGroup";
		public const string ClientAddressReference = "ipaddress";
		public const string ClosePageReference = "closePage";
		public const string CompanyDomainReference = "companyDomain";
		public const string CompanyTagReference = "companyTag";
		public const string LandingPageTagReference = "landingPageTag";
		public const string DomainReference = "domain";
		public const string ExpectedAudioLatencyReference = "AECExpectedAudioLatency";
		public const string ExpectedAudioLatencyFallbackReference = "AECExpectedAudioLatencyFallback";
		public const string FacebookApiKeyReference = "facebookApiKey";
		public const string FacebookAppIdReference = "facebookAppId";
		public const string FacebookAppSecretReference = "facebookAppSecret";
		public const string FacebookEmailPermCallback = "facebookEmailPermCallback";
		public const string FacebookInvitationsCallback = "facebookInvitationsCallback";
		public const string FacebookLoginCallbackReference = "facebookLoginCallback";
		public const string FacebookSendInvitations = "facebookSendInvitations";
		public const string FileNameReference = "fileName";
		public const string FilterLengthReference = "AECFilterLength";
		public const string FilterLengthFallbackReference = "AECFilterLengthFallback";
		public const string FirstReference = "first";
		public const string GmailApiKeyReference = "gmailApiKey";
		public const string GmailSecretKeyReference = "gmailSecretKey";
		public const string GmailUriAuthenticateReference = "gmailUriAuth";
		public const string GmailUriAuthSuccessReference = "gmailUriAuthSuccess";
		public const string HostReference = "HostReference";
		public const string InitialPageReference = "InitialPage";
		public const string InvitationIdReference = "invitationId";
		public const string IsShowHintReference = "isshowhint";
		public const string AppVisualStateReference = "appVisualState";
		public const string LastReference = "last";
		public const string LinkedInApiKeyReference = "linkedinApiKey";
		public const string LinkedInAuthLinkReference = "linkedInAuthLinkReference";
		public const string LinkedInSecretKeyReference = "linkedinSecretKey";
		public const string LoginConfigSuffix = ".login";
		public const string LoginReference = "login";
		public const string LoginSessionIdReference = "loginSessionId";
		public const string MediaServerControlPortReference = "mediaServerControlPort";
		public const string MediaServerHostReference = "mediaServerHost";
		public const string MediaServerStreamingPortReference = "mediaServerStreamingPort";
		public const string OffsetReference = "offset";
		public const string OpenIdLoginCallbackReference = "openidLoginCallback";
		public const string OpenIdLoginReference = "openidLogin";
		public const string OwnerUserIdReference = "ownerUserId";
		public const string OwnerUserTagReference = "ownerUserTag";
		public const string ParamReference = "param";
		public const string ResyncAudioReference = "AECResyncAudio";
		public const string RoomCookieReference = "room";
		public const string RoomIdReference = "roomId";
		public const string RoomNameReference = "roomName";
		public const string RoutingReference = "routing";
		public const string RoutingGroupTagReference = "routingGroupTag";
		public const string ServiceHostMappingReference = "serviceHostMapping";
		public const string SharedDesktopHostReference = "sharedDesktopHost";
		public const string SharedDesktopServerPortReference = "sharedDesktopServerPort";
		public const string SharedDesktopViewerPortReference = "sharedDesktopViewerPort";
		public const string SharedFileIdReference = "sharedFileId";
		public const string Tag1Reference = "tag1";
		public const string Tag2Reference = "tag2";
		public const string Tag3Reference = "tag3";
		public const string Tag4Reference = "tag4";
		public const string TwitterUriAuthSuccessReference = "twitterUriAuthSuccess";
		public const string UserEncryptedPasswordReference = "userEncryptedPassword";
		public const string UserIdReference = "userId";
		public const string UserTagReference = "userTag";
		public const string UserTypeReference = "userType";
		public const string WriteDebugOutputToWebPageReference = "writeDebugOutputToWebPage";
		public const string MessageReference = "messageRef";
		public const string RoomSessionIdReference = "roomSessionIdRef";
		public const string IsNavigationEmbeddedReference = "isNavEmbedded";
		public const string AppIdReference = "appId";

		public const int DefaultMediaServerControlPort = 4521;
		public const int DefaultMediaServerStreamingPort = 4522;

		#endregion

		#region Other

		public const string DefaultRoomName = "_default";
		public const string DefaultAuthenticationGroupTag = "_default";
		public const string DefaultCompanyTag = "Alanta";
		public const string DefaultCompanyDomain = "alanta.com";
		public const string DefaultHost = "alanta.com";
		public const string DefaultScheme = "http";
		public const string GuestUser = "Guest";
		public const int DefaultInvitationExpiringTimeMins = 2; // minutes (ks 6/27/12 - Changed to 2 from 5 at Alex's request).
		public const string RecentContactGroupName = "Default";
		public const int CookieExpirationInDays = 14;
		public const string DefaultFromAddress = "alanta@alanta.com";
		public const int MaxGuestUserFileUploadSize = 2000000; // in bytes, roughly 2 MB
		public const int MaxRegisteredUserFileUploadSize = 100000000; // in bytes, roughly 100 MB
		public const int MaxSubscriptionUserFileUploadSize = 1000000000; // in bytes, roughly 1000 MB
		public const int AvatarSizeMax = 4000000; // in bytes, i.e., roughly 4 MB
		public const int DefaultTimeoutRoomGoToUnavailableSec = 60;
		public const string NoPhotoRelativePath = "/images/grey_body.png";
		public const string EmailIcon = "/Alanta.Client.UI.Common;component/images/email.png";
		public const string EmailIconBw = "/Alanta.Client.UI.Common;component/images/email_bw.png";
		public const string FacebookIcon = "/Alanta.Client.UI.Common;component/images/facebook.png";
		public const string FacebookIconBw = "/Alanta.Client.UI.Common;component/images/facebook_bw.png";
		public const string LinkedinIcon = "/Alanta.Client.UI.Common;component/images/linkedin.png";
		public const string LinkedinIconBw = "/Alanta.Client.UI.Common;component/images/linkedin_bw.png";
		public const string GmailIcon = "/Alanta.Client.UI.Common;component/images/gmail.png";
		public const string GmailIconBw = "/Alanta.Client.UI.Common;component/images/gmail_bw.png";
		public const string WhiteboardIconLocation = "/Alanta.Client.UI.Common;component/Images/ShareWhiteboard.png";
		public const string SharedDesktopIconLocation = "/Alanta.Client.UI.Common;component/Images/ShareDesktop.png";
		public const string StatisticsIconLocation = "/Alanta.Client.UI.Common;component/Images/ShareStatistics.png";
		public const string AvatarUploadHandler = "AvatarUploadHandler.ashx";
		public const string GoogleAnalyticsAccount = "UA-24490204-1";
		public const int InitialChatMessagesToRetrieve = 50;
		public const int MaxFacebookInviteContactsCount = 50; // limitations of "Requests" Dialog

	    public const string DefaultRoomUnavailableMessage =
	        "Hi, this is {0}. I'm very sorry, but I'm not available at the moment. Please leave me a message and the preferred method and time to contact you.";

		public const string DuplexHttpEndpointReference = "duplexHttpEndpoint";
		public const string DuplexTcpEndpointReference = "duplexTcpEndpoint";
		//public const string ContactsServEndpointReference = "contactsServiceEndpoint";

		public const string ServiceRelativeLocation = "RoomService/pollingDuplex";
		//public const string LocalCameraRelativePath = "/Resources/WLocalWebCam.swf";
		//public const string RemoteCameraRelativePath = "/Resources/WRemoteWebCam.swf";

		public const int LocalCameraWidth = 214;
		public const int LocalCameraHeight = 150;

		public const int MediaServerReconnectInterval = 15; // in seconds
		public const int KeepaliveInterval = 30; // in seconds

		public const string LpDestinationApp = "app";

		/// <summary>
		/// Some error conditions repeat too frequently to make it worthwhile to display an error message every time they occur.  
		/// This is the minimum time delay to insert between showing these messages.
		/// </summary>
		public const int RepeatedErrorMessageDelay = 120; // in seconds.  
		public const int SlideExpirationOfInvitationMins = 120; //minutes

		public const string Undefined = "Undefined";

		/// <summary>
		/// Minimum margin of Border for Hosts
		/// </summary>
		public const int MinMarginBorderTwice = 20;

		#endregion

		#region Pages

		public const string LoginPageUrl = "/login";
		public const string ContactsPageUrl = "/contacts";

		public static Uri DefaultInvitationUri = new Uri("http://alanta.com/");
		public static string RoomUnavailableMessage = "Hi, this is {0}. I'm very sorry, but I'm not available at the moment. Please leave me a message and the preferred method and time to contact you.";

		#endregion

		#region Regular Expressions

		// This matches either 'Ken Smith <smithkl42@gmail.com>' or 'smithkl42@gmail.com'
		public const string RegexEmailFull = "^((?>[a-zA-Z\\d!#$%&'*+\\-/=?^_`{|}~]+\x20*|\"((?=[\x01-\x7f])[^\"\\]|\\[\x01-\x7f])*\"\x20*)*" +
											 "(?<angle><))?((?!\\.)(?>\\.?[a-zA-Z\\d!#$%&'*+\\-/=?^_`{|}~]+)+|\"((?=[\x01-\x7f])[^\"\\]|\\[\x01-\x7f])*\")" +
											 "@(((?!-)[a-zA-Z\\d\\-]+(?<!-)\\.)+[a-zA-Z]{2,}|\\[(((?(?<!\\[)\\.)(25[0-5]|2[0-4]\\d|[01]?\\d?\\d)){4}|[a-zA-Z\\d\\-]*[a-zA-Z\\d]:" +
											 "((?=[\x01-\x7f])[^\\\\[\\]]|\\[\x01-\x7f])+)\\])(?(angle)>)$";

		// This matches just 'smithkl42@gmail.com'
		public const string RegexEmailSimple = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
											   @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
											   @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";

		public const string RegexEmailSimpleSearch = @"([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
											   @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
											   @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)";

		// Phone number check
		public const string RegexPhoneNumber = "^\\(?([0-9]{3})\\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$";

		// URL Regex, rb 7/18/10 added '/#' in first square brackets, for support beta.alanta.com/#/roomName
		// and ([^\s]\b) for remove punctuation in the end
		// 6/10/11 removed (^|[ \t\r\n]) because on converting Url to Anchor copying "\n"
		/// <summary>
		/// Simple URI
		// <see cref="http://www.truerwords.net/articles/ut/urlactivation.html"/>
		/// </summary>
		public const string RegexAbsoluteUrl = @"((ftp|http|https|gopher|mailto|news|nntp|telnet|wais|file|prospero|aim|webcal):(([A-Za-z0-9$_.+!*(),;/?/#:@&~=-])|%[A-Fa-f0-9]{2}){2,}(#([a-zA-Z0-9][a-zA-Z0-9$_.+!*(),;/?:@&~=%-]*))?([A-Za-z0-9$_+!*();/?:~-]))([^\s]\b)";
		//public const string RegexAbsoluteUrl = @"(^|[ \t\r\n])((ftp|http|https|gopher|mailto|news|nntp|telnet|wais|file|prospero|aim|webcal):(([A-Za-z0-9$_.+!*(),;/?/#:@&~=-])|%[A-Fa-f0-9]{2}){2,}(#([a-zA-Z0-9][a-zA-Z0-9$_.+!*(),;/?:@&~=%-]*))?([A-Za-z0-9$_+!*();/?:~-]))([^\s]\b)";

		public const string RegexAbsoluteUrlReplace = "<a href=\"$0\">$0</a>";

		#endregion

	}
}