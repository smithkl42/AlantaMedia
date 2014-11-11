using Alanta.Client.UI.Common.SampleData;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Alanta.Client.UI.Common.ViewModels;
using System.Linq;

namespace Alanta.Client.Test.ViewModelTests
{
    [TestClass]
    public class WorkspaceViewModelTests : ViewModelTestBase
    {

        [TestMethod]
        [Tag("room")]
        [Tag("viewmodel")]
        [Tag("workspaceviewmodel")]
        public void SampleWorkspaceViewModelTest()
        {
            var sampleWorkspaceVm = new SampleWorkspaceViewModel();
            Assert.AreEqual(room.Whiteboards.Count + room.SharedFiles.Count, sampleWorkspaceVm.WorkspaceItems.Count);
        }

        [TestMethod]
        [Tag("room")]
        [Tag("viewmodel")]
        [Tag("workspaceviewmodel")]
        public void OpenAndClosedWorkspaceItemsTest()
        {
            var localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
            localUserVm.Model = user;
            var roomVm = viewModelFactory.GetViewModel<RoomViewModel>();
            roomVm.Model = room;
            var workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
            workspaceVm.WorkspaceItems.RegisterObservedCollection(roomVm.WhiteboardCollectionVm.ViewModels);
            int allStart = workspaceVm.WorkspaceItems.Count;
            int closedStart = workspaceVm.ClosedWorkspaceItems.Count;
            int openStart = workspaceVm.OpenWorkspaceItems.Count;

            var whiteboards = viewModelFactory.GetViewModel<WhiteboardCollectionViewModel>();
            var wopen = GetNewWhiteboardVm();
            wopen.IsShared = true;
            var wclosed = GetNewWhiteboardVm();
            wclosed.IsShared = false;
            whiteboards.ViewModels.Add(wopen);
            whiteboards.ViewModels.Add(wclosed);
            Assert.AreEqual(allStart + 2, workspaceVm.WorkspaceItems.Count);
            Assert.AreEqual(closedStart + 1, workspaceVm.ClosedWorkspaceItems.Count);
            Assert.AreEqual(openStart + 1, workspaceVm.OpenWorkspaceItems.Count);
            Assert.IsTrue(workspaceVm.ClosedWorkspaceItems.Contains(wclosed));
            Assert.IsTrue(workspaceVm.OpenWorkspaceItems.Contains(wopen));
            wopen.IsShared = false;
            Assert.AreEqual(openStart, workspaceVm.OpenWorkspaceItems.Count);
            Assert.AreEqual(closedStart + 2, workspaceVm.ClosedWorkspaceItems.Count);
        }

    }
}
