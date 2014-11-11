using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Alanta.Client.Test.ViewModelTests;
using Alanta.Client.UI.Common.RoomView;
using Alanta.Client.UI.Common.ViewModels;
using Alanta.Common;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class RoomInfoTests : ViewModelTestBase
	{
		private const string tag1 = "tag1";
		private const string tag2 = "tag2";
		private const string tag3 = "tag3";
		private const string tag4 = "tag4";
		private readonly Guid invitationId;

		public RoomInfoTests()
		{
			invitationId = Guid.NewGuid();
		}

		[TestMethod]
		[Tag("roominfo")]
		public void RoomInfoUrlTest_All_NoDefault()
		{
			var companyVm = viewModelFactory.GetViewModel<CompanyViewModel>();
			companyVm.Model = TestGlobals.Company;
			var queryString = new Dictionary<string, string>();
			queryString[tag1] = tag1;
			queryString[tag2] = tag2;
			queryString[tag3] = tag3;
			queryString[tag4] = tag4;
			queryString[Constants.InvitationIdReference] = invitationId.ToString();
			var roomInfo = new RoomInfoUrl(queryString, companyVm);
			companyVm.Model.UseDefaultAuthenticationGroup = false;
			Assert.AreEqual(tag1, roomInfo.AuthenticationGroupTag);
			Assert.AreEqual(tag2, roomInfo.OwnerUserTag);
			Assert.AreEqual(tag3, roomInfo.RoomName);
			Assert.AreEqual(tag4, roomInfo.UserTag);
			Assert.AreEqual(invitationId, roomInfo.InvitationId);
		}

		[TestMethod]
		[Tag("roominfo")]
		public void RoomInfoUrlTest_All_UseDefault()
		{
			var companyVm = viewModelFactory.GetViewModel<CompanyViewModel>();
			companyVm.Model = TestGlobals.Company;
			var queryString = new Dictionary<string, string>();
			queryString[tag1] = tag1;
			queryString[tag2] = tag2;
			queryString[tag3] = tag3;
			queryString[Constants.InvitationIdReference] = invitationId.ToString();
			var roomInfo = new RoomInfoUrl(queryString, companyVm);
			companyVm.Model.UseDefaultAuthenticationGroup = true;
			Assert.AreEqual(tag1, roomInfo.OwnerUserTag);
			Assert.AreEqual(tag2, roomInfo.RoomName);
			Assert.AreEqual(tag3, roomInfo.UserTag);
			Assert.AreEqual(invitationId, roomInfo.InvitationId);
		}
	}
}
