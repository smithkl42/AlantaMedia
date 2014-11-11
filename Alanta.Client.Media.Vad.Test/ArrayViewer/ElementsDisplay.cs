using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace ASV
{
	public class ArrayViewerElementsDisplay1D : ArrayViewer.Array
	{
		#region variables
		private readonly bool updateTransformScale = false;
		private readonly double transformScaleMaxMultiplier;
		private readonly ArrayViewer.IScalarTransformScale transformScale;
		private readonly ArrayViewer.INonUniformScalarValues values;
		public bool enableHiliting = true;

		private Pen regularPen;
		public Pen RegularPen
		{
			get
			{
				return regularPen;
			}
			set
			{
				regularPen.Dispose();
				regularPen = value;
				if (arrayViewer != null) arrayViewer.Paint();
			}
		}
		private Pen hilitePen;
		public Pen HilitePen
		{
			get
			{
				return hilitePen;
			}
			set
			{
				if (arrayViewer == null) throw new Exception();
				hilitePen.Dispose();
				hilitePen = value;
				arrayViewer.Paint();
			}
		}
		
		#region events
		public delegate void ElementEventHandler(int index);
		public delegate void ElementKeyEventHandler(int index, KeyEventArgs e);
		public event ElementEventHandler ElementSelected = null;
		public event ElementEventHandler ElementDoubleClicked = null;
		public event ElementKeyEventHandler ElementKeyDown = null;
		#endregion
		#endregion
		public ArrayViewerElementsDisplay1D(ArrayViewer.IScalarTransformScale transformScale, ArrayViewer.INonUniformScalarValues values)
		{
			this.values = values;
			this.transformScale = transformScale;
			regularPen = new Pen(Color.DarkBlue, 2);
			hilitePen = new Pen(Color.Red, 2);
		}
		public ArrayViewerElementsDisplay1D(ArrayViewer.INonUniformScalarValues values, double transformScaleMaxMultiplier)
			: this(new ArrayViewer.LinearScalarTransformScale(), values)
		{
			updateTransformScale = true;
			this.transformScaleMaxMultiplier = transformScaleMaxMultiplier;
		}
		public ArrayViewerElementsDisplay1D(List<ArrayViewer.NonUniformScalarValue> elements, Color color)
		{
			this.values = new ArrayViewer.NonUniformScalarValues(elements.ToArray());
			this.transformScale = new ArrayViewer.LinearScalarTransformScale(values);
			enableHiliting = false;
			regularPen = new Pen(color, 1);
			hilitePen = null;
		}
		private Rectangle lastPaintArea;
		private int? lastHilitedIndex = null;
		public override void Paint(Graphics g, Rectangle freeArea, ref bool needClearBackground)
		{
			ArrayViewer.ZoomScaleCalculator1D sc = GetScaleCalculator(freeArea);
			int? lastElementX = null;
			if (updateTransformScale)
			{
				double max = values[0];
				double min = values[0];
				for (int i = 0; i < values.Count; i++)
				{
					int x;
					if (sc.PositionToArea(values.GetPosition(i), out x))
					{
						double value = values[i];
						if (value > max) max = value;
						else if (value < min) min = value;
					}
				}
				transformScale.Set(min, max * transformScaleMaxMultiplier);
			}
			for (int i = 0; i < values.Count; i++)
			{
				int x;
				if (sc.PositionToArea(values.GetPosition(i), out x))
				{
					if (lastElementX != null && lastElementX.Value == x) continue;
					PaintElement(i, x, g, freeArea);
					lastElementX = x;
				}
			}
			lastPaintArea = freeArea;
		}
		private void PaintElement(int index, int x, Graphics g, Rectangle area)
		{
			bool doHilite = (enableHiliting && lastHilitedIndex != null && lastHilitedIndex.Value == index);
			Pen pen = doHilite ? hilitePen : regularPen;
			int y = area.Bottom - Convert.ToInt32(transformScale.Transform(values[index]) * (area.Bottom - area.Top));
			g.DrawLine(pen, x, area.Bottom, x, y);
		}
		private void PaintElement(int index)
		{
			int x;
			if (!GetScaleCalculator(lastPaintArea).PositionToArea(values.GetPosition(index), out x)) return;
			Graphics g = arrayViewer.CreateGraphics();
			PaintElement(index, x, g, lastPaintArea);
			g.Dispose();
		}
		private ArrayViewer.ZoomScaleCalculator1D GetScaleCalculator(Rectangle area)
		{
			return new ArrayViewer.ZoomScaleCalculator1D(arrayViewer.ZoomScale, values, area, Convert.ToInt32(regularPen.Width));
		}
		private bool GetDistance(MouseEventArgs e, int index, ArrayViewer.ZoomScaleCalculator1D sc, out int? distance) // [pixels]
		{
			distance = null;
			int x;
			if (!sc.PositionToArea(values.GetPosition(index), out x)) return false;
			distance = Math.Abs(x - e.X);
			return true;
		}
		public override void MouseDoubleClick(MouseEventArgs e)
		{
			int? hoverIndex = GetHoverIndex(e);
			if (hoverIndex != null && ElementDoubleClicked != null)
				ElementDoubleClicked(hoverIndex.Value);
		}
		public override bool MouseMove(MouseEventArgs e)
		{
			int? hoverIndex = GetHoverIndex(e);
			if (hoverIndex != null)
				if (hoverIndex != lastHilitedIndex)
				{
					int? oldHilitedIndex = lastHilitedIndex;
					lastHilitedIndex = hoverIndex;
					if (enableHiliting) PaintElement(hoverIndex.Value);
					if (oldHilitedIndex != null && enableHiliting) PaintElement(oldHilitedIndex.Value);
					if (ElementSelected != null) ElementSelected(hoverIndex.Value);
				}
			return false;
		}
		public override void KeyDown(KeyEventArgs e)
		{
			if (ElementKeyDown != null && lastHilitedIndex != null)
				ElementKeyDown(lastHilitedIndex.Value, e);
		}
		private int? GetHoverIndex(MouseEventArgs e)
		{
			ArrayViewer.ZoomScaleCalculator1D sc = GetScaleCalculator(lastPaintArea);

			int? minDistance = null;
			int? minDistanceIndex = null;
			for (int i = 0; i < values.Count; i++)
			{
				int? distance;
				if (GetDistance(e, i, sc, out distance))
				{
					if (minDistance == null)
					{
						minDistance = distance;
						minDistanceIndex = i;
					}
					else
						if (distance.Value < minDistance.Value)
						{
							minDistance = distance;
							minDistanceIndex = i;
						}
				}
			}

			return minDistanceIndex;
		}
	}	
}
