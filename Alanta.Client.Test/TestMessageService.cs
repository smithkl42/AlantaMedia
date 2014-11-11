using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Alanta.Client.Common;
using Alanta.Client.Common.Loader;
using Alanta.Client.UI.Common.ViewModels;

namespace Alanta.Client.Test
{
    public class TestMessageService : MessageService
    {
        public event EventHandler<EventArgs<string>> ShowErrorMessageCalled;
        public event EventHandler<EventArgs<string>> ShowInformativeMessageCalled;
        public event EventHandler<EventArgs<string>> ShowInformativeHintCalled;
        public event EventHandler<EventArgs<List<Inline>>> ShowInformativeMessageWithInlinesCalled;
        public event EventHandler<EventArgs<string>> ShowErrorHintCalled;
        public event EventHandler<EventArgs> PlayChatMessageSentSoundCalled;
        public event EventHandler<EventArgs> PlayChatMessageReceivedSoundCalled;
        public event EventHandler<EventArgs> PlayRoomUpdatedSoundCalled;

        public override void ShowErrorMessage(string message)
        {
            if (ShowErrorMessageCalled != null)
            {
                ShowErrorMessageCalled(this, new EventArgs<string>(message));
            }
        }

		public override void ShowMessage(string message)
        {
            if (ShowInformativeMessageCalled != null)
            {
                ShowInformativeMessageCalled(this, new EventArgs<string>(message));
            }
        }

		public override void ShowErrorHint(string message)
        {
            if (ShowErrorHintCalled != null)
            {
                ShowErrorHintCalled(this, new EventArgs<string>(message));
            }
        }

		public override void ShowHint(string message)
        {
            if (ShowInformativeHintCalled != null)
            {
                ShowInformativeHintCalled(this, new EventArgs<string>(message));
            }
        }

		public override void PlayChatMessageSentSound()
        {
            if (PlayChatMessageSentSoundCalled != null)
            {
                PlayChatMessageSentSoundCalled(this, new EventArgs());
            }
        }

		public override void PlayChatMessageReceivedSound()
        {
            if (PlayChatMessageReceivedSoundCalled != null)
            {
                PlayChatMessageReceivedSoundCalled(this, new EventArgs());
            }
        }

		public override void PlayRoomUpdatedSound()
        {
            if (PlayRoomUpdatedSoundCalled != null)
            {
                PlayRoomUpdatedSoundCalled(this, new EventArgs());
            }
        }

		public override void ShowHint(List<Inline> inlines)
        {
            if (ShowInformativeMessageWithInlinesCalled != null)
            {
                ShowInformativeMessageWithInlinesCalled(this, new EventArgs<List<Inline>>(inlines));
            }
        }

    }
}
