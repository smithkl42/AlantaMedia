using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace ASV
{
	public abstract class ArrayViewerNonUniformContinousArray1D : ArrayViewer.Array
	{
		#region variables
		private readonly bool useRightMouseToEditSmoothDistance;
		private bool changingSmoothValue = false;
		private Timer changeTimer = null;
		private Point cursorPositionBeforeChanging;
		protected double smoothDistance;
		private readonly ArrayViewer.INonUniformScalarValues values;
		private readonly ArrayViewer.IScalarTransformScale transformScale;
		public ArrayViewer.IScalarTransformScale TransformScale
		{
			get { return transformScale; }
		}
		private Pen arrayPen;
		public Pen ArrayPen
		{
			get
			{
				return arrayPen;
			}
			set
			{
				arrayPen.Dispose();
				arrayPen = value;
			}
		}
		public Color ArrayPenColor
		{
			get
			{
				return arrayPen.Color;
			}
			set
			{
				arrayPen.Color = value;
			}
		}
		#endregion
		/// <param name="transformScale">nullable (initializes own transform scale if null)</param>
		public ArrayViewerNonUniformContinousArray1D(ArrayViewer.INonUniformScalarValues values, ArrayViewer.IScalarTransformScale transformScale, bool useRightMouseToEditSmoothDistance)
		{
			this.useRightMouseToEditSmoothDistance = useRightMouseToEditSmoothDistance;
			this.values = values;
			InitializeSmoothDistance();
			this.transformScale = transformScale != null ? transformScale : CreateTransformScale();
			this.arrayPen = new Pen(Color.DarkGreen, 1);
		}
		private void InitializeSmoothDistance()
		{
			const double smoothDistanceCoefficient = 1;
			double? minDistance = null;
			for (int i = 0; i < values.Count - 1; i++)
			{
				double distance = values.GetPosition(i + 1) - values.GetPosition(i);
				if (minDistance == null) minDistance = distance;
				else if (distance < minDistance.Value) minDistance = distance;
			}
			smoothDistance = smoothDistanceCoefficient * minDistance.Value;
		}
		private ArrayViewer.IScalarTransformScale CreateTransformScale()
		{
			const int measuresCount = 1000;
			double max, min;
			double pos = values.GetPosition(0);
			double step = (values.GetPosition(values.Count - 1) - pos) / values.Count;
			max = min = GetValueAtPosition(pos);
			for (int i = 1; i < measuresCount; i++)
			{
				pos += step;
				double v = GetValueAtPosition(pos);
				if (v > max) max = v;
				if (v < min) min = v;
			}
			double diff = max - min;
			if (max == min) min -= 1;
			return new ArrayViewer.LinearScalarTransformScale(min, max);
		}
		private double GetValueAtPosition(double position) // convolution of all elements
		{
			double r = 0;
			for (int i = 0; i < values.Count; i++)
				r += values[i] * SpreadFunction(position - values.GetPosition(i));
			return r;
		}
		public override void Paint(Graphics g, Rectangle freeArea, ref bool needClearBackground)
		{
			ArrayViewer.ZoomScaleCalculator1D sc = new ArrayViewer.ZoomScaleCalculator1D(arrayViewer.ZoomScale, values, freeArea, 0);
			int? lastY = null;
			for (int x = freeArea.Left; x < freeArea.Right; x++)
			{
				double pos = sc.AreaToPosition(x);
				double v = GetValueAtPosition(pos);
				v = transformScale.Transform(v);
				int y = freeArea.Bottom - Convert.ToInt32(v * (freeArea.Bottom - freeArea.Top));
				if (lastY != null) g.DrawLine(arrayPen, x - 1, lastY.Value, x, y);
				lastY = y;
			}
		}
		protected abstract double SpreadFunction(double distance);
		#region smooth value change
		public override bool MouseDown(MouseEventArgs e)
		{
			if (useRightMouseToEditSmoothDistance && e.Button == MouseButtons.Right && !changingSmoothValue)
			{
				arrayViewer.Capture = true;
				changingSmoothValue = true;
				cursorPositionBeforeChanging = Cursor.Position;
				Cursor.Hide();
				Cursor.Hide();
				changeTimer = new Timer();
				changeTimer.Tick += new EventHandler(changeTimer_Tick);
				changeTimer.Interval = 50;
				changeTimer.Start();
				return true;
			}
			return false;
		}
		public override bool MouseMove(MouseEventArgs e)
		{
			return changingSmoothValue;
		}
		public override void MouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && changingSmoothValue)
			{
				arrayViewer.Capture = false;
				AfterChangingProcedure();
			}
		}
		public override void MouseCaptureChanged(EventArgs e)
		{
			if (changingSmoothValue)
				AfterChangingProcedure();
		}
		private void AfterChangingProcedure()
		{
			changingSmoothValue = false;
			Cursor.Show();
			changeTimer.Stop();
			changeTimer.Dispose();
		}
		private void changeTimer_Tick(object sender, EventArgs e)
		{
			if (Cursor.Position != cursorPositionBeforeChanging)
			{
				Point difference = new Point(Cursor.Position.X - cursorPositionBeforeChanging.X, Cursor.Position.Y - cursorPositionBeforeChanging.Y);
				Cursor.Position = cursorPositionBeforeChanging;
				ProcessChangeSmoothDistance(difference);
			}
		}
		private void ProcessChangeSmoothDistance(Point difference)
		{
			const double mouseChangeCoefficient = 0.01;
			double change = Convert.ToDouble(-difference.Y) * mouseChangeCoefficient + 1;
			if (change < 0.5) change = 0.5;

			smoothDistance *= change;


			arrayViewer.Paint();
		}
		#endregion
	}
	public class ArrayViewerNonUniformSmoothArray1D : ArrayViewerNonUniformContinousArray1D
	{
		public ArrayViewerNonUniformSmoothArray1D(ArrayViewer.INonUniformScalarValues values, ArrayViewer.IScalarTransformScale transformScale, bool useRightMouseToEditSmoothDistance)
			: base(values, transformScale, useRightMouseToEditSmoothDistance)
		{
		}
		protected override double SpreadFunction(double distance)
		{
			if (distance > smoothDistance || distance < -smoothDistance) return 0;
			else
			{
				double r = 0.5 + 0.5 * Math.Cos(distance * Math.PI / smoothDistance);
				return r / smoothDistance;
			}
		}
	}
	public class ArrayViewerNonUniformIntegralArray1D : ArrayViewerNonUniformContinousArray1D
	{
		public ArrayViewerNonUniformIntegralArray1D(ArrayViewer.INonUniformScalarValues values, ArrayViewer.IScalarTransformScale transformScale, bool useRightMouseToEditSmoothDistance)
			: base(values, transformScale, useRightMouseToEditSmoothDistance)
		{
		}
		protected override double SpreadFunction(double distance)
		{
			if (distance > smoothDistance) return 1;
			if (distance < -smoothDistance) return 0;
			return 0.5 + 0.5 * Math.Sin(distance * Math.PI / 2 / smoothDistance);
		}
	}
}
