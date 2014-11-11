using System;
using System.Collections.ObjectModel;
using Alanta.Client.Common;
using Alanta.Client.Common.Collections;
using Alanta.Client.Data.RoomService;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.ViewModels;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveUI;

namespace Alanta.Client.Test
{
    [TestClass]
    public class ObservableCollectionTests : SilverlightTest
    {
        private ViewModelFactory viewModelFactory;
        private ObservableCollection<WhiteboardViewModel> whiteboards;
        private ObservableCollection<SharedFileViewModel> sharedFiles;
        private MergedObservableCollection<IWorkspaceItem> mergedOc;
        private FilteredObservableCollection<WhiteboardViewModel> filteredOc;

        [TestInitialize]
        public void TestInitialize()
        {
            var messageService = new TestMessageService();
            var roomService = new TestRoomServiceAdapter();
            var viewLocator = new ViewLocator();
            viewModelFactory = new ViewModelFactory(roomService, messageService, viewLocator);
            mergedOc = new MergedObservableCollection<IWorkspaceItem>();
            whiteboards = new ObservableCollection<WhiteboardViewModel>();
            sharedFiles = new ObservableCollection<SharedFileViewModel>();
        }

        #region MergedObservableCollection Tests

        [TestMethod]
        [Tag("observablecollection")]
        public void MergedOC_RegisterObservedCollection()
        {
            whiteboards.Add(GetNewWhiteboard());
            sharedFiles.Add(GetNewSharedFile());
            mergedOc.RegisterObservedCollection(whiteboards);
            mergedOc.RegisterObservedCollection(sharedFiles);
            Assert.AreEqual(2, mergedOc.Count);
        }

        [TestMethod]
        [Tag("observablecollection")]
        public void MergedOC_UnregisterObservedCollection()
        {
            whiteboards.Add(GetNewWhiteboard());
            sharedFiles.Add(GetNewSharedFile());
            mergedOc.RegisterObservedCollection(whiteboards);
            mergedOc.RegisterObservedCollection(sharedFiles);
            Assert.AreEqual(2, mergedOc.Count);
            mergedOc.UnregisterObservedCollection(whiteboards);
            Assert.AreEqual(1, mergedOc.Count);
            mergedOc.UnregisterObservedCollection(sharedFiles);
            Assert.AreEqual(0, mergedOc.Count);
        }

        [TestMethod]
        [Tag("observablecollection")]
        public void MergedOC_AddAndRemoveItems()
        {
            whiteboards.Add(GetNewWhiteboard());
            sharedFiles.Add(GetNewSharedFile());
            mergedOc.RegisterObservedCollection(whiteboards);
            mergedOc.RegisterObservedCollection(sharedFiles);
            Assert.AreEqual(2, mergedOc.Count);
            whiteboards.Add(GetNewWhiteboard());
            sharedFiles.Add(GetNewSharedFile());
            Assert.AreEqual(4, mergedOc.Count);
            var whiteboard = GetNewWhiteboard();
            var sharedFile = GetNewSharedFile();
            whiteboards.Add(whiteboard);
            Assert.AreEqual(5, mergedOc.Count);
            whiteboards.Remove(whiteboard);
            Assert.AreEqual(4, mergedOc.Count);
            sharedFiles.Add(sharedFile);
            Assert.AreEqual(5, mergedOc.Count);
            sharedFiles.Remove(sharedFile);
            Assert.AreEqual(4, mergedOc.Count);
        }

