using System.Collections.Generic;
using System.Windows.Documents;

namespace Alanta.Client.Common.Loader
{
    public interface IMessageService
    {
        void ShowErrorMessage(string message);
        void ShowMessage(string message);
        void ShowErrorHint(string message);
        void ShowHint(string message);
        void ShowHint(List<Inline> inlines);
        void PlayChatMessageSentSound();
        void PlayChatMessageReceivedSound();
        void PlayRoomUpdatedSound();
    	void PlayModerationRequestSound(bool repeat = false);
    	void StopModerationRequestSound();
    }
}
