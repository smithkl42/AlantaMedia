using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ASV
{
	public class ArrayViewerArray2D: ArrayViewer.Array
	{
		private readonly ArrayViewer.IScalarTransformScale transformScale;
		private readonly ArrayViewer.IScalarValues2D values;
		private readonly Bitmap bitmap;
		public ArrayViewerArray2D(ArrayViewer.IScalarTransformScale transformScale, ArrayViewer.IScalarValues2D values)
		{
			this.transformScale = transformScale;
			this.values = values;
			bitmap = new Bitmap(values.CountX, values.CountY);
			RefreshBitmapFromValues();
		}
		private void RefreshBitmapFromValues()
		{
			for (int x = 0; x < values.CountX; x++)
				for (int y = 0; y < values.CountY; y++)
					bitmap.SetPixel(x, y, GetPixelColor(x, y));
		}
		private Color GetPixelColor(int x, int y)
		{
			int r, g, b;
			double value = values[x, y];
			value = transformScale.Transform(value);
			r = g = b = Convert.ToInt32(value * 255);
			if (r < 0) r = 0; else if (r > 255) r = 255;
			if (g < 0) g = 0; else if (g > 255) g = 255;
			if (b < 0) b = 0; else if (b > 255) b = 255;
			return Color.FromArgb(r, g, b);
		}
		public override bool CanClearBackground
		{
			get
			{
				return true;
			}
		}
		public override void Paint(Graphics g, Rectangle freeArea, ref bool needClearBackground)
		{
			needClearBackground = false;
			float x = (float)values.CountX * (float)arrayViewer.ZoomScale.Left;
			float width = (float)values.CountX * (float)(arrayViewer.ZoomScale.Right - arrayViewer.ZoomScale.Left);
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			g.DrawImage(bitmap, freeArea, x, -0.5F, width, (float)values.CountY, GraphicsUnit.Pixel);
			

		}
	}
}