        [TestMethod]
        [Tag("observablecollection")]
        public void MergedOC_AddAndRemoveMultipleInstances()
        {
            mergedOc.RegisterObservedCollection(whiteboards);
            int startingItems = mergedOc.Count;
            var whiteboard1 = GetNewWhiteboard();
            whiteboards.Add(whiteboard1);
            Assert.AreEqual(startingItems + 1, mergedOc.Count);
            whiteboards.Add(whiteboard1);
            Assert.AreEqual(startingItems + 1, mergedOc.Count);
            whiteboards.Add(whiteboard1);
            Assert.AreEqual(startingItems + 1, mergedOc.Count);
            whiteboards.Add(whiteboard1);
            Assert.AreEqual(startingItems + 1, mergedOc.Count);

            whiteboards.Remove(whiteboard1);
            Assert.AreEqual(startingItems + 1, mergedOc.Count);
            whiteboards.Remove(whiteboard1);
            Assert.AreEqual(startingItems + 1, mergedOc.Count);
            whiteboards.Remove(whiteboard1);
            Assert.AreEqual(startingItems + 1, mergedOc.Count);
            whiteboards.Remove(whiteboard1);
            Assert.AreEqual(startingItems, mergedOc.Count);
        }

        #endregion

        #region FilteredObservableCollection Tests

        [TestMethod]
        [Tag("observablecollection")]
        public void FilteredOC_InitializeTest()
        {
            var w1 = GetNewWhiteboard();
            w1.Model.CreatedOn = DateTime.Now;
            var w2 = GetNewWhiteboard();
            w2.Model.CreatedOn = DateTime.Now;
            var w3 = GetNewWhiteboard();
            w3.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);
            var w4 = GetNewWhiteboard();
            w4.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);

            whiteboards.Add(w1);
            whiteboards.Add(w2);
            whiteboards.Add(w3);
            whiteboards.Add(w4);

            filteredOc = new FilteredObservableCollection<WhiteboardViewModel>(whiteboards, vm => vm.Model.CreatedOn > DateTime.Now - TimeSpan.FromHours(1));

