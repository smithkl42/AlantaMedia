using System.Globalization;
using Microsoft.Silverlight.Testing.UnitTesting.Metadata.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Alanta.Client.Data.RoomService;
using Alanta.Client.Data.Uploader;
using Alanta.Client.UI.Desktop.RoomView.Contacts;
using Alanta.Client.UI.Desktop.Controls.FileManager;
using Alanta.Client.UI.Desktop.Resources;
using Microsoft.Silverlight.Testing;
using Alanta.Client.UI.Common.ViewModels;
using System.Collections.ObjectModel;
using System;
using Alanta.Client.UI.Common.Classes;

namespace Alanta.Client.Test
{
	[TestClass]
	public class ConverterTests
	{
		private ViewModelFactory viewModelFactory;
		private RoomViewModel roomViewModel;

		[TestInitialize]
		public void Initialize()
		{
			var roomService = new TestRoomServiceAdapter();
			roomService.CreateClient();
			var messageService = new TestMessageService();
			var viewLocator = new ViewLocator();
			viewModelFactory = new ViewModelFactory(roomService, messageService, viewLocator);
			roomViewModel = viewModelFactory.GetViewModel<RoomViewModel>();
		}

		[TestMethod]
		[Tag("converter")]
		public void ContactNameConverter_NormalName()
		{
			var converter = new ContactNameConverter();
			string actual = converter.Convert("Ken Smith", typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual("Ken", actual);
		}

		[TestMethod]
		[Tag("converter")]
		public void ContactNameConverter_OneName()
		{
			var converter = new ContactNameConverter();
			string actual = converter.Convert("Ken", typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual("Ken", actual);
		}

		[TestMethod]
		[Tag("converter")]
		public void ContactNameConverter_ThreeNames()
		{
			var converter = new ContactNameConverter();
			string actual = converter.Convert("Alpha Beta Gamma", typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual("Alpha", actual);
		}

		[TestMethod]
		[Tag("converter")]
		public void ContactNameConverter_LongFirstName()
		{
			var converter = new ContactNameConverter();
			string actual = converter.Convert("   SomeLongFirstName LastName   ", typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual("Some...", actual);
		}

		[TestMethod]
		[Tag("converter")]
		public void ContactNameConverter_LongLastName()
		{
			var converter = new ContactNameConverter();
			string actual = converter.Convert("SomeLongFirstName SomeLongLastName", typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual("Some...", actual);
		}

		[TestMethod]
		[Tag("converter")]
		public void ContactNameConverter_Empty()
		{
			var converter = new ContactNameConverter();
			string actual = converter.Convert(string.Empty, typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual(string.Empty, actual);
		}

		[TestMethod]
		[Tag("converter")]
		public void ContactNameConverter_Null()
		{
			var converter = new ContactNameConverter();
			string actual = converter.Convert(string.Empty, typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual(string.Empty, actual);
		}

		/// <summary>
		/// If the SharedFile has no upload command and no URL from which it can be retrieved, something has gone wrong, 
		/// and we're in an error state.
		/// </summary>
		[TestMethod]
		[Tag("converter")]
		public void SharedFileStateConverter_ConvertedFileNull_UploadCommandNull()
		{
			var sharedFile = new SharedFile();
			var converter = new SharedFileStateConverter();
			string actual = converter.Convert(sharedFile, typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual(ClientStrings.SharedFileActionError, actual);
		}

		/// <summary>
		/// If the SharedFile has no URL from which it can be retrieved, but does have an upload command, and the upload command state is "Uploading", 
		/// the appropriate option is to cancel the upload.
		/// </summary>
		[TestMethod]
		[Tag("converter")]
		public void SharedFileStateConverter_ConvertedFileNull_UploadCommandStateUploading()
		{
			var fileUploadController = new FileUploadController(null, null, null)
			{
				UploadState = UploadState.Uploading
			};
			var sharedFile = new SharedFileViewModel()
			{
				FileUploadController = fileUploadController
			};
			var converter = new SharedFileStateConverter();
			string actual = converter.Convert(sharedFile, typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual(ClientStrings.SharedFileActionCancel, actual);
		}

		/// <summary>
		/// If the SharedFile has no URL from which it can be retrieved but does have an upload command, 
		/// if the upload command state is anything other than "Uploading", 
		/// then the appropriate option is to upload it.
		/// </summary>
		[TestMethod]
		[Tag("converter")]
		public void SharedFileStateConverter_ConvertedFileNull_UploadCommandAnythingElse()
		{
			var fileUploadController = new FileUploadController(null, null, null)
			{
				UploadState = UploadState.Pending
			};
			var sharedFile = new SharedFileViewModel()
			{
				FileUploadController = fileUploadController
			};
			var converter = new SharedFileStateConverter();
			string actual = converter.Convert(sharedFile, typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual(ClientStrings.SharedFileActionUpload, actual);
		}

		/// <summary>
		/// If the SharedFile has a URL from which it can be retrieved and already exists in the current room, 
		/// the appropriate option is to stop sharing it.
		/// </summary>
		[TestMethod]
		[Tag("converter")]
		public void SharedFileStateConverter_ConvertedFileSet_AlreadyShared()
		{
			var user = new User() { UserId = Guid.NewGuid(), UserTag = TestConstants.Facebook1UserId };
			var roomSharedFileCollectionsViewModel = viewModelFactory.GetViewModel<RoomSharedFileCollectionViewModel>();
			var sharedFile = new SharedFile()
			{
				SharedFileId = Guid.NewGuid(),
				ConvertedFileLocation = "http://localhost/something",
				User = user
			};
			var sharedFileViewModel = viewModelFactory.GetViewModel<SharedFileViewModel>(vm => vm.Model.SharedFileId == sharedFile.SharedFileId);
			sharedFileViewModel.Model = sharedFile;
			roomSharedFileCollectionsViewModel.ViewModels.Add(sharedFileViewModel);
			var converter = new SharedFileStateConverter()
			{
				SharedFileViewModels = roomSharedFileCollectionsViewModel.ViewModels
			};
			string actual = converter.Convert(sharedFileViewModel, typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual(ClientStrings.SharedFileActionRemove, actual);
		}

		/// <summary>
		/// If the SharedFile has a URL from which it can be retrieved but not yet exist in the current room, 
		/// the appropriate option is to start sharing it.
		/// </summary>
		[TestMethod]
		[Tag("converter")]
		public void SharedFileStateConverter_ConvertedFileSet_NotShared()
		{
			var user = new User() { UserId = Guid.NewGuid(), UserTag = TestConstants.Facebook1UserId };
			var sharedFile = new SharedFile()
			{
				SharedFileId = Guid.NewGuid(),
				ConvertedFileLocation = "http://localhost/something",
				User = user
			};
			var sharedFileViewModel = viewModelFactory.GetViewModel<SharedFileViewModel>(vm => vm.Model.SharedFileId == sharedFile.SharedFileId);
			sharedFileViewModel.Model = sharedFile;
			var converter = new SharedFileStateConverter()
			{
				SharedFileViewModels = roomViewModel.SharedFileCollectionVm.ViewModels
			};
			string actual = converter.Convert(sharedFileViewModel, typeof(string), null, CultureInfo.CurrentCulture) as string;
			Assert.AreEqual(ClientStrings.SharedFileActionShare, actual);
		}
	}
}
