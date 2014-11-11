using System;
using System.Windows;
using Alanta.Client.Common;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.Resources;
using Alanta.Client.UI.Common.SharedDesktop;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class SharedDesktopServerViewModelTests : ViewModelTestBase
	{
		private SharedDesktopServerViewModel desktopVm;
		private RoomViewModel roomVm;
		private Session session;
		private Mock<IVncServer> mockVnc;
		private Mock<IVncInstaller> mockInstaller;
		// private ErrorCollectionViewModel errors;

		[TestInitialize]
		[Asynchronous]
		public override void TestInit()
		{
			base.TestInit();
			desktopVm = viewModelFactory.GetViewModel<SharedDesktopServerViewModel>();
			roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			// errors = viewModelFactory.GetViewModel<ErrorCollectionViewModel>();
			mockVnc = new Mock<IVncServer>();
			session = new Session { SessionId = Guid.NewGuid(), Room = room, User = user, IsActive = true };
			roomVm.SessionId = session.SessionId;
			mockVnc.SetupAllProperties();
			mockVnc.Setup(v => v.IsBrowserSupported()).Returns(true);
			mockVnc.Setup(v => v.InitializeVnc()).Returns(VncInitializationStatus.Initialized);
			mockInstaller = new Mock<IVncInstaller>();
			desktopVm.VncServer = mockVnc.Object;
			desktopVm.VncInstaller = mockInstaller.Object;
		}

		public override void TestComplete()
		{
			desktopVm.StopReinitializeTimer();
			base.TestComplete();
		}

		#region State Tests

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void StateTest_Progressing()
		{
			desktopVm.State = SharedDesktopState.Progressing;
			Assert.AreEqual(true, desktopVm.IsProgressing);
			Assert.AreEqual(Visibility.Visible, desktopVm.ProgressVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.InstallVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ErrorVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ControlVisibility);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void StateTest_Error()
		{
			desktopVm.State = SharedDesktopState.Error;
			Assert.AreEqual(false, desktopVm.IsProgressing);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ProgressVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.InstallVisibility);
			Assert.AreEqual(Visibility.Visible, desktopVm.ErrorVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ControlVisibility);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void StateTest_Install()
		{
			desktopVm.State = SharedDesktopState.InstallRequired;
			Assert.AreEqual(false, desktopVm.IsProgressing);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ProgressVisibility);
			Assert.AreEqual(Visibility.Visible, desktopVm.InstallVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ErrorVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ControlVisibility);
			Assert.AreEqual(CommonStrings.SharedDesktop_InstallMessage, desktopVm.InstallMessage);
			Assert.AreEqual(CommonStrings.SharedDesktop_InstallLink, desktopVm.InstallContent);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void StateTest_UpdateRequired()
		{
			desktopVm.State = SharedDesktopState.UpdateRequired;
			Assert.AreEqual(false, desktopVm.IsProgressing);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ProgressVisibility);
			Assert.AreEqual(Visibility.Visible, desktopVm.InstallVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ErrorVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ControlVisibility);
			Assert.AreEqual(string.Format(CommonStrings.SharedDesktop_UpdateMsgFormat, desktopVm.InstalledVncVersion, desktopVm.LatestVncVersion), desktopVm.InstallMessage);
			Assert.AreEqual(CommonStrings.SharedDesktop_UpdateLink, desktopVm.InstallContent);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void StateTest_Ready()
		{
			desktopVm.State = SharedDesktopState.Ready;
			Assert.AreEqual(false, desktopVm.IsProgressing);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ProgressVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.InstallVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ErrorVisibility);
			Assert.AreEqual(Visibility.Visible, desktopVm.ControlVisibility);
			Assert.IsTrue(desktopVm.IsStartAvailable);
			Assert.IsFalse(desktopVm.IsStopAvailable);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void StateTest_Sharing()
		{
			desktopVm.State = SharedDesktopState.Sharing;
			Assert.AreEqual(false, desktopVm.IsProgressing);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ProgressVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.InstallVisibility);
			Assert.AreEqual(Visibility.Collapsed, desktopVm.ErrorVisibility);
			Assert.AreEqual(Visibility.Visible, desktopVm.ControlVisibility);
			Assert.IsFalse(desktopVm.IsStartAvailable);
			Assert.IsTrue(desktopVm.IsStopAvailable);
		}

		#endregion

		#region VncServer Property Tests

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void InitializeTest()
		{
			// The desktopVm is already initialized by this point.
			Assert.IsNotNull(desktopVm.ShareCommand);
			Assert.IsNotNull(desktopVm.ShareNewItemCommand);
			Assert.IsNotNull(desktopVm.CreateItemCommand);
			Assert.IsNotNull(desktopVm.UnshareCommand);
			Assert.IsNotNull(desktopVm.DownloadVncCommand);
			Assert.IsNotNull(desktopVm.DeleteCommand);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void VncServerInitializationTest_Initialized()
		{
			Assert.AreEqual(SharedDesktopState.Progressing, desktopVm.State);
			var eventArgs = new EventArgs();
			mockVnc.Setup(v => v.InitializeVnc()).Returns(() => VncInitializationStatus.Initialized);
			desktopVm.InitializeVnc();
			mockInstaller.Verify(i => i.CheckForRecommendedVersion(It.Is<IVncServer>(v => v == mockVnc.Object)), Times.Once());
			mockMessageService.Verify(ms => ms.ShowErrorMessage(It.IsAny<string>()), Times.Never());
			// Assert.AreEqual(0, errors.ViewModels.Count);
			Assert.AreEqual(SharedDesktopState.Ready, desktopVm.State);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void VncServerInitializationTest_NotInstalled()
		{
			Assert.AreEqual(SharedDesktopState.Progressing, desktopVm.State);
			mockVnc.Setup(v => v.InitializeVnc()).Returns(() => VncInitializationStatus.NotInstalled);
			desktopVm.InitializeVnc();
			mockMessageService.Verify(ms => ms.ShowErrorMessage(It.IsAny<string>()), Times.Never());
			// Assert.AreEqual(0, errors.ViewModels.Count);
			Assert.AreEqual(SharedDesktopState.InstallRequired, desktopVm.State);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void VncServerInitializationTest_UpdateRequired()
		{
			Assert.AreEqual(SharedDesktopState.Progressing, desktopVm.State);
			mockVnc.Setup(v => v.InitializeVnc()).Returns(() => VncInitializationStatus.Obsolete);
			desktopVm.InitializeVnc();
			mockMessageService.Verify(ms => ms.ShowErrorMessage(It.IsAny<string>()), Times.Never());
			// Assert.AreEqual(0, errors.ViewModels.Count);
			Assert.AreEqual(SharedDesktopState.UpdateRequired, desktopVm.State);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void VncServerInitializationTest_AnotherInstanceRunning()
		{
			Assert.AreEqual(SharedDesktopState.Progressing, desktopVm.State);
			mockVnc.Setup(v => v.InitializeVnc()).Returns(() => VncInitializationStatus.AnotherInstanceRunning);
			desktopVm.InitializeVnc();
			mockMessageService.Verify(ms => ms.ShowErrorMessage(It.IsAny<string>()), Times.Never());
			// Assert.AreEqual(0, errors.ViewModels.Count);
			Assert.AreEqual(SharedDesktopState.Error, desktopVm.State);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void VncServerInitializationTest_PageError()
		{
			Assert.AreEqual(SharedDesktopState.Progressing, desktopVm.State);
			var eventArgs = new EventArgs<string>("Some error message");
			mockVnc.Raise(v => v.PageError += null, eventArgs);
			mockMessageService.Verify(ms => ms.ShowErrorMessage(It.IsAny<string>()), Times.Never());
			// Assert.AreEqual(0, errors.ViewModels.Count);
			Assert.AreEqual(SharedDesktopState.Error, desktopVm.State);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void VncServerInitializationTest_VncError()
		{
			Assert.AreEqual(SharedDesktopState.Progressing, desktopVm.State);
			var eventArgs = new VncErrorEventArgs(1111, "Some error message");
			desktopVm.InitializeVnc();
			mockVnc.Raise(v => v.VncError += null, eventArgs);
			mockMessageService.Verify(ms => ms.ShowErrorMessage(It.IsAny<string>()), Times.Once());
			Assert.AreEqual(SharedDesktopState.Ready, desktopVm.State);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void VncServerInitializationTest_AddClientByDesktopId_AnotherInstance()
		{
			Assert.AreEqual(SharedDesktopState.Progressing, desktopVm.State);
			mockRoomService
				.Setup(rs => rs.CreateSharedDesktop(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OperationCallback<Session>>()))
				.Callback((Guid sessionId, string sharedDesktopHost, string password, OperationCallback<Session> callback) => callback(null, session));
			mockVnc.Setup(v => v.AddClientByDesktopId(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
				.Throws(new Exception("some exception from VNC"));
			mockVnc.SetupGet(v => v.IsAnotherInstanceRunning).Returns(true);

			desktopVm.InitializeVnc();
			desktopVm.CreateItemCommand.Execute(null);

			Assert.AreEqual(SharedDesktopState.Error, desktopVm.State);
			Assert.AreEqual(CommonStrings.DesktopSharing_AnotherInstanceRunned, desktopVm.ErrorMessage);
		}

		#endregion

		#region Create Item Command Tests

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void CreateItemCommandTest_NoError()
		{
			// Arrange
			mockRoomService
				.Setup(rs => rs.CreateSharedDesktop(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OperationCallback<Session>>()))
				.Callback((Guid sessionId, string sharedDesktopHost, string password, OperationCallback<Session> callback) => callback(null, session));

			// Act
			desktopVm.CreateItemCommand.Execute(null);

			// Assert
			Assert.AreEqual(session, desktopVm.Model);
			mockVnc.Verify(v => v.AddClientByDesktopId(
									It.Is<string>(host => host == DataGlobals.SharedDesktopHost),
									It.Is<int>(port => port == DataGlobals.SharedDesktopServerPort),
									It.Is<string>(s => s == session.SessionId.ToString())),
						   Times.Once());
			Assert.AreEqual(SharedDesktopState.Sharing, desktopVm.State);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void CreateItemCommandTest_CreateSharedDesktopError()
		{
			// Arrange
			var sharingException = new Exception("Error message");
			mockRoomService
				.Setup(rs => rs.CreateSharedDesktop(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OperationCallback<Session>>()))
				.Callback((Guid sessionId, string sharedDesktopHost, string password, OperationCallback<Session> callback) => callback(sharingException, session));

			// Act
			desktopVm.CreateItemCommand.Execute(null);

			// Assert
			Assert.AreNotEqual(session, desktopVm.Model);
			Assert.AreEqual(SharedDesktopState.Ready, desktopVm.State);
			// Assert.AreEqual(1, errors.ViewModels.Count);
			// Assert.IsTrue(errors.ViewModels.Any(vm => vm.Error == sharingException));
			mockMessageService.Verify(ms =>
				ms.ShowErrorMessage(It.Is<string>(m =>
					m == string.Format(CommonStrings.SharedDesktop_FailedToShare, sharingException.Message))), Times.Once());
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void CreateItemCommandTest_AddClientByDesktopIdError()
		{
			// Arrange
			var addClientByDesktopIdException = new Exception("Error message");
			mockRoomService
				.Setup(rs => rs.CreateSharedDesktop(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OperationCallback<Session>>()))
				.Callback((Guid sessionId, string sharedDesktopHost, string password, OperationCallback<Session> callback) => callback(null, session));
			mockVnc
				.Setup(v => v.AddClientByDesktopId(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
				.Throws(addClientByDesktopIdException);

			// Act
			desktopVm.CreateItemCommand.Execute(null);

			// Assert
			Assert.AreEqual(session, desktopVm.Model);
			Assert.AreEqual(SharedDesktopState.Ready, desktopVm.State);
			// Assert.AreEqual(1, errors.ViewModels.Count);
			// Assert.IsTrue(errors.ViewModels.Any(vm => vm.Error == addClientByDesktopIdException));
			// ks 3/30/11 - The error actually gets displayed through a call to OnVncError().
			//mockMessageService.Verify(ms => 
			//    ms.ShowErrorMessage(It.Is<string>(m =>
			//        m == string.Format(CommonStrings.SharedDesktop_FailedToShare, addClientByDesktopIdException.Message))), Times.Once());
		}
		#endregion

		#region Delete Command Tests
		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void DeleteItemCommandTest_NoError()
		{
			// Arrange
			mockVnc.SetupGet(v => v.IsDesktopShared).Returns(true);
			mockRoomService
				.Setup(rs => rs.UnshareDesktop(It.IsAny<Guid>(), It.IsAny<OperationCallback<Session>>()))
				.Callback((Guid sessionId, OperationCallback<Session> callback) => callback(null, session));

			// Act
			desktopVm.DeleteCommand.Execute(null);

			// Assert
			mockVnc.VerifyGet(v => v.IsDesktopShared, Times.Once());
			mockRoomService.Verify(rs => rs.UnshareDesktop(It.IsAny<Guid>(), It.IsAny<OperationCallback<Session>>()), Times.Once());
			// Assert.AreEqual(0, errors.ViewModels.Count);
			Assert.AreEqual(SharedDesktopState.Ready, desktopVm.State);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void DeleteItemCommandTest_UnshareDesktopError()
		{
			// Arrange
			var unshareDesktopError = new Exception("Some error message");
			mockVnc.SetupGet(v => v.IsDesktopShared).Returns(true);
			mockRoomService
				.Setup(rs => rs.UnshareDesktop(It.IsAny<Guid>(), It.IsAny<OperationCallback<Session>>()))
				.Callback((Guid sessionId, OperationCallback<Session> callback) => callback(unshareDesktopError, session));

			// Act
			desktopVm.DeleteCommand.Execute(null);

			// Assert
			mockVnc.VerifyGet(v => v.IsDesktopShared, Times.Once());
			mockRoomService.Verify(rs => rs.UnshareDesktop(It.IsAny<Guid>(), It.IsAny<OperationCallback<Session>>()), Times.Once());
			mockMessageService.Verify(ms => ms.ShowErrorMessage(It.IsAny<string>()), Times.Once());
			// Assert.AreEqual(1, errors.ViewModels.Count);
			// Assert.IsTrue(errors.ViewModels.Any(vm => vm.Error == unshareDesktopError));
			Assert.AreEqual(SharedDesktopState.Ready, desktopVm.State);
		}

		#endregion

		#region Other Command Tests

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void ShareCommandTest()
		{
			Assert.IsFalse(desktopVm.IsShared);
			desktopVm.ShareCommand.Execute(null);
			Assert.IsTrue(desktopVm.IsShared);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void ShareNewItemCommandTest()
		{
			Assert.IsFalse(desktopVm.IsShared);
			desktopVm.ShareCommand.Execute(null);
			Assert.IsTrue(desktopVm.IsShared);
		}

		[Tag("viewmodel")]
		[Tag("shareddesktop")]
		[Tag("shareddesktopserverviewmodel")]
		[TestMethod]
		public void UnshareCommandTest()
		{
			Assert.IsFalse(desktopVm.IsShared);
			desktopVm.ShareCommand.Execute(null);
			Assert.IsTrue(desktopVm.IsShared);
			desktopVm.UnshareCommand.Execute(null);
			Assert.IsFalse(desktopVm.IsShared);
			Assert.AreEqual(SharedDesktopState.Ready, desktopVm.State);
		}
		#endregion
	}
}
