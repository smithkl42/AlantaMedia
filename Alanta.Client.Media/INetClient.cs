using System;
namespace Alanta.Client.Media
{
    public interface INetClient
    {
        void Connect(Action<Exception> processConnection, Action<byte[], int, int> processReceivedPacket);
        void Disconnect();
        bool IsConnected { get; }
        void Send(string message);
        void Send(byte[] packet);
        void Send(byte[] packet, int offset, int length);
        string Host {get;}
        int Port {get;}
        //void SetReceiveBuffer(byte[] buffer, int offset);
        //void SetReceiveBuffer(ByteStream buffer);
    }
}
