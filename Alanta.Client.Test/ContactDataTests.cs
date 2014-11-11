using System;
using System.Collections.Generic;
using System.Linq;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.Data.Social;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class ContactDataTests : DataTestBase
	{
		const string testGroupName = "TEST_GROUP";
		readonly Guid _testGroupId = new Guid("f674e93c-2b35-4cb2-b649-c687e1c6a13f");
		private ContactGroup _testContactGroup;
		private ContactController _contactController;

		public override void TestInitializing(Action callback)
		{
			_localUserVm = _viewModelFactory.GetViewModel<LocalUserViewModel>();
			_localUserVm.Model = TestGlobals.User;
			_localUserVm.Password = TestGlobals.Password;
			_localUserVm.Login(callback: loginError =>
			{
				_contactController = new ContactController(_roomService);
				_contactData = ContactData.GetContactData(_contactController, _localUserVm.UserId, _localUserVm.LoginSession);
				_contactData.AddContactGroup(_testGroupId, testGroupName, null, (err, contactGroup) =>
				{
					_testContactGroup = contactGroup;
					callback();
				});
			});
		}

		public override void TestCleaning(Action callback)
		{
			_roomService.DeleteContactGroup(_testGroupId, _localUserVm.LoginSession, err => callback());
		}

		[TestMethod]
		[Asynchronous]
		[Tag("contacts")]
		public void Add_Get_DeleteContactGroup()
		{
			bool isCompleted = false;
			string groupName = "testAddGetDeleteGroup";
			var groupId = Guid.NewGuid();
			var contacts = CreateTestContacts();
			_contactData.AddContactGroup(groupId, groupName, contacts, (addError, contactGroup) =>
				{
					Assert.IsNull(addError, "Add contact group failed");
					_roomService.GetContactGroups(_localUserVm.LoginSession, (getError, resultGroups) =>
						{
							Assert.IsNull(getError, "Getting groups failed");
							Assert.IsTrue(resultGroups.Any(cg => cg.GroupName == groupName), "Group not found");
							_roomService.DeleteContactGroup(groupId, _localUserVm.LoginSession, deleteError =>
								{
									Assert.IsNull(deleteError, "Deleting group failed");
									isCompleted = true;
								});
						});
				});

			EnqueueConditional(() => isCompleted);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("contacts")]
		public void Add_Get_ContactsToGroup()
		{
			bool isCompleted = false;
			var contacts = CreateTestContacts();

			_contactData.AddContactsToGroup(contacts, _testContactGroup.ContactGroupId, err1 =>
				{
					Assert.IsNull(err1, "Add contacts to group failed");
					_roomService.GetMergedContacts(_localUserVm.LoginSession, (err2, resultContacts) =>
						{
							Assert.IsNull(err2, "Get all contacts failed");
							double contactsCount = contacts.Count;
							Assert.IsTrue(contactsCount == resultContacts.Count);
							var contactsUnitedCount = (from c1 in contacts
													   join c2 in resultContacts on c1.MergedContactId equals c2.MergedContactId
													   select c1).Count();
							Assert.IsTrue(contactsCount == contactsUnitedCount, "MergedContactId of contacts is different");
							var mc1 = resultContacts.Single(c => c.MergedContactId == contacts[0].MergedContactId);
							Assert.IsTrue(contacts[0].ContactPhysicalAddresses.Count == mc1.ContactPhysicalAddresses.Count);
							Assert.IsTrue(contacts[0].LinkedContacts.Count == mc1.LinkedContacts.Count);
							Assert.IsTrue(contacts[0].ContactPoints.Count == mc1.ContactPoints.Count);
							isCompleted = true;
						});
				});

			EnqueueConditional(() => isCompleted);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("contacts")]
		public void Add_Update_Get_DeleteContact()
		{
			bool isCompleted = false;
			var testContacts = CreateTestContacts();
			var mc1 = testContacts[0];
			var mc2 = testContacts[1];
			_contactData.AddContactsToGroup(testContacts, _testContactGroup.ContactGroupId, addError =>
				{
					Assert.IsNull(addError, "Add contact to group failed");
					mc1.FullName = "MergedContact Updated";
					string addressName = "addr1Test";
					string contactPointId = TestConstants.Facebook2UserId;
					mc1.ContactPoints.Add(new ContactPoint
					{
						ContactPointId = contactPointId,
						ContactPointTypeId = (byte)ContactPointTypes.Facebook
					});
					mc1.ContactPhysicalAddresses.Add(new ContactPhysicalAddress
					{
						Address1 = addressName,
						City = "City1"
					});
					_contactData.SaveContact(mc1, saveError =>
						{
							Assert.IsNull(saveError, "Save contact failed");
							_roomService.GetMergedContacts(_localUserVm.LoginSession, (getError, contacts) =>
								{
									Assert.IsNull(getError, "Get contacts failed");
									var mcRetrieved = contacts.Single(c => c.MergedContactId == mc1.MergedContactId);
									Assert.IsNotNull(mcRetrieved, "Not found merged contact");
									Assert.IsNotNull(mcRetrieved.ContactPhysicalAddresses.FirstOrDefault(a => a.Address1 == addressName), "Address not found");
									Assert.IsNotNull(mcRetrieved.ContactPoints.FirstOrDefault(c => c.ContactPointId == contactPointId), "ContactPoint not found");
									Assert.AreEqual(mc1.FullName, mcRetrieved.FullName);
									_roomService.RemoveContactsFromGroup(new List<Guid> { mc1.MergedContactId }, _testContactGroup.ContactGroupId, _localUserVm.LoginSession, removeError =>
										{
											Assert.IsNull(removeError, "Deleting contact failed");
											_roomService.DeleteContact(mc1.MergedContactId, _localUserVm.LoginSession, delete1Error =>
											{
												Assert.IsNull(delete1Error);
												_roomService.DeleteContact(mc2.MergedContactId, _localUserVm.LoginSession, delete2Error =>
												{
													Assert.IsNull(delete2Error);
													isCompleted = true;
												});
											});
										});
								});
						});
				});

			EnqueueConditional(() => isCompleted);
			EnqueueTestComplete();
		}


		[TestMethod]
		[Asynchronous]
		[Tag("contacts")]
		public void SaveDefaultContactGroup()
		{
			bool isCompleted = false;
			var invitations = new List<MeetingInvitation>();
			var mergedContacts = CreateTestContacts();
			foreach (var contact in mergedContacts)
			{
				foreach (var contactPoint in contact.ContactPointsDeliveryRead)
				{
					invitations.Add(new MeetingInvitation(contact, contactPoint));
				}
			}
			_contactData.SaveDefaultContactGroup(invitations, err =>
				{
					Assert.IsNull(err, "Save contacts in default group failed");
					isCompleted = true;
				});

			EnqueueConditional(() => isCompleted);
			EnqueueTestComplete();
		}

		private static List<MergedContact> CreateTestContacts()
		{
			var mc1 = new MergedContact();
			mc1.MergedContactId = Guid.NewGuid();
			mc1.FullName = "Merged Contact1";
			mc1.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = "skype_contact1",
				ContactPointTypeId = (byte)ContactPointTypes.Skype
			});
			mc1.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.EmailGmail1,
				ContactPointTypeId = (byte)ContactPointTypes.Skype
			});
			mc1.LinkedContacts.Add(new LinkedContact
			{
				SourceType = (byte)ContactPointTypes.Gmail
			});

			mc1.ContactPhysicalAddresses.Add(new ContactPhysicalAddress
			{
				Address1 = "Address1111111",
				Address2 = "Address1111112",
				City = "City1",
				LocationType = (byte)LocationTypes.Home,
				IsManual = true
			});
			mc1.ContactPhysicalAddresses.Add(new ContactPhysicalAddress
			{
				Address1 = "Address1111111bb",
				Address2 = "Address1111112bbb",
				City = "City1bbb",
				LocationType = (byte)LocationTypes.Work,
				IsManual = true
			});


			var mc2 = new MergedContact();
			mc2.MergedContactId = Guid.NewGuid();
			mc2.FullName = "Merged Contact2";
			mc2.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.Facebook1UserId,
				ContactPointTypeId = (byte)ContactPointTypes.Facebook
			});
			mc2.ContactPoints.Add(new ContactPoint
			{
				ContactPointId = TestConstants.LinkedIn1UserId,
				ContactPointTypeId = (byte)ContactPointTypes.Linkedin
			});
			mc2.LinkedContacts.Add(new LinkedContact
			{
				SourceType = (byte)ContactPointTypes.Facebook
			});
			mc2.LinkedContacts.Add(new LinkedContact
			{
				SourceType = (byte)ContactPointTypes.Linkedin
			});

			var contacts = new List<MergedContact>();
			contacts.Add(mc1);
			contacts.Add(mc2);
			return contacts;
		}

		//[TestMethod]
		//[Asynchronous]
		//public void AddContactTest()
		//{
		//    // Create the contact that will be added.
		//    Contact contact = new Contact()
		//    {
		//        ContactId = Guid.NewGuid(),
		//        Email = "a@g.com",
		//        Name = "John Smith",
		//        UserId = TestGlobals.UserId
		//    };
		//    bool contactsAdded = false;

		//    // Initialize the repository event handler, which tells us that we can start communicating normally..
		//    EventHandler<PropertyChangedEventArgs<Room>> handleRepositoryInitialized = null;
		//    handleRepositoryInitialized = (s, e) =>
		//    {
		//        dataPublisher.RoomViewModelInitialized -= handleRepositoryInitialized;
		//        contactData.AddContact(contact, null);
		//    };
		//    dataPublisher.RoomViewModelInitialized += handleRepositoryInitialized;

		//    // Initialize the chatMessageAdded event handler, which will tell us that the contact was added successfully.
		//    EventHandler<GenericEventArgs<ObservableCollection<Contact>>> handleContactsAdded = null;
		//    handleContactsAdded = (s, e) =>
		//    {
		//        dataPublisher.ContactsReceived -= handleContactsAdded;
		//        ObservableCollection<Contact> contacts = e.Value;
		//        Contact newContact = contacts.FirstOrDefault();
		//        Assert.IsNotNull(newContact);
		//        Assert.AreEqual(contact.ContactId, newContact.ContactId);
		//        Assert.AreEqual(contact.Email, newContact.Email);
		//        Assert.AreEqual(contact.Name, newContact.Name);
		//        contactsAdded = true;
		//    };
		//    dataPublisher.ContactsReceived += handleContactsAdded;

		//    // Kick everything off.
		//    JoinRoom(null);
		//    EnqueueConditional(() => contactsAdded);
		//    EnqueueTestComplete();
		//}

		//[TestMethod]
		//[Asynchronous]
		//public void GetContactsTest()
		//{
		//    // Create the contact that will be added.
		//    Contact contact = new Contact()
		//    {
		//        ContactId = Guid.NewGuid(),
		//        Email = "a@g.com",
		//        Name = "John Smith",
		//        UserId = TestGlobals.UserId
		//    };
		//    bool contactsReceived = false;
		//    ObservableCollection<Contact> contacts = null;
		//    bool callingGetContacts = false;

		//    // Initialize the repository event handler, which tells us that we can start communicating normally..
		//    EventHandler<PropertyChangedEventArgs<Room>> handleRepositoryInitialized = null;
		//    handleRepositoryInitialized = (s, e) =>
		//    {
		//        dataPublisher.RoomViewModelInitialized -= handleRepositoryInitialized;
		//        contactData.AddContact(contact, null);
		//    };
		//    dataPublisher.RoomViewModelInitialized += handleRepositoryInitialized;

		//    // Initialize the chatMessageAdded event handler, which will tell us that the contact was added successfully.
		//    EventHandler<GenericEventArgs<ObservableCollection<Contact>>> handleContactsAdded = null;
		//    handleContactsAdded = (s, e) =>
		//    {
		//        contacts = e.Value;
		//        Contact newContact = contacts.FirstOrDefault(c => c.ContactId == contact.ContactId);
		//        Assert.IsNotNull(newContact);
		//        Assert.AreEqual(contact.ContactId, newContact.ContactId);
		//        Assert.AreEqual(contact.Email, newContact.Email);
		//        Assert.AreEqual(contact.Name, newContact.Name);
		//        if (!callingGetContacts)
		//        {
		//            callingGetContacts = true;
		//            ContactRetrievalState state = new ContactRetrievalState(null, null, null);
		//            contactData.GetContacts(state);
		//        }
		//        else
		//        {
		//            dataPublisher.ContactsReceived -= handleContactsAdded;
		//            contactsReceived = true;
		//        }
		//    };
		//    dataPublisher.ContactsReceived += handleContactsAdded;

		//    // Kick everything off.
		//    JoinRoom(null);
		//    EnqueueConditional(() => contactsReceived);
		//    EnqueueTestComplete();
		//}
	}
}
