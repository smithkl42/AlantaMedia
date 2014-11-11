using System;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.SampleData;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.Silverlight.Testing.UnitTesting.Metadata.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class DataPublisherTests : SilverlightTest
	{
		private bool eventRaised;
		private TestController controller;
		private ViewModelFactory viewModelFactory;

		public DataPublisherTests()
		{
		}

		[ClassInitialize]
		public static void ClassInitialize()
		{
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
		}

		[TestInitialize]
		public void TestInitialize()
		{
			var roomService = new DesignTimeRoomServiceAdapter();
			var viewLocator = new ViewLocator();
			var messageService = new TestMessageService();
			viewModelFactory = new ViewModelFactory(roomService, messageService, viewLocator);
			controller = new TestController(TestGlobals.UserTag, Guid.NewGuid().ToString(), viewModelFactory, new TestCompanyInfo());
		}

		[TestCleanup]
		[Asynchronous]
		public void TestCleanup()
		{
			eventRaised = false;
			ClientLogger.Debug(" -- Beginning test cleanup.");
			EnqueueCallback(() => controller.RoomService.CloseClientAsync(error => EnqueueTestComplete()));
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseRepositoryInitializedTest()
		{
			RoomViewModel dp = controller.RoomVm;
			var oldRoom = new Room();
			var newRoom = new Room();
			dp.RoomViewModelInitialized += (s, e) =>
				{
					eventRaised = true;
					Assert.AreSame(oldRoom, e.OldValue, "e.OldValue did not match the expected room.");
					Assert.AreSame(newRoom, e.NewValue, "e.NewValue did not match the expected room.");
				};
			dp.RaiseRoomViewModelInitialized(oldRoom, newRoom);
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseOwnerChanged()
		{
			RoomViewModel dp = controller.RoomVm;
			var oldValue = new User();
			var newValue = new User();
			dp.OwnerChanged += (s, e) =>
			{
				eventRaised = true;
				Assert.AreSame(oldValue, e.OldValue, "e.OldValue did not match the expected room.");
				Assert.AreSame(newValue, e.NewValue, "e.NewValue did not match the expected room.");
			};
			dp.RaiseOwnerChanged(oldValue, newValue);
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseOwnerIdChanged()
		{
			var dp = controller.RoomVm;
			string oldValue = "owner1";
			string newValue = "owner2";
			dp.OwnerIdChanged += (s, e) =>
			{
				eventRaised = true;
				Assert.AreSame(oldValue, e.OldValue, "e.OldValue did not match the expected room.");
				Assert.AreSame(newValue, e.NewValue, "e.NewValue did not match the expected room.");
			};
			dp.RaiseOwnerIdChanged(oldValue, newValue);
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseRoomNameChanged()
		{
			var dp = controller.RoomVm;
			string oldValue = "room1";
			string newValue = "room2";
			dp.RoomNameChanged += (s, e) =>
			{
				eventRaised = true;
				Assert.AreSame(oldValue, e.OldValue, "e.OldValue did not match the expected room.");
				Assert.AreSame(newValue, e.NewValue, "e.NewValue did not match the expected room.");
			};
			dp.RaiseRoomNameChanged(oldValue, newValue);
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseRoomChanged()
		{
			var dp = controller.RoomVm;
			var oldValue = new Room();
			var newValue = new Room();
			dp.RoomChanged += (s, e) =>
			{
				eventRaised = true;
				Assert.AreSame(oldValue, e.OldValue, "e.OldValue did not match the expected room.");
				Assert.AreSame(newValue, e.NewValue, "e.NewValue did not match the expected room.");
			};
			dp.RaiseRoomChanged(oldValue, newValue);
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseDesktopShared()
		{
			var dp = controller.RoomVm;
			var newValue = new Session();
			dp.DesktopShared += (s, e) =>
			{
				eventRaised = true;
				Assert.AreSame(newValue, e.ObjectAdded, "e.ObjectAdded did not match the expected value.");
			};
			dp.RaiseDesktopShared(newValue);
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseDesktopUnshared()
		{
			var dp = controller.RoomVm;
			var newValue = new Session();
			dp.DesktopUnshared += (s, e) =>
			{
				eventRaised = true;
				Assert.AreSame(newValue, e.ObjectRemoved, "e.ObjectAdded did not match the expected value.");
			};
			dp.RaiseDesktopUnshared(newValue);
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseLoginCompleted()
		{
			var dp = controller.RoomVm;
			var ex = new Exception();
			dp.LoginCompleted += (s, e) =>
				{
					eventRaised = true;
					Assert.AreSame(ex, e.Error);
				};
			dp.RaiseLoginCompleted(ex);
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseAuthenticationCompleted()
		{
			var dp = controller.RoomVm;
			var user = new User();
			var ex = new Exception();
			dp.AuthenticationCompleted += (s, e) =>
			{
				eventRaised = true;
				Assert.AreSame(ex, e.Error);
				Assert.AreEqual(user, e.UserState);
			};
			dp.RaiseAuthenticationCompleted(ex, user);
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		public void RaiseRoomJoined()
		{
			var dp = controller.RoomVm;
			dp.RoomJoined += (s, e) =>
			{
				eventRaised = true;
			};
			dp.RaiseRoomJoined();
			Assert.IsTrue(eventRaised, "The event handler did not fire.");
		}

		[TestMethod]
		[Tag("datapublisher")]
		[Asynchronous]
		public void RaiseDataProcessingExceptionOccurred()
		{
			var exception = new Exception();
			string message = Guid.NewGuid().ToString();
			EventHandler<ExceptionEventArgs> handler = null;
			handler = (s, e) =>
			{
				DataPublisher.DataProcessingExceptionOccurred -= handler;
				Assert.AreSame(exception, e.Exception);
				Assert.AreEqual(message, e.Message);
				eventRaised = true;
			};
			DataPublisher.DataProcessingExceptionOccurred += handler;
			DataPublisher.RaiseDataProcessExceptionOccurred(this, exception, message);
			EnqueueConditional(() => eventRaised);
			EnqueueTestComplete();
		}

	}
}
