using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace ASV
{
	public class ArrayViewerScalarTransformScaleDisplay1D : ArrayViewer.Array
	{
		private readonly ArrayViewer.IScalarTransformScale transformScale;
		public int verticalWidth;
		public int horizontalHeight;
		private Pen gridPen;
		private Font textFont;
		private Brush textBrush;
		private int horizontalScaleElementWidth = 85;

		public delegate string GetHorisontalLabelTextHandler(double position);
		public GetHorisontalLabelTextHandler getHorisontalLabelText;

		public ArrayViewerScalarTransformScaleDisplay1D(ArrayViewer.IScalarTransformScale transformScale)
		{
			this.getHorisontalLabelText = new GetHorisontalLabelTextHandler(GetHorisontalLabelText);
			this.transformScale = transformScale;
			this.horizontalHeight = 30;
			this.verticalWidth = 60;
			this.gridPen = new Pen(Color.LightGray);
			this.textFont = new Font("lucida console", 9);
			this.textBrush = new SolidBrush(Color.DarkViolet);
		}
		public override Rectangle GetFreeArea(Rectangle initialFreeArea)
		{
			return new Rectangle(initialFreeArea.Left + verticalWidth, initialFreeArea.Top, initialFreeArea.Width - verticalWidth, initialFreeArea.Height - horizontalHeight);
		}
		public override void Paint(Graphics g, Rectangle freeArea, ref bool needClearBackground)
		{
			if (verticalWidth != 0)
			{
				int count = (freeArea.Height - horizontalHeight) / (textFont.Height);
				transformScale.Delimit(delegate(double value)
				{
					PaintVerticalScaleDelimitedElement(value, g, freeArea);
				}, count);
			}
			if (horizontalHeight != 0)
			{
				int count = (freeArea.Width - verticalWidth) / horizontalScaleElementWidth;
				ArrayViewer.IntervalDelimiter.Delimit(arrayViewer.ZoomScale.Left, arrayViewer.ZoomScale.Right, count, delegate(double position)
				{
					PaintHorizontalScaleDelimitedElement(position, g, freeArea);
				});
			}
		}
		private void PaintVerticalScaleDelimitedElement(double value, Graphics g, Rectangle freeArea)
		{
			int y = Convert.ToInt32((1 - transformScale.Transform(value)) * (freeArea.Height - horizontalHeight));
			g.DrawLine(gridPen, freeArea.Left + verticalWidth, y, freeArea.Right, y);

			RectangleF textRectangle = new RectangleF(freeArea.Left, y, verticalWidth, 0);
			textRectangle.Inflate(0, textFont.Height / 2);
			string text = Convert.ToString(value);
			StringFormat stringFormat = new StringFormat(StringFormatFlags.NoWrap);
			stringFormat.Alignment = StringAlignment.Far;
			g.DrawString(text, textFont, textBrush, textRectangle, stringFormat);
		}
		private void PaintHorizontalScaleDelimitedElement(double position, Graphics g, Rectangle freeArea)
		{
			int x = verticalWidth + Convert.ToInt32((position - arrayViewer.ZoomScale.Left) / (arrayViewer.ZoomScale.Right - arrayViewer.ZoomScale.Left) * (freeArea.Width - verticalWidth));
			g.DrawLine(gridPen, x, freeArea.Bottom - horizontalHeight, x, freeArea.Top);

			// lastHorizontalScaleDelimitedElementRight

			RectangleF textRectangle = new RectangleF(x, freeArea.Bottom - horizontalHeight, 0, horizontalHeight);
			textRectangle.Inflate(horizontalScaleElementWidth / 2 - 1, 0);
			string text = getHorisontalLabelText(position);
			StringFormat stringFormat = new StringFormat(StringFormatFlags.NoClip);
			stringFormat.Alignment = StringAlignment.Center;
			//stringFormat.Alignment = StringAlignment.Far;
			g.DrawString(text, textFont, textBrush, textRectangle, stringFormat);
		}
		private string GetHorisontalLabelText(double position)
		{
			return Convert.ToString(position);
		}
	}
}
