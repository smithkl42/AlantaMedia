using System;
using System.Collections.ObjectModel;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class RoomObjectTests : DataTestBase
	{
		/// <summary>
		/// Tests the chat message infrastructure from stem to stern, including a call to the web service.
		/// </summary>
		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		[Tag("chat")]
		public void SendChatMessageTest()
		{
			bool messageReceived = false;
			string message = Guid.NewGuid().ToString();
			var chatMessageCollectionViewModel = _viewModelFactory.GetViewModel<ChatMessageCollectionViewModel>();

			JoinRoom(error =>
				{
					chatMessageCollectionViewModel.ViewModels.CollectionChanged += (s, e) =>
					{
						var vm = (ChatMessageViewModel)e.NewItems[0];
						Assert.AreEqual(message, vm.Model.Text);
						messageReceived = true;
					};

					chatMessageCollectionViewModel.SendChatMessageCommand.Execute(message);
				});
			EnqueueConditional(() => messageReceived);
			EnqueueTestComplete();
		}

		/// <summary>
		/// Confirms that adding a ChatMessage to an observable collection results in the correct 
		/// ChatMessageViewModel being created and added to the right collection.
		/// </summary>
		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		[Tag("chat")]
		public void ChatMessageReceivedTest()
		{
			bool chatMessageReceived = false;
			var chatMessages = new ObservableCollection<ChatMessage>();
			var chatMessageCollectionViewModel = _viewModelFactory.GetViewModel<ChatMessageCollectionViewModel>();
			chatMessageCollectionViewModel.Models = chatMessages;

			var chatMessage = new ChatMessage
			{
				Date = DateTime.Now,
				Text = Guid.NewGuid().ToString(),
				UserName = TestGlobals.OwnerUserName
			};

			chatMessageCollectionViewModel.ViewModels.CollectionChanged += (s, e) =>
				{
					var vm = (ChatMessageViewModel)e.NewItems[0];
					Assert.AreEqual(chatMessage, vm.Model);
					chatMessageReceived = true;
				};

			chatMessages.Add(chatMessage);

			EnqueueConditional(() => chatMessageReceived);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		[Tag("sharedfile")]
		public void CreateSharedFileTest()
		{
			bool finished = false;
			ExecuteTestFramework(error =>
			{
				var sharedFile = GetSharedFile();

				_roomService.CreateSharedFile(sharedFile, (error2, createdFile) => Try(() =>
				{
					Assert.IsNull(error2);
					Assert.AreEqual(sharedFile.SharedFileId, createdFile.SharedFileId);
					Assert.AreEqual(sharedFile.UserId, createdFile.User.UserId);
					Assert.AreEqual(sharedFile.OriginalFileLocation, createdFile.OriginalFileLocation);
					finished = true;
				}));
			});
			EnqueueConditional(() => finished);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		[Tag("sharedfile")]
		public void RegisterSharedFileTest()
		{
			bool finished = false;
			ExecuteTestFramework(error =>
			{
				var sharedFile = GetSharedFile();

				_roomService.CreateSharedFile(sharedFile, (createError, createdFile) => _roomService.RegisterSharedFile(createdFile, _roomVm.Model.RoomId, Assert.IsNull));

				_roomService.SharedFileAdded += (s, e) =>
					{
						Assert.AreEqual(sharedFile.SharedFileId, e.Value.SharedFileId);
						finished = true;
					};

			});
			EnqueueConditional(() => finished);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		[Tag("sharedfile")]
		public void UnregisterSharedFileTest()
		{
			bool finished = false;
			ExecuteTestFramework(error =>
			{
				var sharedFile = GetSharedFile();

				_roomService.SharedFileAdded += (s, e) => _roomService.UnregisterSharedFile(sharedFile, _roomVm.SessionId, Assert.IsNull);

				_roomService.SharedFileRemoved += (s, e) =>
					{
						Assert.AreEqual(sharedFile.SharedFileId, e.Value);
						finished = true;
					};

				_roomService.CreateSharedFile(sharedFile, (error2, createdFile) => _roomService.RegisterSharedFile(createdFile, _roomVm.Model.RoomId, Assert.IsNull));

			});
			EnqueueConditional(() => finished);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Tag("room")]
		[Tag("sharedfile")]
		public void DeleteSharedFileTest()
		{
			bool finished = false;
			ExecuteTestFramework(error =>
			{
				var sharedFile = GetSharedFile();

				_roomService.SharedFileAdded += (s, e) => _roomService.DeleteSharedFile(e.Value, _roomVm.SessionId, Assert.IsNull);

				_roomService.SharedFileDeleted += (s, e) =>
				{
					Assert.AreEqual(sharedFile.SharedFileId, e.Value);
					finished = true;
				};

				_roomService.SharedFileRemoved += (s, e) =>
				{
					Assert.AreEqual(sharedFile.SharedFileId, e.Value);
					finished = true;
				};

				_roomService.CreateSharedFile(sharedFile, (createError, createdFile) => _roomService.RegisterSharedFile(createdFile, _roomVm.Model.RoomId, Assert.IsNull));

			});
			EnqueueConditional(() => finished);
			EnqueueTestComplete();
		}

		private SharedFile GetSharedFile()
		{
			return new SharedFile
			{
				SharedFileId = Guid.NewGuid(),
				SharedFileTypeId = "Undefined",
				OriginalFileName = Guid.NewGuid().ToString(),
				OriginalFileLocation = @"c:\temp\" + Guid.NewGuid().ToString(),
				OriginalFileUploadedOn = DateTime.Now,
				UserId = _roomVm.UserVm.Model.UserId
			};
		}

	}
}
