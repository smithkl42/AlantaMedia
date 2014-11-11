using System;

namespace Alanta.Client.Common.Logging.Targets
{
    public class TargetExceptionArgs : ExceptionEventArgs
    {
        public TargetExceptionArgs(Exception ex, string message, bool isTargetAvailable)
            : base(ex, message)
        {
            IsTargetAvailable = isTargetAvailable;
        }

        public bool IsTargetAvailable { get; set; }
    }
}
