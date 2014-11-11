using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.SampleData;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class ChatMessageViewModelTests : ViewModelTestBase
	{
		[Tag("viewmodel")]
		[Tag("chat")]
		[Tag("chatmessageviewmodel")]
		[TestMethod]
		public void SampleChatMessageCollectionViewModelTest()
		{
			var vm = new SampleChatMessageCollectionViewModel();
			Assert.AreEqual(room.ChatMessages.Count, vm.ViewModels.Count);
		}

		[Tag("viewmodel")]
		[Tag("chat")]
		[Tag("chatmessageviewmodel")]
		[TestMethod]
		public void FormattingServiceTest()
		{
			var vm = new SampleChatMessageCollectionViewModel();
			var initialCount = vm.ViewModels.Count;
			var mockFormatter = new Mock<ChatFormattingServiceBase>(vm.ViewModels, vm.UserVm);
			mockFormatter.Setup(f => f.AddMessage(It.IsAny<ChatMessageViewModel>()));
			vm.ChatMessageFormatters.Add(mockFormatter.Object);
			var msg = DesignTimeHelper.GetChatMessage(user);
			vm.AddChatMessage(msg);
			mockFormatter.Verify(f => f.AddMessage(It.IsAny<ChatMessageViewModel>()), Times.Exactly(initialCount + 1));
		}

		[Tag("viewmodel")]
		[Tag("chat")]
		[Tag("chatmessageviewmodel")]
		[TestMethod]
		public void AddChatMessageTest()
		{
			var vm = new SampleChatMessageCollectionViewModel();
			var initialCount = vm.ViewModels.Count;
			var msg = DesignTimeHelper.GetChatMessage(user);
			vm.AddChatMessage(msg);
			var msgVm = vm.ViewModels.First(x => x.Model == msg);
			Assert.AreEqual(initialCount + 1, vm.ViewModels.Count);
			Assert.IsNotNull(msgVm);
			Assert.AreEqual(vm.ViewModels.Count - 1, msgVm.Index);
		}

		[Tag("viewmodel")]
		[Tag("chat")]
		[Tag("chatmessageviewmodel")]
		[TestMethod]
		public void AddRoomNotificationTest()
		{
			var vm = new SampleChatMessageCollectionViewModel();
			var initialCount = vm.ViewModels.Count;
			vm.AddRoomNotification("Test");
			var msgVm = vm.ViewModels.First(x => x.Model.Text == "Test");
			Assert.AreEqual(initialCount + 1, vm.ViewModels.Count);
			Assert.IsNotNull(msgVm);
			Assert.AreEqual(vm.ViewModels.Count - 1, msgVm.Index);
		}

		[Tag("viewmodel")]
		[Tag("chat")]
		[Tag("chatmessageviewmodel")]
		[TestMethod]
		public void FormatChatMessageTest()
		{
			var vm = new SampleChatMessageCollectionViewModel();
			var formatter = new TestChatFormattingService(vm.ViewModels, vm.UserVm);
			int initialCount = formatter.FormattedMessages.Blocks.Count;
			Assert.AreEqual(vm.ViewModels.Count, initialCount);
			bool getParagraphsFromChatMessageCalled = false;
			formatter.GetParagraphsFromChatMessageCalled += (s, e) => getParagraphsFromChatMessageCalled = true;
			var chatVm = new ChatMessageViewModel {Model = DesignTimeHelper.GetChatMessage(user)};
			formatter.AddMessage(chatVm);
			Assert.IsTrue(getParagraphsFromChatMessageCalled);
			Assert.AreEqual(initialCount + 1, formatter.FormattedMessages.Blocks.Count);
		}

		[Tag("viewmodel")]
		[Tag("chat")]
		[Tag("chatmessageviewmodel")]
		[TestMethod]
		public void FormatNotificationTest()
		{
			var vm = new SampleChatMessageCollectionViewModel();
			var formatter = new TestChatFormattingService(vm.ViewModels, vm.UserVm);
			int initialCount = formatter.FormattedMessages.Blocks.Count;
			Assert.AreEqual(vm.ViewModels.Count, initialCount);
			bool getParagraphsFromNotificationCalled = false;
			formatter.GetParagraphsFromNotificationCalled += (s, e) => getParagraphsFromNotificationCalled = true;
			var chatVm = new ChatMessageViewModel { Model = new RoomNotification(DateTime.Now, "Test") };
			formatter.AddMessage(chatVm);
			Assert.IsTrue(getParagraphsFromNotificationCalled);
			Assert.AreEqual(initialCount + 1, formatter.FormattedMessages.Blocks.Count);
		}

		[Tag("viewmodel")]
		[Tag("chat")]
		[Tag("chatmessageviewmodel")]
		[TestMethod]
		public void DeleteAllMessagesVisibilityTest()
		{
			var chatVm = viewModelFactory.GetViewModel<ChatMessageCollectionViewModel>();
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			Assert.AreEqual(Visibility.Collapsed, chatVm.DeleteAllMessagesVisibility);
			roomVm.Model = room;
			Assert.AreEqual(Visibility.Collapsed, chatVm.DeleteAllMessagesVisibility);
			localUserVm.Model = room.User;
			Assert.AreEqual(Visibility.Visible, chatVm.DeleteAllMessagesVisibility);
		}

	}

	public class TestChatFormattingService : ChatFormattingServiceBase
	{
		public TestChatFormattingService(ObservableCollection<ChatMessageViewModel> chatMessages, UserViewModel userViewModel)
			: base(chatMessages, userViewModel)
		{
		}

		public event EventHandler GetParagraphsFromNotificationCalled;
		public event EventHandler GetParagraphsFromChatMessageCalled;

		protected override IEnumerable<Paragraph> GetParagraphsFromNotification(RoomNotification roomNotification)
		{
			var paragraphs = new List<Paragraph>();
			var p = new Paragraph();
			p.Inlines.Add(new Run { Text = roomNotification.Text });
			paragraphs.Add(p);
			if (GetParagraphsFromNotificationCalled != null) GetParagraphsFromNotificationCalled(this, new EventArgs());
			return paragraphs;
		}

		protected override IEnumerable<Paragraph> GetParagraphsFromChatMessage(ChatMessage chatMessage, ChatMessage previousMessage)
		{
			var paragraphs = new List<Paragraph>();
			var p = new Paragraph();
			p.Inlines.Add(new Run { Text = chatMessage.Text });
			paragraphs.Add(p);
			if (GetParagraphsFromChatMessageCalled != null) GetParagraphsFromChatMessageCalled(this, new EventArgs());
			return paragraphs;
		}
	}
}
