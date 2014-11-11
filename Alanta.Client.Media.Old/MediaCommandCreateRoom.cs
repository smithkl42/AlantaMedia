using System.Xml.Linq;

namespace Alanta.Client.Media
{
    public class MediaCommandCreateRoom : MediaCommand
    {
        public MediaCommandCreateRoom(string roomId)
            : base("CreateRoom")
        {
            RoomId = roomId;
        }

        public override XElement GetXElement()
        {
            XElement element = base.GetXElement();
            element.Add(new XAttribute("RoomId", RoomId));
            return element;
        }

        public string RoomId { get; private set; }

    }
}
