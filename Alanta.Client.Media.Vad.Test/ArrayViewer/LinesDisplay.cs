using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ASV
{
	public class ArrayViewerLine
	{
		public readonly double startPosition, startValue;
		public readonly double endPosition, endValue;
		public readonly Color color;
		public readonly int width;
		public ArrayViewerLine(double startPosition, double startValue, double endPosition, double endValue, Color color)
			: this(startPosition, startValue, endPosition, endValue, color, 1)
		{
		}
		public ArrayViewerLine(double startPosition, double startValue, double endPosition, double endValue, Color color, int width)
		{
			this.startPosition = startPosition;
			this.startValue = startValue;
			this.endPosition = endPosition;
			this.endValue = endValue;
			this.color = color;
			this.width = width;
		}
	}
	public class ArrayViewerLines : IArrayViewerLines
	{
		private readonly List<ArrayViewerLine> arrows;
		public ArrayViewerLines(List<ArrayViewerLine> arrows)
		{
			this.arrows = arrows;
		}

		int IArrayViewerLines.Count
		{
			get { return arrows.Count; }
		}
		void IArrayViewerLines.GetLine(int index, out double startPosition, out double startValue, 
			out double endPosition, out double endValue, out Color color, out int width)
		{
			ArrayViewerLine arrow = arrows[index];
			startPosition = arrow.startPosition;
			startValue = arrow.startValue;
			endPosition = arrow.endPosition;
			endValue = arrow.endValue;
			color = arrow.color;
			width = arrow.width;
		}
	}
	public interface IArrayViewerLines
	{
		int Count
		{
			get;
		}
		void GetLine(int index,
			out double startPosition, out double startValue,
			out double endPosition, out double endValue,
			out Color color, out int width
			);
	}
	public class ArrayViewerLinesDisplay : ArrayViewer.Array
	{
		private readonly IArrayViewerLines lines;
		private readonly ArrayViewer.IScalarTransformScale transformScale;
		public ArrayViewerLinesDisplay(ArrayViewer.IScalarTransformScale transformScale, IArrayViewerLines lines)
		{
			this.lines = lines;
			this.transformScale = transformScale;
		}

		private ArrayViewer.ZoomScaleCalculator1D GetScaleCalculator(Rectangle area)
		{
			return new ArrayViewer.ZoomScaleCalculator1D(arrayViewer.ZoomScale, area);
		}

		public override void Paint(Graphics g, Rectangle freeArea, ref bool needClearBackground)
		{
			ArrayViewer.ZoomScaleCalculator1D sc = GetScaleCalculator(freeArea);
			for (int i = 0; i < lines.Count; i++)
			{
				double startPosition, startValue, endPosition, endValue;
				Color arrowColor; int arrowWidth;
				lines.GetLine(i, out startPosition, out startValue, out endPosition, out endValue, out arrowColor, out arrowWidth);

				int startX;			
				bool startIsVisible = sc.PositionToArea(startPosition, out startX);
				int endX;
				bool endIsVisible = sc.PositionToArea(endPosition, out endX);

				if (!startIsVisible && !endIsVisible) continue;

				int startY = freeArea.Bottom - Convert.ToInt32(transformScale.Transform(startValue) * (freeArea.Bottom - freeArea.Top));
				int endY = freeArea.Bottom - Convert.ToInt32(transformScale.Transform(endValue) * (freeArea.Bottom - freeArea.Top));

				if (!startIsVisible)
				{
					startY = endY;
					startX = freeArea.Left;
				}
				if (!endIsVisible)
				{
					endY = startY;
					endX = freeArea.Right;
				}

				PaintLine(startX, startY, endX, endY, g, arrowColor, arrowWidth);
			}
			//lastPaintArea = freeArea;
		}
		private void PaintLine(int startX, int startY, int endX, int endY, Graphics g, Color arrowColor, int arrowWidth)
		{
			Pen pen = new Pen(arrowColor, arrowWidth);
			g.DrawLine(pen, startX, startY, endX, endY);
			pen.Dispose();
		}
		/*
		private void PaintElement(int index)
		{
			int x;
			if (!GetScaleCalculator(lastPaintArea).PositionToArea(values.GetPosition(index), out x)) return;
			Graphics g = owner.CreateGraphics();
			PaintElement(index, x, g, lastPaintArea);
			g.Dispose();
		}
		*/
	}
}
