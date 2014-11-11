using System;
using System.Xml.Linq;

namespace Alanta.Client.Media
{
    public abstract class MediaCommand
    {
        protected MediaCommand(string name)
        {
            Name = name;
            byte[] guid = Guid.NewGuid().ToByteArray();
            char[] base64data =  new char[(int)(Math.Ceiling((double)guid.Length / 3) * 4)];
            Convert.ToBase64CharArray(guid, 0, guid.Length, base64data, 0);
            ID = new string(base64data);
        }

        public virtual XElement GetXElement()
        {
            return new XElement(Name,
                 new XAttribute("Id", ID));
        }

        public string Name { get; private set; }
        public string ID { get; private set; }
        public string Result { get; set; }

    }
}
