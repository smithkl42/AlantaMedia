using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Alanta.Client.Common.Collections
{
    /// <summary>
    /// Watches a variety of ObservableCollection instances and merges items placed or removed from those collections.
    /// </summary>
    /// <typeparam name="T">The Type of the ObservableCollection watched.</typeparam>
    /// <remarks>
    /// Unlike a normal ObservableCollection, this collection enforces uniqueness, i.e., each item can only be added one time.
    /// However, if an item is added more than once, a reference counter is also incremented, so that if, say, three instances of an 
    /// object are added to three different source collections, the object itself won't be removed until all three instances of it have 
    /// been removed from the two source collections.
    /// </remarks>
    public class MergedObservableCollection<T> : ObservableCollection<T>, IDisposable
        where T : class, INotifyPropertyChanged
    {
        #region Constructors
        public MergedObservableCollection()
        {
            _observedCollections = new List<ObservedCollection<T>>();
            _observedItems = new List<ObservedItem<T>>();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private bool _disposed;

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                foreach (var collection in _observedCollections)
                {
                    collection.ObservableCollection.CollectionChanged -= observedCollection_CollectionChanged;
                }
                _observedCollections.Clear();
                _disposed = true;
            }
        }
        #endregion

        #region Fields and Properties

        private readonly List<ObservedCollection<T>> _observedCollections;
        private readonly List<ObservedItem<T>> _observedItems;

        #endregion

        #region Methods

        /// <summary>
        /// Registers an ObservableCollection and adds all filtered members to the MergedObservableCollection.
        /// </summary>
        /// <param name="collection">The collection to observe for additions and deletions.</param>
        public void RegisterObservedCollection(INotifyCollectionChanged collection)
        {
            var observedCollection = _observedCollections.FirstOrDefault(oc => oc.ObservableCollection == collection);
            if (observedCollection == null)
            {
                observedCollection = new ObservedCollection<T>
                {
                    ObservableCollection = collection
                };
                _observedCollections.Add(observedCollection);
                collection.CollectionChanged += observedCollection_CollectionChanged;
                var items = collection as IEnumerable;
                if (items != null)
                {
                    AddItems(items.Cast<T>(), observedCollection);
                }
            }
        }

        public void UnregisterObservedCollection(INotifyCollectionChanged collection)
        {
            var observedCollection = _observedCollections.FirstOrDefault(oc => oc.ObservableCollection == collection);
            if (observedCollection != null)
            {
                _observedCollections.Remove(observedCollection);
                collection.CollectionChanged -= observedCollection_CollectionChanged;
                var items = collection as IEnumerable;
                if (items != null)
                {
                    RemoveItems(items.Cast<T>(), observedCollection);
                }
            }
        }

        void observedCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var observedCollection = _observedCollections.FirstOrDefault(oc => oc.ObservableCollection == sender);
            if (observedCollection != null)
            {
                if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
                {
                    if (e.NewItems != null)
                    {
                        AddItems(e.NewItems.Cast<T>(), observedCollection);
                    }
                    if (e.OldItems != null)
                    {
                        RemoveItems(e.OldItems.Cast<T>(), observedCollection);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    var itemsToRemove = _observedItems.Where(oi => oi.Sources.Contains(observedCollection)).Select(oi => oi.Item);
                    RemoveItems(itemsToRemove, observedCollection);
                }
            }
        }

        private void AddItems(IEnumerable<T> items, ObservedCollection<T> observedCollection)
        {
            if (observedCollection.AddFilter == null)
            {
                foreach (T item in items)
                {
                    AddPrivate(item, observedCollection);
                }
            }
        }

        private void RemoveItems(IEnumerable<T> itemsToRemove, ObservedCollection<T> observedCollection)
        {
            foreach (var item in itemsToRemove)
            {
                RemovePrivate(item, observedCollection);
            }
        }

        /// <summary>
        /// Adds an item to the merged collection and increments its reference count.
        /// </summary>
        private void AddPrivate(T item, ObservedCollection<T> observedCollection)
        {
            if (!Contains(item))
            {
                Add(item);
            }
            var observedItem = _observedItems.FirstOrDefault(i => i.Item == item);
            if (observedItem == null)
            {
                observedItem = new ObservedItem<T>(item);
                _observedItems.Add(observedItem);
            }
            observedItem.Sources.Add(observedCollection);
        }

        /// <summary>
        /// Decrements an item's reference count and if it's reached zero, removes it from the main collection.
        /// </summary>
        private void RemovePrivate(T item, ObservedCollection<T> observedCollection)
        {
            var observedItem = _observedItems.FirstOrDefault(i => i.Item == item);
            if (observedItem != null)
            {
                observedItem.Sources.Remove(observedCollection);
                if (observedItem.Sources.Count > 0)
                {
                    return;
                }
            }
            Remove(item);
        }
        #endregion

    }
}
