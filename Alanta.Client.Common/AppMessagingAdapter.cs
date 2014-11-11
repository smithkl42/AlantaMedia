using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Alanta.Client.Common.Loader;

namespace Alanta.Client.Common
{
    /// <summary>
    ///     Application Messaging Adapter for notify UI
    /// </summary>
    public class AppMessagingAdapter : IDisposable
    {
        private static AppMessagingAdapter _instance;
        private IIsolatedStorageService _isolatedStorageService;
        private IMessageService _messageService;

        private AppMessagingAdapter()
        {
        }

        public static AppMessagingAdapter Instance
        {
            get { return _instance ?? (_instance = new AppMessagingAdapter()); }
        }

        public void Dispose()
        {
            _isolatedStorageService = null;
            _messageService = null;
        }

        public void Initialize(IMessageService messagingService, IIsolatedStorageService storageService)
        {
            _messageService = messagingService;
            _isolatedStorageService = storageService;
        }

        public void TryIncreaseIsolateStorage(Action<bool> callback)
        {
            if (_isolatedStorageService != null)
            {
                _isolatedStorageService.TryIncreaseStorage(callback);
            }
            else if (callback != null)
            {
                callback(false);
            }
        }

        public void ShowHint(string message)
        {
            if (_messageService != null)
                _messageService.ShowHint(message);
        }

        public void ShowErrorHint(string message)
        {
            if (_messageService != null)
                _messageService.ShowErrorHint(message);
        }

        public void ShowHint(List<Inline> inlines)
        {
            if (_messageService != null)
                _messageService.ShowHint(inlines);
        }
    }
}