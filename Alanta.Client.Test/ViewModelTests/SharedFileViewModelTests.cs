using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.Data.Uploader;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Alanta.Client.UI.Common.SampleData;
using Moq;
using System.Linq;

namespace Alanta.Client.Test.ViewModelTests
{
    [TestClass]
    public class SharedFileViewModelTests : ViewModelTestBase
    {
        #region SharedFileViewModel Tests
        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SampleSharedFileViewModelTest()
        {
            var sampleVm = new SampleSharedFileViewModel();
            Assert.AreEqual(sampleVm.OwnerUserId, DesignTimeHelper.GetRegisteredUser().UserId);
            Assert.AreEqual(sampleVm.OwnerUserName, DesignTimeHelper.GetRegisteredUser().UserName);
        }

        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SampleUserSharedFileCollectionViewModelTest()
        {
            var sampleVm = new SampleUserSharedFileCollectionViewModel();
            Assert.AreEqual(DesignTimeHelper.GetRegisteredUser(), sampleVm.UserVm.Model);
            Assert.AreEqual(DesignTimeHelper.GetRegisteredUser().UserId, sampleVm.UserId);
        }

        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void FileUploadControllerPropertyTest()
        {
            string fileName = "text.txt";
            var uploader = new FileUploadController(null, TestGlobals.User, TestGlobals.NamedRoom);
            Assert.AreEqual(null, uploader.FileInfo);
            Assert.IsTrue(string.IsNullOrEmpty(uploader.FileName));
            Assert.AreEqual(false, uploader.IsUploading);
            Assert.AreEqual(false, uploader.IsDeleted);

            uploader.FileName = fileName;
            Assert.AreEqual(fileName, uploader.FileName);

            uploader.UploadState = UploadState.Uploading;
            Assert.AreEqual(true, uploader.IsUploading);
            Assert.AreEqual(false, uploader.IsDeleted);

            uploader.UploadState = UploadState.Deleted;
            Assert.AreEqual(true, uploader.IsDeleted);
            Assert.AreEqual(false, uploader.IsUploading);
        }

		[TestMethod]
		[Tag("viewmodel")]
		[Tag("sharedfileviewmodel")]
		public void IsSetFocusAvailableTest()
		{
			var sharedFileVm = viewModelFactory.GetViewModel<SharedFileViewModel>();
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
			Assert.IsFalse(sharedFileVm.IsSetFocusAvailable);
			roomVm.Model = room;
			Assert.IsFalse(sharedFileVm.IsSetFocusAvailable);
			localUserVm.Model = room.User;
			Assert.IsTrue(sharedFileVm.IsSetFocusAvailable);
		}
        #endregion

        #region SharedFileViewModel State Tests

        /// <summary>
        /// Should be in error, because the viewmodel has no model
        /// </summary>
        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SharedFileStatusTest_NoModel()
        {
            var sharedFileVm = new SharedFileViewModel();
            sharedFileVm.Initialize(viewModelFactory);
            Assert.AreEqual(SharedFileState.Error, sharedFileVm.State);
        }

        /// <summary>
        /// Should be in error, because the viewmodel has no FileUploadController
        /// </summary>
        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SharedFileStatusTest_NoFileUploadController()
        {
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid() };
            var sharedFileVm = new SharedFileViewModel();
            sharedFileVm.Initialize(viewModelFactory);
            sharedFileVm.Model = sharedFile;
            Assert.AreEqual(SharedFileState.Error, sharedFileVm.State);
        }

