
using System.Collections.Generic;
using System.ComponentModel;

namespace Alanta.Client.Common.Collections
{
    public class ObservedItem<T> where T : class, INotifyPropertyChanged
    {
        public ObservedItem(T item)
        {
            Item = item;
            Sources = new List<object>();
        }

        public T Item { get; set; }
        public List<object> Sources { get; private set; }
    }

}
