using System;
using System.Collections.Specialized;

namespace Alanta.Client.Common.Collections
{
    /// <summary>
    /// Represents an instance of an ObservableCollection that is being watched for changes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservedCollection<T>
    {
        /// <summary>
        /// The ObservableCollection that is being watched for changes.
        /// </summary>
        public INotifyCollectionChanged ObservableCollection { get; set; }

        /// <summary>
        /// The function that determines which items added to the ObservableCollection make it into the merged collection.
        /// </summary>
        public Func<T, bool> AddFilter { get; set; }

        /// <summary>
        /// The function that determines which items removed from the ObservableCollection are removed from the merged collection.
        /// </summary>
        public Func<T, bool> RemoveFilter { get; set; }
    }
}
