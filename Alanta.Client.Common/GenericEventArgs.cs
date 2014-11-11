using System;

namespace Alanta.Client.Common
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }

    public class EventArgs<T1, T2> : EventArgs
    {
        public EventArgs(T1 value1, T2 value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public T1 Value1 { get; set; }
        public T2 Value2 { get; set; }
    }

    public class OperationCompletedEventArgs : EventArgs
    {
        public OperationCompletedEventArgs(Exception error)
        {
            Error = error;
        }
        public Exception Error { get; set; }
    }

    public class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception exception, string message)
        {
            Exception = exception;
            Message = message;
        }
        public Exception Exception { get; set; }
        public string Message { get; set; }
    }

}
