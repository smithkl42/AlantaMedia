using Alanta.Client.Common.Logging;
using System;

namespace Alanta.Client.Common
{
    public interface IController: IDisposable
    {
        DataConnectionManager DataConnectionManager { get; }
        ClientLogger Logger { get; }
        LoggerData LoggerData { get; }
        // void Dispose();
    }
}
