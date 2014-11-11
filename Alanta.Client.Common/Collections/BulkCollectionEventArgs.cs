using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Alanta.Client.Common.Collections
{
    public class BulkCollectionEventArgs<T> : EventArgs
    {
        private readonly NotifyCollectionChangedAction _action;
        private readonly List<T> _newItems;
        private readonly int _newStartingIndex;
        private readonly List<T> _oldItems;
        private readonly int _oldStartingIndex;

        public BulkCollectionEventArgs(NotifyCollectionChangedAction action)
        {
            _newStartingIndex = -1;
            _oldStartingIndex = -1;
            if (action != NotifyCollectionChangedAction.Reset)
            {
                throw new NotSupportedException("NotifyCollectionChangedEventArgs_UnSupportedConstructorAction");
            }
            _action = action;
        }

        public BulkCollectionEventArgs(NotifyCollectionChangedAction action, T changedItem, int index)
        {
            _newStartingIndex = -1;
            _oldStartingIndex = -1;
            if ((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove))
            {
                throw new NotSupportedException("NotifyCollectionChangedEventArgs_ConstructorOnlySupportsEitherAddOrRemove");
            }
            _action = action;
            if (action == NotifyCollectionChangedAction.Add)
            {
                _newItems = new List<T> { changedItem };
                _newStartingIndex = index;
            }
            else
            {
                _oldItems = new List<T> { changedItem };
                _oldStartingIndex = index;
            }
        }

        public BulkCollectionEventArgs(NotifyCollectionChangedAction action, List<T> changedItems, int index)
        {
            _newStartingIndex = -1;
            _oldStartingIndex = -1;
            if ((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove))
            {
                throw new NotSupportedException("NotifyCollectionChangedEventArgs_ConstructorOnlySupportsEitherAddOrRemove");
            }
            _action = action;
            if (action == NotifyCollectionChangedAction.Add)
            {
                _newItems = changedItems;
                _newStartingIndex = index;
            }
            else
            {
                _oldItems = changedItems;
                _oldStartingIndex = index;
            }
        }

        public BulkCollectionEventArgs(NotifyCollectionChangedAction action, T newItem, T oldItem, int index)
        {
            _newStartingIndex = -1;
            _oldStartingIndex = -1;
            if (action != NotifyCollectionChangedAction.Replace)
            {
                throw new NotSupportedException("NotifyCollectionChangedEventArgs_UnSupportedConstructorAction");
            }
            _action = action;
            _newItems = new List<T> { newItem };
            _oldItems = new List<T> { oldItem };
            _newStartingIndex = index;
        }

        public NotifyCollectionChangedAction Action
        {
            get
            {
                return _action;
            }
        }

        public List<T> NewItems
        {
            get
            {
                return _newItems;
            }
        }

        public int NewStartingIndex
        {
            get
            {
                return _newStartingIndex;
            }
        }

        public List<T> OldItems
        {
            get
            {
                return _oldItems;
            }
        }

        public int OldStartingIndex
        {
            get
            {
                return _oldStartingIndex;
            }
        }
    }
}
