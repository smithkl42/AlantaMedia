using System;
using System.Windows.Browser;
using System.Windows.Threading;
using Alanta.Client.Common.Desktop;
using Alanta.Client.Common.Logging;
using System.Windows;

namespace Alanta.Client.Common
{
    [ScriptableType]
    public class GlobalDomPublisher
    {
        public event EventHandler<VncErrorEventArgs> VncErrorOccured;

        // Deprecated - moved to VncServer.cs.
        [ScriptableMember]
        public void OnVncError(int errorCode, string errorMessage)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
              {
                  if (VncErrorOccured != null)
                      VncErrorOccured(this, new VncErrorEventArgs(errorCode, errorMessage));
                  ClientLogger.LogDebugMessage(string.Format("WinVnc error: ({0}) - {1}", errorCode, errorMessage));
              });
        }
    }
}
