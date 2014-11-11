using System.Linq;
using System.Windows;
using Alanta.Client.UI.Common.SampleData;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class SessionViewModelTests : ViewModelTestBase
	{
		[Tag("viewmodel")]
		[Tag("sessioncollectionviewmodel")]
		[Tag("session")]
		[TestMethod]
		public void RemoteSessionsTest()
		{
			// Arrange
			var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			roomVm.Model = room;
			roomVm.SessionId = room.Sessions.First().SessionId;
			var sessionsVm = viewModelFactory.GetViewModel<SessionCollectionViewModel>();

			// Act
			sessionsVm.Models = room.Sessions;

			// Assert
			Assert.AreEqual(room.Sessions.Count, sessionsVm.ViewModels.Count);
			Assert.AreEqual(room.Sessions.Count - 1, sessionsVm.RemoteSessions.Count);
			Assert.IsFalse(sessionsVm.RemoteSessions.Any(vm => vm.Model.SessionId == roomVm.SessionId));
		}

		[Tag("viewmodel")]
		[Tag("sessioncollectionviewmodel")]
		[Tag("session")]
		[TestMethod]
		public void SampleSessionCollectionViewModelTest()
		{
			// Arrange and Act
			var sample = new SampleSessionCollectionViewModel();

			// Assert
			Assert.AreEqual(room.Sessions.Count, sample.ViewModels.Count);
			foreach (var roomSession in room.Sessions)
			{
				var session = roomSession;
				Assert.IsTrue(sample.RemoteSessions.Any(vm => vm.Model.SessionId == session.SessionId));
			}
		}

		[Tag("viewmodel")]
		[Tag("sessionviewmodel")]
		[Tag("session")]
		[TestMethod]
		public void AvatarVisibilityTest()
		{
			// Arrange
			var sample = new SampleSessionCollectionViewModel();
			var sessionVm = sample.ViewModels.First();
			Assert.AreEqual(Visibility.Visible, sessionVm.AvatarVisibility);
			Assert.AreEqual(Visibility.Collapsed, sessionVm.VideoVisibility);

			// Act
			sessionVm.AvatarVisibility = Visibility.Collapsed;

			// Assert
			Assert.AreEqual(Visibility.Collapsed, sessionVm.AvatarVisibility);
			Assert.AreEqual(Visibility.Visible, sessionVm.VideoVisibility);
		}

	}
}
