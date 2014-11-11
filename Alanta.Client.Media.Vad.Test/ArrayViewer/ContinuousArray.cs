using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ASV
{
	public class ArrayViewerContinousArray1D : ArrayViewer.Array
	{
		private class ScaleCalculator1D
		{
			private readonly ArrayViewerGenericZoomScale scale;
			private readonly int valuesCount;
			private readonly Rectangle area;
			public ScaleCalculator1D(ArrayViewerGenericZoomScale scale, int valuesCount, Rectangle area)
			{
				if (valuesCount == 0) throw new Exception();
				this.scale = scale;
				this.valuesCount = valuesCount;
				this.area = area;
			}
			public int AreaToIndex(int x)
			{
				double d = Convert.ToDouble(x - area.Left) / (area.Right - area.Left);
				d = scale.Left + (scale.Right - scale.Left) * d;
				int r = Convert.ToInt32(d * valuesCount);
				if (r < 0) return 0;
				else if (r >= valuesCount) return valuesCount - 1;
				else return r;
			}
		}

		public event EventHandler Hovered = null;
		bool hovered = false;
		private readonly ArrayViewer.IScalarTransformScale transformScale;
		readonly double? transformScaleUpdateLimit; readonly double? transformScaleMultiplier;
		public ArrayViewer.IScalarTransformScale TransformScale
		{
			get
			{
				return transformScale;
			}
		}
		private readonly ArrayViewer.IScalarValues values;
		private readonly string description;
		private readonly System.Windows.Forms.ToolTip descriptionTooltip;
		private Point[] arrayPoints;
		private Pen arrayPen;
		public Pen ArrayPen
		{
			get
			{
				return arrayPen;
			}
			set
			{
				//if (owner == null) throw new Exception();
				arrayPen.Dispose();
				arrayPen = value;
				//owner.Paint();
			}
		}
		private Pen backgroundPen;
		public Pen BackgroundPen
		{
			get
			{
				return backgroundPen;
			}
			set
			{
				if (arrayViewer == null) throw new Exception();
				backgroundPen.Dispose();
				backgroundPen = value;
				arrayViewer.Paint();
			}
		}
		public ArrayViewerContinousArray1D(ArrayViewer.IScalarValues values, double transformScaleMultiplier)
			: this(new ArrayViewer.LinearScalarTransformScale(), values, 0.0001)
		{
			this.transformScaleMultiplier = transformScaleMultiplier;
		}
		public ArrayViewerContinousArray1D(ArrayViewer.IScalarTransformScale transformScale, ArrayViewer.IScalarValues values)
			: this(transformScale, values, null)
		{
		}
		public ArrayViewerContinousArray1D(ArrayViewer.IScalarTransformScale transformScale, ArrayViewer.IScalarValues values, double? transformScaleUpdateLimit)
			: this(transformScale, values, transformScaleUpdateLimit, null)
		{
		}
		public ArrayViewerContinousArray1D(ArrayViewer.IScalarTransformScale transformScale, ArrayViewer.IScalarValues values, double? transformScaleUpdateLimit, Pen pen)
			: this(transformScale, values, transformScaleUpdateLimit, pen, null)
		{
		}
		/// <param name="transformScaleUpdateLimit">used for automatic update of transform scale</param>
		public ArrayViewerContinousArray1D(ArrayViewer.IScalarTransformScale transformScale, ArrayViewer.IScalarValues values, double? transformScaleUpdateLimit, Pen pen, string description)
		{
			this.transformScaleUpdateLimit = transformScaleUpdateLimit;
			this.values = values;
			this.transformScale = transformScale;
			arrayPen = pen != null ? pen : new Pen(Color.DarkBlue, 1);
			backgroundPen = new Pen(Color.White, 1);
			this.description = description;
			if (description != null) descriptionTooltip = new System.Windows.Forms.ToolTip();
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
			int pointsCount = (freeArea.Right - freeArea.Left) * 2;
			if (pointsCount <= 0) return;
			this.arrayPoints = new Point[pointsCount];
			Point[] backgroundPoints1 = null, backgroundPoints2 = null;
			if (needClearBackground)
			{
				backgroundPoints1 = new Point[pointsCount];
				backgroundPoints2 = new Point[pointsCount];
			}
			int pointsIndex = 0;

			ScaleCalculator1D sc = new ScaleCalculator1D(arrayViewer.ZoomScale, values.Count, freeArea);
			#region update transform scale
			if (transformScaleUpdateLimit != null)
			{
				int startIndex = sc.AreaToIndex(freeArea.Left);
				int endIndex = sc.AreaToIndex(freeArea.Right);
				double max, min;
				max = min = values[startIndex];
				for (int i = startIndex; i < endIndex; i++)
				{
					double d = values[i];
					if (d > max) max = d;
					else if (d < min) min = d;
				}
				if (transformScaleMultiplier.HasValue)
				{
					transformScale.Set(min, max * transformScaleMultiplier.Value);
				}
				else
				{
					double med = (max + min) / 2;
					double diff = transformScaleUpdateLimit.Value + max - min;
					diff *= 1.2;
					transformScale.Set(med - diff / 2, med + diff / 2);
				}
			}
			#endregion
			int? lastEndIndex = null;
			int? lastYMin = null, lastYMax = null; // [pixels]
			for (int x = freeArea.Left; x < freeArea.Right; x++, pointsIndex += 2)
			{
				int startIndex = lastEndIndex == null ? sc.AreaToIndex(x) : lastEndIndex.Value;
				int endIndex = sc.AreaToIndex(x + 1); lastEndIndex = endIndex;

				// calculate peak values
				double min = values[startIndex];
				double max = values[endIndex];
				for (int index = startIndex + 1; index <= endIndex; index++)
				{
					if (values[index] > max) max = values[index];
					if (values[index] < min) min = values[index];
				}

				// convert
				int yMin = freeArea.Bottom - (Double.IsNaN(min) ? 0 : Convert.ToInt32(transformScale.Transform(min) * (freeArea.Bottom - freeArea.Top)));
				int yMax = freeArea.Bottom - (Double.IsNaN(max) ? 0 : Convert.ToInt32(transformScale.Transform(max) * (freeArea.Bottom - freeArea.Top)));
				//if (yMax == yMin) yMax--;

				// draw line


				//if (needClearBackground) g.DrawLine(backgroundPen, x, freeArea.Bottom, x, yMin);
				//g.DrawLine(arrayPen, x, yMin, x, yMax);
				//if (needClearBackground) g.DrawLine(backgroundPen, x, yMax, x, freeArea.Top);
				arrayPoints[pointsIndex].X = x;
				arrayPoints[pointsIndex].Y = yMin;
				arrayPoints[pointsIndex + 1].X = x;
				arrayPoints[pointsIndex + 1].Y = yMax;

				if (backgroundPoints1 != null)
				{
					backgroundPoints1[pointsIndex].X = x;
					backgroundPoints1[pointsIndex].Y = yMin + 1;
					backgroundPoints1[pointsIndex + 1].X = x;
					backgroundPoints1[pointsIndex + 1].Y = freeArea.Bottom;
				}

				if (backgroundPoints2 != null)
				{
					backgroundPoints2[pointsIndex].X = x;
					backgroundPoints2[pointsIndex].Y = yMax - 1;
					backgroundPoints2[pointsIndex + 1].X = x;
					backgroundPoints2[pointsIndex + 1].Y = freeArea.Top;
				}

				// draw link line
				/*if (lastYMax != null)
				{
					bool thereIsAlreadyLink = false;
					if (lastYMax.Value <= yMax && lastYMin.Value >= yMax) thereIsAlreadyLink = true;
					else if (lastYMax.Value <= yMin && lastYMin.Value >= yMin) thereIsAlreadyLink = true;
					else if (yMax <= lastYMin.Value && yMin >= lastYMin.Value) thereIsAlreadyLink = true;
					else if (yMax <= lastYMax.Value && yMin >= lastYMax.Value) thereIsAlreadyLink = true;
					if (!thereIsAlreadyLink) g.DrawLine(arrayPen, x, lastYMin.Value, x, yMin);
				}
				*/

				lastYMax = yMax;
				lastYMin = yMin;
			}

			if (needClearBackground)
			{
				//g.Clear(Color.White);
				g.DrawLines(backgroundPen, backgroundPoints1);
				g.DrawLines(backgroundPen, backgroundPoints2);
				needClearBackground = false;
			}
			g.DrawLines(arrayPen, arrayPoints);


		}
		public bool HitTest(int x, int y)
		{
			for (int i = 0; i < arrayPoints.Length / 2; i++)
			{
				Point start = arrayPoints[i * 2];
				Point end = arrayPoints[i * 2 + 1];
				if (x == start.X && y <= start.Y && y >= end.Y)
					return true;
			}
				
			return false;
		}
		public override bool MouseMove(System.Windows.Forms.MouseEventArgs e)
		{
			if (description != null)
			{
				if (HitTest(e.X, e.Y))
				{
					descriptionTooltip.Show(description, arrayViewer, e.X, e.Y, 2000);
					if (!hovered) if (Hovered != null) Hovered(this, new EventArgs());
					hovered = true;
					return true;
				}
				else hovered = false;
			}
			return false;
		}
	}
}
