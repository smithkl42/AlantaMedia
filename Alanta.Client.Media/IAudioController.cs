using System;
using System.IO;
namespace Alanta.Client.Media
{
    public interface IAudioController
    {
        void GetNextAudioFrame(Action<MemoryStream> callback);
        void SubmitRecordedFrame(AudioContext audioContext, byte[] frame);
        bool IsConnected { get; }
    }
}
