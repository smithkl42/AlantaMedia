using System.Xml.Linq;

namespace Alanta.Client.Media
{
    public class MediaCommandAddClient : MediaCommand
    {
        public MediaCommandAddClient(string roomId, string clientSsrc)
            : base("AddClient")
        {
            RoomId = roomId;
            ClientSsrc = clientSsrc;
        }

        public override XElement GetXElement()
        {
            XElement element = base.GetXElement();
            element.Add(new XAttribute("RoomId", RoomId));
            element.Add(new XAttribute("ClientSsrc", ClientSsrc));
            return element;
        }

        public string RoomId { get; private set; }
        public string ClientSsrc { get; private set; }

    }
}