        /// <summary>
        /// State should be ReadyToUpload, because we haven't uploaded it yet.
        /// </summary>
        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SharedFileStatusTest_Pending()
        {
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid() };
            var sharedFileVm = new SharedFileViewModel();
            sharedFileVm.Initialize(viewModelFactory);
            sharedFileVm.Model = sharedFile;
            sharedFileVm.FileUploadController = new FileUploadController(null, null, null);
            sharedFileVm.FileUploadController.UploadState = UploadState.Pending;
            Assert.AreEqual(SharedFileState.ReadyToUpload, sharedFileVm.State);
        }

        /// <summary>
        /// State should be Uploading, because we're in the middle of an upload.
        /// </summary>
        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SharedFileStatusTest_Uploading()
        {
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid() };
            var sharedFileVm = new SharedFileViewModel();
            sharedFileVm.Initialize(viewModelFactory);
            sharedFileVm.Model = sharedFile;
            sharedFileVm.FileUploadController = new FileUploadController(null, null, null);
            sharedFileVm.FileUploadController.UploadState = UploadState.Uploading;
            Assert.AreEqual(SharedFileState.Uploading, sharedFileVm.State);
        }

        /// <summary>
        /// State should be Error, because the upload encountered an error.
        /// </summary>
        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SharedFileStatusTest_UploadError()
        {
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid() };
            var sharedFileVm = new SharedFileViewModel();
            sharedFileVm.Initialize(viewModelFactory);
            sharedFileVm.Model = sharedFile;
            sharedFileVm.FileUploadController = new FileUploadController(null, null, null);
            sharedFileVm.FileUploadController.UploadState = UploadState.Error;
            Assert.AreEqual(SharedFileState.Error, sharedFileVm.State);
        }

        /// <summary>
        /// State should be Converting, because we've finished uploading, but it doesn't yet have a converted URI.
        /// </summary>
        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SharedFileStatusTest_Uploaded()
        {
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid() };
            var sharedFileVm = new SharedFileViewModel();
            sharedFileVm.Initialize(viewModelFactory);
            sharedFileVm.Model = sharedFile;
            sharedFileVm.FileUploadController = new FileUploadController(null, null, null);
            sharedFileVm.FileUploadController.UploadState = UploadState.Finished;
            Assert.AreEqual(SharedFileState.Converting, sharedFileVm.State);
        }

        /// <summary>
        /// State should be ReadyToShare, because it's been converted, and hasn't yet been shared.
        /// </summary>
        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SharedFileStatusTest_ConvertedButNotShared()
        {
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid(), ConvertedFileLocation = "http://localhost" };
            var sharedFileVm = new SharedFileViewModel();
            sharedFileVm.Initialize(viewModelFactory);
            sharedFileVm.Model = sharedFile;
            Assert.AreEqual(SharedFileState.ReadyToShare, sharedFileVm.State);
        }

        /// <summary>
        /// State should be ReadyToStopSharing, because it's been converted, and has been shared.
        /// </summary>
        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void SharedFileStatusTest_ConvertedAndShared()
        {
            var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
            roomVm.Model = room;
            Assert.AreEqual(room.SharedFiles.Count, roomVm.SharedFileCollectionVm.ViewModels.Count);
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid(), ConvertedFileLocation = "http://localhost" };
            var sharedFileVm = viewModelFactory.GetViewModel<SharedFileViewModel>(vm => vm.Model.SharedFileId == sharedFile.SharedFileId);
            sharedFileVm.Model = sharedFile;
            roomVm.SharedFileCollectionVm.ViewModels.Add(sharedFileVm);
            Assert.AreEqual(SharedFileState.ReadyToStopSharing, sharedFileVm.State);
        }
        #endregion

        #region UserSharedFileCollectionViewModel Tests

        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void UserSharedFileCollectionViewModel_SelectFilesTest()
        {
            var sampleVm = new SampleUserSharedFileCollectionViewModel();
            int initialCount = sampleVm.ViewModels.Count;
            var mockFileUploadService = new Mock<IFileUploadService>();

            // Unfortunately we can't do a real test here, because Silverlight doesn't let us create FileInfo objects without a real prompt.
            mockFileUploadService
                .Setup(uploader => uploader.ShowFileSelectionPrompt(It.IsAny<OperationCallback<IEnumerable<FileInfo>>>()))
                .Callback((OperationCallback<IEnumerable<FileInfo>> callback) => callback(null, null));
            sampleVm.FileUploadService = mockFileUploadService.Object;
            sampleVm.SelectFiles();
            mockFileUploadService.Verify(uploader => uploader.ShowFileSelectionPrompt(It.IsAny<OperationCallback<IEnumerable<FileInfo>>>()), Times.Once());
            Assert.AreEqual(initialCount, sampleVm.ViewModels.Count);
        }

        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void UserSharedFileCollectionViewModel_ProcessSelectedFileTest_AddNewFile()
        {
            var sampleVm = new SampleUserSharedFileCollectionViewModel();
            int initialCount = sampleVm.ViewModels.Count;
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid(), OriginalFileName = Guid.NewGuid().ToString() };
            var sharedFileVm = viewModelFactory.GetViewModel<SharedFileViewModel>(vm => vm.Model.SharedFileId == sharedFile.SharedFileId);
            sharedFileVm.Model = sharedFile;
            sampleVm.ProcessSelectedFile(sharedFileVm);
            Assert.AreEqual(initialCount + 1, sampleVm.ViewModels.Count);
            Assert.IsTrue(sampleVm.ViewModels.Contains(sharedFileVm));
        }

        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void UserSharedFileCollectionViewModel_ProcessSelectedFileTest_ReplaceExistingFile()
        {
            var mockFileUploadService = new Mock<IFileUploadService>();
            mockFileUploadService
                .Setup(uploader => uploader.ShowFileReplacementPrompt(
                    It.IsAny<IEnumerable<SharedFileViewModel>>(),
                    It.IsAny<SharedFileViewModel>(),
                    It.IsAny<OperationCallback<SharedFileReplaceCommands>>()))
                .Callback((IEnumerable<SharedFileViewModel> sharedFileViewModels, SharedFileViewModel sharedFileViewModel, OperationCallback<SharedFileReplaceCommands> callback) =>
                          callback(null, SharedFileReplaceCommands.Replace));
            var sampleVm = new SampleUserSharedFileCollectionViewModel();
            sampleVm.FileUploadService = mockFileUploadService.Object;
            int initialCount = sampleVm.ViewModels.Count;
            var oldSharedFileVm = sampleVm.ViewModels.First();
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid(), OriginalFileName = oldSharedFileVm.Model.OriginalFileName };
            var newSharedFileVm = viewModelFactory.GetViewModel<SharedFileViewModel>(vm => vm.Model.SharedFileId == sharedFile.SharedFileId);
            newSharedFileVm.Model = sharedFile;
            sampleVm.ProcessSelectedFile(newSharedFileVm);
            Assert.AreEqual(initialCount, sampleVm.ViewModels.Count);
            Assert.IsTrue(sampleVm.ViewModels.Contains(newSharedFileVm));
            Assert.IsFalse(sampleVm.ViewModels.Contains(oldSharedFileVm));
        }

        [TestMethod]
        [Tag("sharedfile")]
        [Tag("viewmodel")]
        [Tag("sharedfileviewmodel")]
        public void UserSharedFileCollectionViewModel_ProcessSelectedFileTest_AddAsNewFile()
        {
            var mockFileUploadService = new Mock<IFileUploadService>();
            mockFileUploadService
                .Setup(uploader => uploader.ShowFileReplacementPrompt(
                    It.IsAny<IEnumerable<SharedFileViewModel>>(),
                    It.IsAny<SharedFileViewModel>(),
                    It.IsAny<OperationCallback<SharedFileReplaceCommands>>()))
                .Callback((IEnumerable<SharedFileViewModel> sharedFileViewModels, SharedFileViewModel sharedFileViewModel, OperationCallback<SharedFileReplaceCommands> callback) =>
                {
                    sharedFileViewModel.Model.OriginalFileName += "_copy";
                    callback(null, SharedFileReplaceCommands.SaveAs);
                });
            var sampleVm = new SampleUserSharedFileCollectionViewModel();
            sampleVm.FileUploadService = mockFileUploadService.Object;
            int initialCount = sampleVm.ViewModels.Count;
            var oldSharedFileVm = sampleVm.ViewModels.First();
            var sharedFile = new SharedFile() { SharedFileId = Guid.NewGuid(), OriginalFileName = oldSharedFileVm.Model.OriginalFileName };
            var newSharedFileVm = viewModelFactory.GetViewModel<SharedFileViewModel>(vm => vm.Model.SharedFileId == sharedFile.SharedFileId);
            newSharedFileVm.Model = sharedFile;
            sampleVm.ProcessSelectedFile(newSharedFileVm);
            Assert.AreEqual(initialCount + 1, sampleVm.ViewModels.Count);
            Assert.IsTrue(sampleVm.ViewModels.Contains(newSharedFileVm));
            Assert.IsTrue(sampleVm.ViewModels.Contains(oldSharedFileVm));
        }

        #endregion

    }
}
