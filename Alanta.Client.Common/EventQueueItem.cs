using System;

namespace Alanta.Client
{
    public class EventQueueItem
    {
        public Action<object, object> PostponedEvent { get; set; }
        public object OldItem { get; set; }
        public object NewItem { get; set; }
        public EventQueueItem(Action<object, object> postponedEvent, object oldItem, object newItem)
        {
            PostponedEvent = postponedEvent;
        }
    }
}
