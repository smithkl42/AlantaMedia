// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System;
using System.Collections.Generic;
using System.Text;

namespace Alanta.Client.Media.Jpeg
{
	public class JpegHeader
	{
		public byte Marker;
		public byte[] Data;
		internal bool IsJFIF;
		public new string ToString { get { return Encoding.UTF8.GetString(Data, 0, Data.Length); } }
	}

	public class DecodedJpeg
	{
		#region Fields and Properties

		public Image Image { get; private set; }

		internal int[] BlockWidth;
		internal int[] BlockHeight;

		internal int Precision = 8;

		private byte[] hSampFactor =  FrameDefaults.HSampFactor;
		public byte[] HSampFactor
		{
			get { return hSampFactor; }
			set { hSampFactor = value; }
		}

		private byte[] vSampFactor = FrameDefaults.VSampFactor;
		public byte[] VSampFactor
		{
			get { return vSampFactor; }
			set { vSampFactor = value; }
		}

		internal bool[] lastColumnIsDummy = new[] { false, false, false };
		internal bool[] lastRowIsDummy = new[] { false, false, false };

		internal int[] compWidth, compHeight;
		internal int MaxHsampFactor;
		internal int MaxVsampFactor;

		public bool HasJFIF { get; private set; }

		private List<JpegHeader> _metaHeaders;

		public IList<JpegHeader> MetaHeaders
		{
			get 
			{
				if (_metaHeaders == null)
				{
					_metaHeaders = new List<JpegHeader>();
				}
				return _metaHeaders.AsReadOnly(); 
			}
		}

		#endregion

		public DecodedJpeg(Image image, IEnumerable<JpegHeader> metaHeaders)
		{
			Image = image;

			// Handles null as an empty list
			_metaHeaders = (metaHeaders == null) ?
				new List<JpegHeader>(0) : new List<JpegHeader>(metaHeaders);

			// Check if the JFIF header was present
			foreach (JpegHeader h in _metaHeaders)
				if (h.IsJFIF) { HasJFIF = true; break; }

			int components = Image.ComponentCount;

			compWidth = new int[components];
			compHeight = new int[components];
			BlockWidth = new int[components];
			BlockHeight = new int[components];

			Initialize();
		}

		public DecodedJpeg(Image image)
			: this(image, null)
		{
			// ks 3/08/10 - Removed as a minor optimization.
			//_metaHeaders = new List<JpegHeader>();
			//string comment = "Jpeg Codec | fluxcapacity.net ";
			//_metaHeaders.Add( 
			//    new JpegHeader() {
			//         Marker = JpegMarker.COM,
			//         Data = System.Text.Encoding.UTF8.GetBytes(comment)
			//    }
			//);
		}

		/// <summary>
		/// This method creates and fills three arrays, Y, Cb, and Cr using the input image.
		/// </summary>
		private void Initialize()
		{
			int w = Image.Width, h = Image.Height;

			int y;

			MaxHsampFactor = 1;
			MaxVsampFactor = 1;

			for (y = 0; y < Image.ComponentCount; y++)
			{
				MaxHsampFactor = Math.Max(MaxHsampFactor, hSampFactor[y]);
				MaxVsampFactor = Math.Max(MaxVsampFactor, vSampFactor[y]);
			}
			for (y = 0; y < Image.ComponentCount; y++)
			{
				compWidth[y] = (((w % 8 != 0) ? ((int)Math.Ceiling(w / 8.0)) * 8 : w) / MaxHsampFactor) * hSampFactor[y];
				if (compWidth[y] != ((w / MaxHsampFactor) * hSampFactor[y]))
				{
					lastColumnIsDummy[y] = true;
				}

				// results in a multiple of 8 for compWidthz
				// this will make the rest of the program fail for the unlikely
				// event that someone tries to compress an 16 x 16 pixel image
				// which would of course be worse than pointless

				BlockWidth[y] = (int)Math.Ceiling(compWidth[y] / 8.0);
				compHeight[y] = (((h % 8 != 0) ? ((int)Math.Ceiling(h / 8.0)) * 8 : h) / MaxVsampFactor) * vSampFactor[y];
				if (compHeight[y] != ((h / MaxVsampFactor) * vSampFactor[y]))
				{
					lastRowIsDummy[y] = true;
				}

				BlockHeight[y] = (int)Math.Ceiling(compHeight[y] / 8.0);
			}
		}

	}

}