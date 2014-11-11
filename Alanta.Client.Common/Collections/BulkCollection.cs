using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Alanta.Client.Common.Collections
{
    /// <summary>
    /// Allow insert list of items, what improve performance when executes code on collection changed
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    public class BulkCollection<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private bool _busy;

        //protected event PropertyChangedEventHandler PropertyChanged;

        //event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged;

        public BulkCollection()
        {
        }

        public BulkCollection(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            CopyFrom(collection);
        }

        public BulkCollection(List<T> list)
            : base(list ?? new List<T>())
        {
            CopyFrom(list);
        }

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public event EventHandler<BulkCollectionEventArgs<T>> BulkCollectionChanged;

        private void CheckReentrancy()
        {
            if (_busy)
            {
                throw new InvalidOperationException("ObservableCollection_CannotChangeObservableCollection");
            }
        }

        protected override void ClearItems()
        {
            CheckReentrancy();
            base.ClearItems();
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            RaiseBulkCollectionChanged(new BulkCollectionEventArgs<T>(NotifyCollectionChangedAction.Reset));
        }

        private void CopyFrom(IEnumerable<T> collection)
        {
            var items = Items;
            if ((collection != null) && (items != null))
            {
                using (var enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        items.Add(enumerator.Current);
                    }
                }
            }
        }

        protected override void InsertItem(int index, T item)
        {
            CheckReentrancy();
            base.InsertItem(index, item);
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            RaiseBulkCollectionChanged(new BulkCollectionEventArgs<T>(NotifyCollectionChangedAction.Add, item, index));
        }

        public void AddRange(IEnumerable<T> collection)
        {
            InsertRange(collection, Count);
        }

        public void InsertRange(IEnumerable<T> collection, int index)
        {
            int startIndex = index;
            var items = Items;
            var addedItems = new List<T>();
            if ((collection != null) && (items != null))
            {
                using (var enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        CheckReentrancy();
                        base.InsertItem(index, enumerator.Current);
                        addedItems.Add(enumerator.Current);
                        OnPropertyChanged("Count");
                        OnPropertyChanged("Item[]");
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, enumerator.Current, index));
                        index++;
                    }
                }
            }

            RaiseBulkCollectionChanged(new BulkCollectionEventArgs<T>(NotifyCollectionChangedAction.Add, addedItems, startIndex));
        }


        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                _busy = true;
                try
                {
                    CollectionChanged(this, e);
                }
                finally
                {
                    _busy = false;
                }
            }
        }

        private void RaiseBulkCollectionChanged(BulkCollectionEventArgs<T> e)
        {
            if (BulkCollectionChanged != null)
            {
                BulkCollectionChanged(this, e);
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                _busy = true;
                try
                {
                    PropertyChanged(this, e);
                }
                finally
                {
                    _busy = false;
                }
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected override void RemoveItem(int index)
        {
            CheckReentrancy();
            var changedItem = base[index];
            base.RemoveItem(index);
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedItem, index));
            RaiseBulkCollectionChanged(new BulkCollectionEventArgs<T>(NotifyCollectionChangedAction.Remove, changedItem, index));
        }

        protected override void SetItem(int index, T item)
        {
            CheckReentrancy();
            var oldItem = base[index];
            base.SetItem(index, item);
            OnPropertyChanged("Item[]");
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
            RaiseBulkCollectionChanged(new BulkCollectionEventArgs<T>(NotifyCollectionChangedAction.Replace, item, oldItem, index));
        }
    }
}