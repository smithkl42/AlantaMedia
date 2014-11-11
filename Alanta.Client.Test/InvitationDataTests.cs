using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Alanta.Client.Common;
using Alanta.Client.Data.RoomService;
using Alanta.Client.Data.Social;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class InvitationDataTests : UiTestBase
	{
		[TestMethod]
		[Asynchronous]
		[Tag("invitations")]
		[Tag("intervention")]
		public void TestSendInvitationsViaEmail()
		{
			bool isCompleted = false;
			var invitations = new List<MeetingInvitation>();
			var mc = new MergedContact();
			mc.FullName = "Merged Contact";
			mc.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.EmailGmail1,
				ContactPointTypeId = (byte)ContactPointTypes.Email
			});
			mc.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.EmailGmail2,
				ContactPointTypeId = (byte)ContactPointTypes.Email
			});
			mc.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.EmailYahoo,
				ContactPointTypeId = (byte)ContactPointTypes.Email
			});
			invitations.Add(new MeetingInvitation(mc, mc.ContactPoints[0]));
			invitations.Add(new MeetingInvitation(mc, mc.ContactPoints[1]));
			invitations.Add(new MeetingInvitation(mc, mc.ContactPoints[2]));
			string body = "TestSendInvitaionsViaEmail() " + DateTime.Now.ToString();
			invitations.ForEach(i => { i.Body = body; i.Subject = "Test"; });
			roomController.ContactController.SendInvitations(invitations, (err, resultInvitations, resString) =>
			{
				Assert.IsNull(err);
				Assert.IsNotNull(resultInvitations);
				resultInvitations.ForEach(inv => Assert.IsTrue(inv.Result == ResultState.Success));
				Assert.IsTrue(resultInvitations.Count == 3);
				Assert.IsTrue(resString == ContactDeliveries.Email);
				isCompleted = true;
			}, roomController.AllContactAccessors);
			EnqueueConditional(() => isCompleted);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("invitations")]
		[Tag("intervention")]
		public void TestSendInvitationsViaGmail()
		{
			bool isCompleted = false;
			var invitations = new List<MeetingInvitation>();
			var mc = new MergedContact();
			mc.FullName = "Merged Contact";
			mc.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.EmailGmail1,
				ContactPointTypeId = (byte)ContactPointTypes.Gmail
			});
			mc.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.EmailGmail2,
				ContactPointTypeId = (byte)ContactPointTypes.Gmail
			});
			invitations.Add(new MeetingInvitation(mc, mc.ContactPoints[0]));
			invitations.Add(new MeetingInvitation(mc, mc.ContactPoints[1]));
			string body = "TestSendInvitaionsViaGmail() " + DateTime.Now.ToString();
			invitations.ForEach(i => { i.Body = body; i.Subject = "Test"; });
			roomController.ContactController.SendInvitations(invitations, (err, resultInvitations, resString) =>
			{
				Assert.IsNull(err);
				Assert.IsNotNull(resultInvitations);
				Assert.IsTrue(resultInvitations.Count == 2);
				Assert.IsTrue(resString == ContactDeliveries.Gmail);
				isCompleted = true;
			}, roomController.AllContactAccessors);
			EnqueueConditional(() => isCompleted);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Timeout(60000)]
		[Asynchronous]
		[Tag("invitations")]
		[Tag("intervention")]
		public void TestSendInvitaionsViaLinkedIn()
		{
			bool isCompleted = false;
			var invitations = new List<MeetingInvitation>();
			var mc = new MergedContact();
			mc.FullName = "Merged ContactLinkedIn";
			mc.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.LinkedIn2UserId,
				ContactPointTypeId = (byte)ContactPointTypes.Linkedin
			});
			invitations.Add(new MeetingInvitation(mc, mc.ContactPoints[0]));
			string body = "TestSendInvitaionsViaLinkedIn() " + DateTime.Now.ToString();
			invitations.ForEach(i => { i.Body = body; i.Subject = "Test"; });
			var linkedinAccess = roomController.ContactAccessors.FirstOrDefault(ca => ca.ContactDelivery == ContactDeliveries.Linkedin) as LinkedinAccess;
			Assert.IsNotNull(linkedinAccess);
			Debug.Assert(linkedinAccess != null);
			linkedinAccess.Login(err1 =>
			{
				Assert.IsNull(err1);
				roomController.ContactController.SendInvitations(invitations, (err, resultInvitations, resString) =>
				{
					Assert.IsNull(err);
					Assert.IsNotNull(resultInvitations);
					resultInvitations.ForEach(i => Assert.AreEqual(ResultState.Success, i.Result));
					Assert.IsTrue(resultInvitations.Count == 1);
					Assert.IsTrue(resString == ContactDeliveries.Linkedin);
					isCompleted = true;
				}, roomController.AllContactAccessors);
			});
			EnqueueConditional(() => isCompleted);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Timeout(60000)]
		[Asynchronous]
		[Tag("invitations")]
		[Tag("intervention")]
		public void TestSendInvitationsViaFacebook()
		{
			bool isCompleted = false;
			var invitations = new List<MeetingInvitation>();
			var mc = new MergedContact();
			mc.FullName = "Merged ContactLinkedIn";
			mc.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.Facebook2UserId,
				ContactPointTypeId = (byte)ContactPointTypes.Facebook
			});
			invitations.Add(new MeetingInvitation(mc, mc.ContactPoints[0]));
			string body = "TestSendInvitaionsViaFacebook() " + DateTime.Now;
			invitations.ForEach(i => { i.Body = body; i.Subject = "Test"; });
			var facebookAccess = roomController.ContactAccessors.FirstOrDefault(ca => ca.ContactDelivery == ContactDeliveries.Facebook) as FacebookAccess;
			Assert.IsNotNull(facebookAccess);
			Debug.Assert(facebookAccess != null);
			facebookAccess.Login(err1 =>
			{
				Assert.IsNull(err1);
				roomController.ContactController.SendInvitations(invitations, (err, resultInvitations, resString) =>
				{
					Assert.IsNull(err);
					Assert.IsNotNull(resultInvitations);
					resultInvitations.ForEach(i => Assert.AreEqual(ResultState.Success, i.Result));
					Assert.IsTrue(resultInvitations.Count == 1);
					Assert.IsTrue(resString == ContactDeliveries.Facebook);
					isCompleted = true;
				}, roomController.AllContactAccessors);
			});
			EnqueueConditional(() => isCompleted);
			EnqueueTestComplete();
		}

		//[TestMethod]
		//[Asynchronous]
		//[Tag("Invitation")]
		//public void SendInvitationsTest()
		//{
		//    bool invitationsSent = false;
		//    var invitations = new List<MeetingInvitation>();
		//    invitations.Add(GetInvitation("smithkl42@gmail.com"));
		//    invitations.Add(GetInvitation("smithkl42@hotmail.com"));

		//    // Initialize the repository event handler, which tells us that we can start communicating normally..
		//    EventHandler<PropertyChangedEventArgs<Room>> handleRepositoryInitialized = null;
		//    handleRepositoryInitialized = (s, e) =>
		//    {
		//        dataPublisher.RoomViewModelInitialized -= handleRepositoryInitialized;
		//        contactData.SendInvitations(invitations, (error, result, networkName) =>
		//            {
		//                MeetingInvitation invitation;
		//                invitation = result.FirstOrDefault(i => i.Contact.ContactPointId == "smithkl42@gmail.com");
		//                Assert.IsNotNull(invitation);
		//                Assert.AreEqual(ResultState.Success, invitation.Result);
		//                invitation = result.FirstOrDefault(i => i.Contact.ContactPointId == "smithkl42@hotmail.com");
		//                Assert.IsNotNull(invitation);
		//                Assert.AreEqual(ResultState.Success, invitation.Result);
		//                invitationsSent = true;
		//            });
		//    };
		//    dataPublisher.RoomViewModelInitialized += handleRepositoryInitialized;

		//    // Kick everything off.
		//    JoinRoom(null);
		//    EnqueueConditional(() => invitationsSent);
		//    EnqueueTestComplete();
		//}

		//private MeetingInvitation GetInvitation(string addressTo)
		//{
		//    //Contact contact = new Contact();
		//    //contact.Email = addressTo;
		//    //contact.Name = addressTo;
		//    //AlantaContactAdapter alantaContact = new AlantaContactAdapter(contact, roomController.ContactData);
		//    //MergedContact mc = new MergedContact();
		//    //mc.MergeContact(alantaContact);
		//    MergedContact mc = new MergedContact();
		//    mc.FullName = addressTo;
		//    mc.ContactPoints.Add(new ContactPoint()
		//    {
		//        ContactPointId = addressTo,
		//        ContactPointTypeId = (byte)ContactPointTypes.Email
		//    });
		//    MeetingInvitation invitation = new MeetingInvitation(mc, mc.ContactPoints[0], userViewModel.Model);
		//    invitation.Body = "<rgbaSample>Hello, world!</rgbaSample>";
		//    invitation.Subject = "Test Message";
		//    return invitation;
		//}
	}
}
