using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Alanta.Client.Data.RoomService;
using Alanta.Client.Data.Social;
using Alanta.Client.UI.Common.ViewModels;
using Alanta.Client.UI.Desktop.RoomView.Invitations;
using Alanta.Client.Whiteboard;
using Alanta.Client.Whiteboard.Controls.WShapes;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	/// <remarks>
	/// rb 7/12/2010 WARNING: ClientTestInitialize() calls Thread.Sleep() to avoid a NullReferenceException in various places. 
	/// There may be a better way of handling that, but it would involve rewriting more of the code than we care to at this point.
	/// </remarks>
	[TestClass]
	[Tag("ui")]
	public class RoomViewUiTests : UiTestBase
	{
		[TestMethod]
		[Asynchronous]
		[Timeout(30000)]
		[Tag("ui")]
		[Tag("workspace")]
		public void WorkspacePanelTest()
		{
			bool whiteboardAdded = false;
			var whiteboardViewModels = viewModelFactory.GetViewModel<WhiteboardCollectionViewModel>();
			var whiteboardModels = new ObservableCollection<Data.RoomService.Whiteboard>();
			whiteboardViewModels.Models = whiteboardModels;
			var whiteboard = new Data.RoomService.Whiteboard { WhiteboardId = Guid.NewGuid(), WhiteboardShapes = new ObservableCollection<WhiteboardShape>() };

			whiteboardViewModels.ViewModels.CollectionChanged += (sender, args) =>
			{
				var vm = args.NewItems[0] as WhiteboardViewModel;
				Assert.AreEqual(whiteboard, vm.Model);
				whiteboardAdded = true;
			};

			whiteboardModels.Add(whiteboard);
			EnqueueConditional(() => whiteboardAdded);
			EnqueueTestComplete();
		}

		//[TestMethod]
		//[Asynchronous]
		//public void ContactsControlTest()
		//{
		//    var items = contactList.ContactCollectionViewSource;
		//    Assert.IsTrue(items == null || items.Source == null || items.View.OfType<ContactAdapter>().Count() == 0, "ContactItems should be empty");
		//    EnqueueTestComplete();
		//}

		//[TestMethod]
		//[Asynchronous]
		//public void ContactItemTest()
		//{
		//    ContactItem contactItem = new ContactItem();
		//    contactItem.Initialize(roomController);
		//    ContactAdapter contact = new ContactAdapter() { FirstName = "Ken", LastName = "Smith", AvatarUrl = "http://alanta.com/images/image1.jpg" };
		//    contactItem.DataContext = contact;
		//    Assert.AreSame(contact, contactItem.DataContext);
		//    EnqueueTestComplete();
		//}

		//[TestMethod]
		//[Asynchronous]
		//public void NewContactFormTest()
		//{
		//    bool formClosed = false;
		//    NewContactForm contactForm = new NewContactForm();
		//    contactForm.Initialize(roomController);
		//    contactForm.Closed += (sender, arg) =>
		//        {
		//            formClosed = true;
		//        };

		//    contactForm.CloseForm();
		//    EnqueueConditional(() => formClosed);
		//    EnqueueTestComplete();
		//}

		//[TestMethod]
		//public void ThumbnailPanelTest()
		//{
		//    ThumbnailPanel thumbnailPanel = new ThumbnailPanel();
		//    FrameworkElement element1 = new UserControl() { Tag = "Element1" };
		//    FrameworkElement element2 = new UserControl() { Tag = "Element2" };
		//    FrameworkElement element3 = new UserControl() { Tag = "Element3" };
		//    thumbnailPanel.AddThumbnailItem(element1);
		//    thumbnailPanel.AddThumbnailItem(element2);
		//    thumbnailPanel.AddThumbnailItem(element3);

		//    Assert.IsNull(thumbnailPanel.SelectedItem);
		//    thumbnailPanel.SelectThumbnasilItem(element2);
		//    Assert.IsNotNull(thumbnailPanel.SelectedItem);
		//    Assert.IsNotNull(thumbnailPanel.SelectedItem.OwnerControl);
		//    Assert.AreSame(element2, thumbnailPanel.SelectedItem.OwnerControl, "SelectedItem is incorrect");
		//    ThumbnailItem thumbnailElement2 = thumbnailPanel.SelectedItem;
		//    thumbnailPanel.SelectThumbnasilItem(element1);
		//    executedTests++;
		//}

		[TestMethod]
		[Tag("ui")]
		public void WhiteboardShapeTest()
		{
			var ellipseElement = WElementBase.GetElementFromShape(new WhiteboardShapeEllipse { BorderColor = Colors.Gray.ToString() }, null) as WEllipse;
			var rectangleElement = WElementBase.GetElementFromShape(new WhiteboardShapeRectangle { BorderColor = Colors.Gray.ToString() }, null) as WRectangle;
			var textElement = WElementBase.GetElementFromShape(new WhiteboardShapeText { BorderColor = Colors.Gray.ToString() }, null) as WText;

			var polyline = new WhiteboardShapePolyline { BorderColor = Colors.Gray.ToString() };
			polyline.Data = "0,0;1,1";
			var polylineElement = WElementBase.GetElementFromShape(polyline, null) as WPolyline;

			Assert.IsNotNull(ellipseElement);
			Assert.IsNotNull(rectangleElement);
			Assert.IsNotNull(textElement);
			Assert.IsNotNull(polylineElement);
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(30000)]
		[Priority(5)]
		[Tag("ui")]
		public void CursorManagerTest()
		{
			bool cursorTypeChanged = false;
			var cursorManager = new CursorManager(new Canvas());
			cursorManager.CursorTypeChanged += (sender, arg) =>
			{
				Assert.AreEqual(Selectors.Rectangle, arg.CursorType);
				cursorTypeChanged = true;
			};
			cursorManager.CursorType = Selectors.Rectangle;
			EnqueueConditional(() => cursorTypeChanged);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(30000)]
		[Tag("ui")]
		public void RichInvitationBox_ParsingEmails()
		{
			var invitationBox = new RichInvitationBox();
			invitationBox.Initialize(roomController);

			// simple email 1
			var simpleEmail1 = new RichItemText { Text = "test1@mail.com" };

			// group 1
			var group1Part1 = new RichItemText { Text = "User1 Name1" };
			var group1Part2 = new RichItemText { Text = "<testgroup1@mail.com>" };
			var group1Part3 = new RichItemText { Text = ",testgroup1Part2@mail.com;" };

			// simple email 2
			var simpleEmail2 = new RichItemText { Text = "TeSt2@gmail.com" };

			// wrong email 1
			var wrongEmail1 = new RichItemText { Text = "_asdf...b.com," };

			// group 2
			var group2Part1 = new RichItemText { Text = "User2" };
			var group2Part2 = new RichItemText { Text = "Name2" };
			var group2Part3 = new RichItemText { Text = "<testgroup2@" };
			var group2Part4 = new RichItemText { Text = "hotmail.zzz>" };


			var itemContact1 = new RichItemContact();
			var mc1 = new MergedContact();
			mc1.FullName = "Full Name111";
			mc1.ContactPoints = new ObservableCollection<ContactPoint>();
			mc1.ContactPoints.Add(new ContactPoint
								  {
									  ContactPointId = "gmailtest@mail.com",
									  ContactPointTypeId = (byte)ContactDeliveries.Gmail
								  });
			var invitation1 = new MeetingInvitation(mc1, mc1.ContactPoints.First());
			itemContact1.Invitation = invitation1;

			var richRows = new List<RichInvitationBox.RichRow>();
			richRows.Add(new RichInvitationBox.RichRow());
			richRows.Add(new RichInvitationBox.RichRow());
			richRows[0].Items.Add(simpleEmail1);
			richRows[0].Items.Add(itemContact1);

			richRows[0].Items.Add(group1Part1);
			richRows[0].Items.Add(new RichItemSpace());
			richRows[1].Items.Add(group1Part2);
			richRows[1].Items.Add(group1Part3);

			richRows[1].Items.Add(simpleEmail2);

			richRows[1].Items.Add(wrongEmail1);

			richRows[1].Items.Add(group2Part1);
			richRows[1].Items.Add(new RichItemSpace());
			richRows[1].Items.Add(new RichItemSpace());
			richRows[1].Items.Add(new RichItemSpace());
			richRows[1].Items.Add(group2Part2);
			richRows[1].Items.Add(new RichItemSpace());
			richRows[1].Items.Add(group2Part3);
			richRows[1].Items.Add(group2Part4);

			bool isItemsValid = false;
			var invitations = new List<MeetingInvitation>();

			//Grouped list of texts which separeted by Controls or "," or "." or ";"
			List<List<IRichItem>> listTextItems;
			List<MeetingInvitation> contactInvitations;
			invitationBox.GetContactInvitationsFromRows(richRows, out listTextItems, out contactInvitations);
			invitations.AddRange(contactInvitations);

			Assert.IsTrue(contactInvitations.Count == 1, "should be one Contact");

			// Parsing (concatenate) texts for getting email addresses
			List<List<RichItemText>> itemsNotRecognized;
			List<MeetingInvitation> textEmailsInvitations;
			invitationBox.GetEmailInvitationsFromText(listTextItems, out textEmailsInvitations, out itemsNotRecognized);
			invitations.AddRange(textEmailsInvitations);

			Assert.IsTrue(textEmailsInvitations.Count == 5, "should be 5 'direct' email contacts");
			Assert.IsTrue(invitations.Count == 6, "count of invitations is wrong");
			Assert.IsTrue(invitations.Contains(itemContact1.Invitation), "GmailContact is not added to invitations");
			Assert.IsTrue(itemsNotRecognized.Sum(group => group.Count) == 1, "should be one wrong text element");
			Assert.IsFalse(isItemsValid, "expected that not all texts is valid");
			if (itemsNotRecognized.Sum(group => group.Count) > 0)
			{
				Assert.IsTrue(wrongEmail1.Text == itemsNotRecognized[0][0].Text, "wrong expected error");
				Assert.IsTrue(!itemsNotRecognized[0][0].IsEmail);
			}

			EnqueueTestComplete();
		}

		[TestMethod]
		[Tag("ui")]
		public void TestContactPointsMerging()
		{
			roomController.InitializeContactAccessors();
			var contactController = roomController.ContactController;
			string fullnameMCShare = "Merged Contact Share";
			contactController.MergedContacts.Clear();
			var mc11 = new MergedContact();
			mc11.FullName = fullnameMCShare;
			mc11.ContactPoints = new ObservableCollection<ContactPoint>();
			mc11.ContactPoints.Add(new ContactPoint
								   {
									   ContactPointId = "merged1@mail.com",
									   ContactPointTypeId = (byte)ContactPointTypes.Email
								   });
			mc11.ContactPoints.Add(new ContactPoint
								   {
									   ContactPointId = "share@gmail.com",
									   ContactPointTypeId = (byte)ContactPointTypes.Gmail
								   });

			var mc12 = new MergedContact();
			mc12.FullName = fullnameMCShare;
			mc12.ContactPoints = new ObservableCollection<ContactPoint>();
			mc12.ContactPoints.Add(new ContactPoint
								   {
									   ContactPointId = "share@gmail.com",
									   ContactPointTypeId = (byte)ContactPointTypes.Gmail
								   });
			mc12.ContactPoints.Add(new ContactPoint
								   {
									   ContactPointId = "contact_skype",
									   ContactPointTypeId = (byte)ContactPointTypes.Skype
								   });
			mc12.MergedContactId = Guid.NewGuid();
			mc12.AvatarUrl = "/avatarurl/image.png";
			contactController.MergedContacts.Add(mc11);
			contactController.MergeContact(mc12);
			Assert.IsTrue(contactController.MergedContacts.Count == 1);
			MergedContact mc = contactController.MergedContacts.First();
			//    Assert.IsTrue(mc.ContactPoints.Count == 3);
			Assert.IsTrue(mc.FullName == fullnameMCShare);
		}

		[TestMethod]
		[Tag("ui")]
		public void TestMergingByCPointsAndAddr()
		{
			roomController.InitializeContactAccessors();
			var contactController = roomController.ContactController;
			contactController.MergedContacts.Clear();
			var mc11 = new MergedContact();
			mc11.FullName = "Merged Contact1";
			mc11.ContactPoints = new ObservableCollection<ContactPoint>();
			mc11.ContactPoints.Add(new ContactPoint
								   {
									   ContactPointId = "merged_mail@mail.com",
									   ContactPointTypeId = (byte)ContactPointTypes.Email
								   });
			mc11.ContactPoints.Add(new ContactPoint
								   {
									   ContactPointId = "share@gmail.com",
									   ContactPointTypeId = (byte)ContactPointTypes.Gmail
								   });
			mc11.ContactPhysicalAddresses = new ObservableCollection<ContactPhysicalAddress>();
			mc11.ContactPhysicalAddresses.Add(new ContactPhysicalAddress
									   {
										   Address1 = "Address share"
									   });

			var mc12 = new MergedContact();
			mc12.FullName = "Merged Contact2";
			mc12.ContactPoints = new ObservableCollection<ContactPoint>();
			mc12.ContactPoints.Add(new ContactPoint
								   {
									   ContactPointId = "merged_mail@mail.com",
									   ContactPointTypeId = (byte)ContactPointTypes.Email
								   });
			mc12.ContactPoints.Add(new ContactPoint
								   {
									   ContactPointId = "share@gmail.com",
									   ContactPointTypeId = (byte)ContactPointTypes.Gmail
								   });
			mc12.ContactPoints.Add(new ContactPoint
								   {
									   ContactPointId = "contact_skype",
									   ContactPointTypeId = (byte)ContactPointTypes.Skype
								   });
			mc12.ContactPhysicalAddresses = new ObservableCollection<ContactPhysicalAddress>();
			mc12.ContactPhysicalAddresses.Add(new ContactPhysicalAddress
									   {
										   Address1 = "Address share"
									   });

			contactController.MergedContacts.Add(mc11);
			contactController.MergeContact(mc12);
			Assert.IsTrue(contactController.MergedContacts.Count == 1);
		}
	}
}