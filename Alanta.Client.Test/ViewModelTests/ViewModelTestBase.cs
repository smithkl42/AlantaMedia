using Alanta.Client.Common;
using Alanta.Client.Common.Loader;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.SampleData;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class ViewModelTestBase : SilverlightTest
	{
		protected Mock<IRoomServiceAdapter> mockRoomService;
		protected IRoomServiceAdapter roomService;
		protected Mock<IMessageService> mockMessageService;
		protected IMessageService messageService;
		protected ViewLocator viewLocator;
		protected ViewModelFactory viewModelFactory;

		protected GuestUser guestUser;
		protected RegisteredUser user;
		protected Room room;

		[Asynchronous]
		[TestInitialize]
		public virtual void TestInit()
		{
			DesignTimeHelper.Reset();
			mockMessageService = GetMockMessageService();
			messageService = mockMessageService.Object;
			viewLocator = new ViewLocator();
			mockRoomService = GetMockRoomService();
			roomService = mockRoomService.Object;
			viewModelFactory = new ViewModelFactory(roomService, messageService, viewLocator);

			user = DesignTimeHelper.GetRegisteredUser();
			room = DesignTimeHelper.GetRoom();
			guestUser = DesignTimeHelper.GetGuestUser();

			// Wait to kick off any of the actual tests until the basic test initialization has completed.
			EnqueueConditional(() => TestGlobals.Initialized);
			EnqueueTestComplete();
		}

		protected virtual Mock<IRoomServiceAdapter> GetMockRoomService()
		{
			var mocker = new Mock<IRoomServiceAdapter>();
			return mocker;
		}

		protected virtual Mock<IMessageService> GetMockMessageService()
		{
			var mocker = new Mock<IMessageService>();
			return mocker;
		}

		protected WhiteboardViewModel GetNewWhiteboardVm()
		{
			var whiteboard = new Data.RoomService.Whiteboard() { WhiteboardId = Guid.NewGuid() };
			var vm = viewModelFactory.GetViewModel<WhiteboardViewModel>(wvm => wvm.Model.WhiteboardId == whiteboard.WhiteboardId);
			vm.Model = whiteboard;
			return vm;
		}

		protected SharedFileViewModel GetNewSharedFile()
		{
			var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid() };
			var vm = viewModelFactory.GetViewModel<SharedFileViewModel>(sfvm => sfvm.Model.SharedFileId == sharedFile.SharedFileId);
			vm.Model = sharedFile;
			return vm;
		}

	}
}
