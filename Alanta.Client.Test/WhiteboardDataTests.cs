using System;
using Alanta.Client.Common;
using Alanta.Client.Data.RoomService;
using Microsoft.Silverlight.Testing;
using Microsoft.Silverlight.Testing.UnitTesting.Metadata.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Alanta.Client.Data;
using System.Collections.ObjectModel;
using Alanta.Client.UI.Common.ViewModels;
using System.Collections.Specialized;
using System.Windows;

namespace Alanta.Client.Test
{
    [TestClass]
    public class WhiteboardDataTests : DataTestBase
    {

        [TestMethod]
        [Asynchronous]
        [Tag("whiteboard")]
        [Timeout(30000)]
        public void RegisterWhiteboardTest()
        {
            bool whiteboardRegistered = false;
            Guid whiteboardID = Guid.NewGuid();
            var whiteboardCollectionViewModels = _viewModelFactory.GetViewModel<WhiteboardCollectionViewModel>();

            // Kick everything off.
            JoinRoom(joinRoomException =>
                {
                    whiteboardCollectionViewModels.ViewModels.CollectionChanged += (s, e) =>
                        {
                            var vm = e.NewItems[0] as WhiteboardViewModel;
                            Assert.AreEqual(whiteboardID, vm.Model.WhiteboardId);
                            whiteboardRegistered = true;
                        };

                    _roomService.RegisterWhiteboard(_roomVm.SessionId, whiteboardID, error =>
                    {
                        Assert.IsNull(error);
                    });
                });
            EnqueueConditional(() => whiteboardRegistered);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Tag("whiteboard")]
        [Timeout(60000)]
        public void UnregisterWhiteboardTest()
        {
            bool whiteboardUnregistered = false;

            JoinRoom(joinRoomException =>
                {
                    var whiteboardCollectionViewModels = _viewModelFactory.GetViewModel<WhiteboardCollectionViewModel>();
                    Guid whiteboardID = Guid.Empty;
                    whiteboardCollectionViewModels.ViewModels.CollectionChanged += (s, e) =>
                    {
                        if (e.Action == NotifyCollectionChangedAction.Add)
                        {
                            var vm = e.NewItems[0] as WhiteboardViewModel;
                            whiteboardID = vm.Model.WhiteboardId;
                            vm.DeleteCommand.Execute(null);
                        }
                        if (e.Action == NotifyCollectionChangedAction.Remove)
                        {
                            var vm = e.OldItems[0] as WhiteboardViewModel;
                            Assert.AreEqual(whiteboardID, vm.Model.WhiteboardId);
                            whiteboardUnregistered = true;
                        }
                    };
                    whiteboardCollectionViewModels.CreateWhiteboard();
                });
            EnqueueConditional(() => whiteboardUnregistered);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Tag("whiteboard")]
        [Timeout(60000)]
        public void WhiteboardAddedReceivedTest()
        {
            bool whiteboardAddedReceived = false;
            var whiteboardModels = new ObservableCollection<Data.RoomService.Whiteboard>();
            var whiteboardCollectionViewModel = _viewModelFactory.GetViewModel<WhiteboardCollectionViewModel>();
            whiteboardCollectionViewModel.Models = whiteboardModels;
            var whiteboard = new Data.RoomService.Whiteboard()
            {
                WhiteboardId = Guid.NewGuid(),
                Room = _roomVm.Model
            };
            whiteboardCollectionViewModel.ViewModels.CollectionChanged += (s, e) =>
            {
                var vm = e.NewItems[0] as WhiteboardViewModel;
                Assert.AreSame(whiteboard, vm.Model);
                whiteboardAddedReceived = true;
            };
            whiteboardModels.Add(whiteboard);
            EnqueueConditional(() => whiteboardAddedReceived);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Tag("whiteboard")]
        [Timeout(30000)]
        public void WhiteboardRemovedReceivedTest()
        {
            bool whiteboardRemovedReceived = false;
            var whiteboardModels = new ObservableCollection<Data.RoomService.Whiteboard>();
            var whiteboardCollectionViewModel = _viewModelFactory.GetViewModel<WhiteboardCollectionViewModel>();
            whiteboardCollectionViewModel.Models = whiteboardModels;
            var whiteboard = new Data.RoomService.Whiteboard()
            {
                WhiteboardId = Guid.NewGuid(),
                Room = _roomVm.Model
            };
            whiteboardCollectionViewModel.ViewModels.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    var vm = e.NewItems[0] as WhiteboardViewModel;
                    Assert.AreSame(whiteboard, vm.Model);
                    Deployment.Current.Dispatcher.BeginInvoke(() => whiteboardModels.Remove(whiteboard));
                }
                else
                {
                    var vm = e.OldItems[0] as WhiteboardViewModel;
                    Assert.AreEqual(whiteboard, vm.Model);
                    whiteboardRemovedReceived = true;
                }
            };
            whiteboardModels.Add(whiteboard);
            EnqueueConditional(() => whiteboardRemovedReceived);
            EnqueueTestComplete();
        }


    }
}
