// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System.Collections.Generic;
using System.Linq;

namespace Alanta.Client.Media.Jpeg.Decoder
{
    public class JpegScan
    {
        private readonly List<JpegComponent> components = new List<JpegComponent>();
        public IList<JpegComponent> Components { get { return components.AsReadOnly(); } }

    	public JpegScan()
    	{
    		MaxV = 0;
    		MaxH = 0;
    	}

    	internal int MaxH { get; private set; }
    	internal int MaxV { get; private set; }

    	public void AddComponent(byte id, byte factorHorizontal, byte factorVertical, byte quantizationId, byte colorMode)
        {
            var component = new JpegComponent(this, id, factorHorizontal, factorVertical, quantizationId, colorMode);

            components.Add(component);

            // Defined in Annex A
            MaxH = components.Max(x => x.factorH);
            MaxV = components.Max(x => x.factorV);
        }

        public void AddComponent(byte id, byte factorHorizontal, byte factorVertical, JpegQuantizationTable jpegQuantizationTable, byte colorMode)
        {
            var component = new JpegComponent(this, id, factorHorizontal, factorVertical, jpegQuantizationTable, colorMode);
            component.Decode = component.DecodeBaseline; // This is a default for the JpegFrameDecoder.
            components.Add(component);

            // Defined in Annex A
            MaxH = components.Max(x => x.factorH);
            MaxV = components.Max(x => x.factorV);
        }

        public JpegComponent GetComponentById(byte id)
        {
            return components.First(x => x.componentId == id);
        }
    }
}