            Assert.AreEqual(2, filteredOc.Count);
        }

        [TestMethod]
        [Tag("observablecollection")]
        public void FilteredOC_ResetTest()
        {
            var w1 = GetNewWhiteboard();
            w1.Model.CreatedOn = DateTime.Now;
            var w2 = GetNewWhiteboard();
            w2.Model.CreatedOn = DateTime.Now;
            var w3 = GetNewWhiteboard();
            w3.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);
            var w4 = GetNewWhiteboard();
            w4.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);

            whiteboards.Add(w1);
            whiteboards.Add(w2);
            whiteboards.Add(w3);
            whiteboards.Add(w4);

            filteredOc = new FilteredObservableCollection<WhiteboardViewModel>(whiteboards, vm => vm.Model.CreatedOn > DateTime.Now - TimeSpan.FromHours(1));

            whiteboards.Clear();
            Assert.AreEqual(0, filteredOc.Count);
        }

        [TestMethod]
        [Tag("observablecollection")]
        public void FilteredOC_AddAndRemoveTest()
        {
            filteredOc = new FilteredObservableCollection<WhiteboardViewModel>(whiteboards, vm => vm.Model.CreatedOn > DateTime.Now - TimeSpan.FromHours(1));

            var w1 = GetNewWhiteboard();
            w1.Model.CreatedOn = DateTime.Now;
            var w2 = GetNewWhiteboard();
            w2.Model.CreatedOn = DateTime.Now;
            var w3 = GetNewWhiteboard();
            w3.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);
            var w4 = GetNewWhiteboard();
            w4.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);

            whiteboards.Add(w1);
            whiteboards.Add(w2);
            whiteboards.Add(w3);
            whiteboards.Add(w4);

            Assert.AreEqual(2, filteredOc.Count);

            whiteboards.Remove(w1);
            whiteboards.Remove(w2);
            whiteboards.Remove(w3);
            whiteboards.Remove(w4);

            Assert.AreEqual(0, filteredOc.Count);
        }

        [TestMethod]
        [Tag("observablecollection")]
        public void FilteredOC_AddAndRemoveMultipleInstancesTest()
        {
            filteredOc = new FilteredObservableCollection<WhiteboardViewModel>(whiteboards, vm => vm.Model.CreatedOn > DateTime.Now - TimeSpan.FromHours(1));

            var w1 = GetNewWhiteboard();
            w1.Model.CreatedOn = DateTime.Now;
            var w2 = GetNewWhiteboard();
            w2.Model.CreatedOn = DateTime.Now;
            var w3 = GetNewWhiteboard();
            w3.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);
            var w4 = GetNewWhiteboard();
            w4.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);

            whiteboards.Add(w1);
            whiteboards.Add(w2);
            whiteboards.Add(w3);
            whiteboards.Add(w4);
            Assert.AreEqual(2, filteredOc.Count);

            whiteboards.Add(w1);
            whiteboards.Add(w2);
            whiteboards.Add(w3);
            whiteboards.Add(w4);
            Assert.AreEqual(2, filteredOc.Count);

            whiteboards.Add(w1);
            whiteboards.Add(w2);
            whiteboards.Add(w3);
            whiteboards.Add(w4);
            Assert.AreEqual(2, filteredOc.Count);

            whiteboards.Remove(w1);
            whiteboards.Remove(w2);
            whiteboards.Remove(w3);
            whiteboards.Remove(w4);
            Assert.AreEqual(2, filteredOc.Count);

            whiteboards.Remove(w1);
            whiteboards.Remove(w2);
            whiteboards.Remove(w3);
            whiteboards.Remove(w4);
            Assert.AreEqual(2, filteredOc.Count);

            whiteboards.Remove(w1);
            whiteboards.Remove(w2);
            whiteboards.Remove(w3);
            whiteboards.Remove(w4);
            Assert.AreEqual(0, filteredOc.Count);
        }

        [TestMethod]
        [Tag("observablecollection")]
        public void FilteredOC_TriggersTest()
        {
            var filterSource = new FilterSource() {StartDate = DateTime.Now - TimeSpan.FromHours(1)};
            filteredOc = new FilteredObservableCollection<WhiteboardViewModel>(whiteboards, vm => vm.Model.CreatedOn > filterSource.StartDate, filterSource);

            var w1 = GetNewWhiteboard();
            w1.Model.CreatedOn = DateTime.Now;
            var w2 = GetNewWhiteboard();
            w2.Model.CreatedOn = DateTime.Now;
            var w3 = GetNewWhiteboard();
            w3.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);
            var w4 = GetNewWhiteboard();
            w4.Model.CreatedOn = DateTime.Now - TimeSpan.FromDays(1);

            whiteboards.Add(w1);
            whiteboards.Add(w2);
            whiteboards.Add(w3);
            whiteboards.Add(w4);
            Assert.AreEqual(2, filteredOc.Count);

            filterSource.StartDate = DateTime.Now - TimeSpan.FromDays(2);
            Assert.AreEqual(4, filteredOc.Count);

            filterSource.StartDate = DateTime.Now + TimeSpan.FromDays(2);
            Assert.AreEqual(0, filteredOc.Count);
        }

        #endregion

        #region Support Methods

        private WhiteboardViewModel GetNewWhiteboard()
        {
            var whiteboard = new Data.RoomService.Whiteboard() {WhiteboardId = Guid.NewGuid()};
            var vm = viewModelFactory.GetViewModel<WhiteboardViewModel>(wvm => wvm.Model.WhiteboardId == whiteboard.WhiteboardId);
            vm.Model = whiteboard;
            return vm;
        }

        private SharedFileViewModel GetNewSharedFile()
        {
            var sharedFile = new SharedFile() {SharedFileId = Guid.NewGuid()};
            var vm = viewModelFactory.GetViewModel<SharedFileViewModel>(sfvm => sfvm.Model.SharedFileId == sharedFile.SharedFileId);
            vm.Model = sharedFile;
            return vm;
        }
        #endregion

    }

    public class FilterSource : ReactiveObject
    {
        private DateTime startDate;
        public DateTime StartDate
        {
            get { return startDate; }
            set { this.RaiseAndSetIfChanged(x => x.StartDate, ref startDate, value); }
        }
    }
}
