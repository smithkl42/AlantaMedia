using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Alanta.Client.Media
{
    public class MediaCommandSet : List<MediaCommand>
    {
        public override string ToString()
        {
            var commandElement = new XElement("Commands");
            ForEach(c => commandElement.Add(c.GetXElement()));
            return commandElement.ToString();
        }

        public void ParseResult(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("The RTP control message to be parsed was empty.");
            }
            XDocument doc = XDocument.Parse(message);
            if (doc == null)
            {
                throw new ArgumentException("Unable to parse the XML message returned from the server.");
            }
            var responses = doc.Descendants("Response");
            foreach (XElement response in responses)
            {
                MediaCommand command = this.FirstOrDefault(c => c.ID == response.Attribute("Id").Value);
                if (command != null)
                {
                    command.Result = response.Attribute("Result").Value;
                }
                else
                {
                    throw new InvalidOperationException(string.Format("The response ID {0} was not recognized.", response.Attribute("Id").Value));
                }
            }
        }

    }
}
