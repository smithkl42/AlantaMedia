using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace ASV
{
	public partial class ArrayViewer : UserControl // main development on 0712(03-05)
	{
		#region interfaces and classes
		public class Exception : System.Exception
		{
			public Exception()
			{
			}
			public Exception(object message)
				: base(message.ToString())
			{
			}
			public Exception(string format, params object[] args)
				: base(String.Format(format, args))
			{
			}
		}
		public static class IntervalDelimiter
		{
			public delegate void Delimited(double value);
			public static void Delimit(double start, double end, int maximalCount, Delimited callback)
			{
				double[] discr = new double[4];
				discr[0] = 0.1; discr[1] = 0.2; discr[2] = 0.25; discr[3] = 0.5;
				
				if (Double.IsNaN(start) || Double.IsInfinity(start)
					|| Double.IsNaN(end) || Double.IsInfinity(end)) throw new Exception("invalid interval to delimit: ({0}, {1})", start, end);

				if (maximalCount <= 0) return;
				else if (maximalCount == 1)
				{
					callback((start + end) / 2);
					return;
				}

				// calculate step
				double step;
				double stepf = Math.Abs(start - end) / maximalCount;
				if (stepf > 1) 
				{
					double dec = 10;
					while (stepf > dec) dec *= 10;
					step = dec;
					for (int i = discr.Length - 1; i >= 0; i--)
					{
						double stept = dec * discr[i];
						if (stept < stepf) break;
						step = stept;
					}
				}
				else
				{
					double dec = 0.1;
					while (stepf < dec) dec *= 0.1; dec *= 10;
					step = dec;
					for (int i = discr.Length - 1; i >= 0; i--)
					{
						double stept = dec * discr[i];
						if (stept < stepf) break;
						step = stept;
					}
				}

				// calculate 1st value
				double val;
				int nval = (int)(start / step);
				val = step * (double)nval;
				
				// loop over interval
				if (end > start)
				{
					while (val < end)
					{
						if (val >= start) callback(val);
						val += step;
					}
				}
				else
				{
					while (val > end)
					{
						if (val <= start) callback(val);
						val -= step;
					}
				}
			}
		}
		public abstract class Array : IUserInputProcessor
		{
			protected ArrayViewer arrayViewer;
			protected ArrayViewerGenericZoomScale ZoomScale
			{
				get
				{
					return arrayViewer.ZoomScale;
				}
			}
			public virtual void AttachTo(ArrayViewer arrayViewer) // create your controls here
			{
				if (this.arrayViewer != null) throw new Exception();
				this.arrayViewer = arrayViewer;
			}
			public virtual void DetachFrom(ArrayViewer owner) // destroy your controls here
			{
				if (this.arrayViewer != owner) throw new Exception();
				this.arrayViewer = null;
			}
			public abstract void Paint(Graphics g, Rectangle freeArea, ref bool needClearBackground);
			public virtual void Resize(Rectangle freeArea)
			{
			}
			public virtual Rectangle GetFreeArea(Rectangle initialFreeArea)
			{
				return initialFreeArea;
			}
			public virtual bool CanClearBackground
			{
				get
				{
					return false;
				}
			}
			#region IUserInputProcessor Members
			public virtual void KeyDown(KeyEventArgs e)
			{
			}
			public virtual void MouseCaptureChanged(EventArgs e)
			{
			}
			public virtual bool MouseDown(MouseEventArgs e)
			{
				return false;
			}
			public virtual void MouseUp(MouseEventArgs e)
			{
			}
			public virtual void MouseDoubleClick(MouseEventArgs e)
			{
			}
			public virtual bool MouseMove(MouseEventArgs e)
			{
				return false;
			}
			public virtual void MouseLeave(EventArgs e)
			{
			}
			#endregion
		}
		public interface IUserInputProcessor
		{
			void KeyDown(KeyEventArgs e);
			void MouseCaptureChanged(EventArgs e);
			/// <returns>true to cancel processing of this event by another arrays</returns>
			bool MouseDown(MouseEventArgs e);
			void MouseUp(MouseEventArgs e);
			void MouseDoubleClick(MouseEventArgs e);
			/// <returns>true to cancel processing of this event by another arrays</returns>
			bool MouseMove(MouseEventArgs e);
			void MouseLeave(EventArgs e);
		}
		public interface ICursorPainter
		{
			void PaintCursor(Graphics g, Rectangle paintArea); // is called on timer
		}
		public class ZoomScaleCalculator1D
		{
			private readonly ArrayViewerGenericZoomScale scale;
			private readonly double startPosition, endPosition;
			private readonly Rectangle area;
			private readonly int elementPenWidth;
			public ZoomScaleCalculator1D(ArrayViewerGenericZoomScale scale, INonUniformScalarValues values, Rectangle area, int elementPenWidth)
			{
				if (values.Count == 0) throw new Exception();
				this.scale = scale;
				this.startPosition = values.GetPosition(0);
				this.endPosition = values.GetPosition(values.Count - 1);
				this.area = area;
				this.elementPenWidth = elementPenWidth;
			}
			public ZoomScaleCalculator1D(ArrayViewerGenericZoomScale scale, Rectangle area)
			{
				this.scale = scale;
				this.startPosition = 0;
				this.endPosition = 1;
				this.area = area;
				this.elementPenWidth = 0;
			}
			public bool PositionToArea(double position, out int x)
			{
				x = 0;
				if (endPosition == startPosition) return false;
				double d = (position - startPosition) / (endPosition - startPosition);
				if (d < scale.Left || d > scale.Right) return false;
				d = (d - scale.Left) / (scale.Right - scale.Left);
				x = area.Left + elementPenWidth / 2 + Convert.ToInt32(d * (area.Right - area.Left - elementPenWidth));
				return true;
			}
			public double AreaToPosition(int x)
			{
				double d = Convert.ToDouble(x - area.Left) / (area.Right - area.Left);
				d = scale.Left + (scale.Right - scale.Left) * d;
				return startPosition + (endPosition - startPosition) * d;
			}
		}
		public interface IScalarValues
		{
			double this[int index]
			{
				get;
			}
			int Count
			{
				get;
			}
		}
		public interface IScalarValues2D
		{
			double this[int x, int y]
			{
				get;
			}
			int CountX
			{
				get;
			}
			int CountY
			{
				get;
			}
		}
		public interface INonUniformScalarValues : IScalarValues // values must be sorted by position 
		{
			/// <summary>
			/// returned value is transformed using ScaleCalculatorNonUniform1D
			/// </summary>
			/// <param name="index"></param>
			/// <returns></returns>
			double GetPosition(int index);
		}
		public interface IScalarTransformScale
		{
			double Transform(double value/*[valueUnit]*/); // return value range [0;1]
			void Delimit(IntervalDelimiter.Delimited callback, // value arguement is in [valueUnit]
				int count
				);
			void Set(double min, double max);
		}
		#endregion
		#region standard transform scales
		public class LinearScalarTransformScale : IScalarTransformScale
		{
			public double min, max;
			public double Transform(double value)
			{
				if (max == min) return 0;
				else return (value - min) / (max - min);
			}
			public LinearScalarTransformScale()
			{
			}
			public LinearScalarTransformScale(double min, double max)
			{
				/*
		const double excessScaleCoefficient = 1.1;
				double diff = (max - min) / 2;
				double mean = (max + min) / 2;
				this.min = mean - diff * excessScaleCoefficient;
				this.max = mean + diff * excessScaleCoefficient;*/
				this.min = min;
				this.max = max;
			}
			public LinearScalarTransformScale(IScalarValues values)
			{
				if (values.Count == 0) throw new ArgumentException();
				max = values[0];
				min = values[0];
				for (int i = 1; i < values.Count; i++)
				{
					if (values[i] > max) max = values[i];
					if (values[i] < min) min = values[i];
				}
				if (max == min) max = min + 1;
			}
			public LinearScalarTransformScale(IScalarValues2D values)
			{
				if (values.CountX == 0) throw new ArgumentException();
				if (values.CountY == 0) throw new ArgumentException();
				max = values[0, 0];
				min = values[0, 0];
				for (int x = 0; x < values.CountX; x++)
					for (int y = 0; y < values.CountY; y++)
					{
						if (values[x, y] > max) max = values[x, y];
						if (values[x, y] < min) min = values[x, y];
					}
				if (max == min) max = min + 1;
			}
			public LinearScalarTransformScale(ArrayViewer.INonUniformScalarValues values)
			{
				if (values.Count == 0) throw new ArgumentNullException();
				min = values[0]; max = values[0];
				for (int i = 0; i < values.Count; i++)
				{
					if (values[i] > max) max = values[i];
					if (values[i] < min) min = values[i];
				}
				if (max == min) max = min + 1;
			}
			public void Delimit(IntervalDelimiter.Delimited callback, int count)
			{
				IntervalDelimiter.Delimit(min, max, count, callback);
			}
			public void Set(double min, double max)
			{
				this.min = min;
				this.max = max;
			}
		}
		public class LogScalarTransformScale : IScalarTransformScale
		{
			private readonly double min, logMax, logMin;
			private const double zeroTransformValue = 0.1;
			public double Transform(double value)
			{
				value = Math.Abs(value);
				if (value < min) return zeroTransformValue;
				else
				{
					double r = (Math.Log(value) - logMin) / (logMax - logMin);
					return zeroTransformValue + r * (1 - zeroTransformValue);
				}
			}
			private double InverseTransform(double value)
			{
				double r = (value - zeroTransformValue) / (1 - zeroTransformValue);
				r *= logMax - logMin;
				r += logMin;
				return Math.Exp(r);
			}
			public LogScalarTransformScale(IScalarValues values) // values must be non-negative
			{
				if (values.Count == 0) throw new Exception();
				double? max = null;
				double? min = null;
				for (int i = 0; i < values.Count; i++)
				{
					double value = Math.Abs(values[i]);
					if (value > 0)
					{
						if (max == null) max = value;
						else if (value > max.Value) max = value;
						if (min == null) min = value;
						else if (value < min.Value) min = value;
					}
				}
				if (max == null || min == null) throw new Exception("cannot initialize log transform scale");
				this.min = min.Value;
				this.logMin = Math.Log(min.Value);
				this.logMax = Math.Log(max.Value);
			}
			public void Delimit(IntervalDelimiter.Delimited callback, int count)
			{
				if (count <= 0) return;
				else if (count == 1)
				{
					callback(min);
					return;
				}
				double d = logMin;
				double step = (logMax - logMin) / (count - 1);
				for (int i = 0; i < count; i++, d += step)
					callback(InverseTransform(d));
			}
			public void Set(double min, double max)
			{
				throw new System.Exception("The method or operation is not implemented.");
			}
		}
		#endregion
		#region standard array values
		public class ScalarValues<T> : IScalarValues
		{
			private readonly T[] array;
			public ScalarValues(T[] array)
			{
				if (array == null) throw new ArgumentNullException("array");
				this.array = array;
			}
			public double this[int index]
			{
				get { return Convert.ToDouble(array[index]); }
			}
			public int Count
			{
				get { return array.Length; }
			}
		}
		public class ScalarValues2D<T> : IScalarValues2D
		{
			private readonly T[,] array;
			/// <param name="array">[y][x]</param>
			public ScalarValues2D(T[,] array)
			{
				if (array.GetLowerBound(0) != 0) throw new ArgumentException();
				if (array.GetLowerBound(1) != 0) throw new ArgumentException();
				this.array = array;
			}
			public double this[int x, int y]
			{
				get { return Convert.ToDouble(array[y, x]); }
			}
			public int CountX
			{
				get { return array.GetUpperBound(1) + 1; }
			}
			public int CountY
			{
				get { return array.GetUpperBound(0) + 1; }
			}
		}
		public class NonUniformScalarValue
		{
			public double value;
			public double position;
			public NonUniformScalarValue(double value, double position)
			{
				this.value = value;
				this.position = position;
			}
			public override string ToString()
			{
				return String.Format("{0} at {1}", value, position);
			}
		}
		public class NonUniformScalarValues : INonUniformScalarValues
		{
			private readonly NonUniformScalarValue[] values;
			public NonUniformScalarValues(NonUniformScalarValue[] values)
			{
				this.values = values;
			}
			public double GetPosition(int index)
			{
				return values[index].position;
			}
			public double this[int index]
			{
				get { return values[index].value; }
			}
			public int Count
			{
				get { return values.Length; }
			}
		}
		#endregion
		#region constants
		private const int cursorTimerInterval = 500;
		#endregion
		#region variables
		private ArrayViewerGenericZoomScale zoomScale = null; // != null if created; is set on create(); is the only one for arrayViewer lifetime
		public ArrayViewerGenericZoomScale ZoomScale
		{
			get { return zoomScale; }
		}
		private Timer cursorPaintTimer = new Timer();
		private Rectangle lastPaintArea;
		private List<Array> arrays = new List<Array>();
		private bool isCreated = false;
		private Color backgroundColor = Color.White;
		public bool autoPaintOnAttachArray = true;
		#endregion

		public ArrayViewer()
		{
			InitializeComponent();
			this.MouseWheel += new MouseEventHandler(ArrayViewer_MouseWheel);
			this.Disposed += new EventHandler(ArrayViewer_Disposed);
		}
		void ArrayViewer_Disposed(object sender, EventArgs e)
		{
			isCreated = false;
		}

		private void AssertIsCreated()
		{
			if (!isCreated) throw new InvalidOperationException("arrayviewer is not created");
		}
		public void Create()
		{
			Create(new ArrayViewerHorizontalZoomScale(ArrayViewerHorizontalZoomScale.MouseInputMode.syncMoveEntireArea));
		}
		public void Create(ArrayViewerGenericZoomScale zoomScale)
		{
			if (isCreated) throw new InvalidOperationException("arrayviewer is already created");
			if (zoomScale == null) throw new ArgumentNullException();
			this.zoomScale = zoomScale;
			zoomScale.AttachTo(this);
			cursorPaintTimer.Interval = cursorTimerInterval;
			cursorPaintTimer.Tick += new EventHandler(cursorPaintTimer_Tick);
			cursorPaintTimer.Start();
			isCreated = true;
		}
		public void AttachArray(Array array)
		{
			AssertIsCreated();
			if (arrays.Contains(array)) throw new Exception("array was already attached");
			arrays.Add(array);
			array.AttachTo(this);
			if (autoPaintOnAttachArray) Paint();
		}
		public void DetachArray(Array array)
		{
			//if (!arrays.Contains(array)) throw new Exception("array was not attached");
			arrays.Remove(array);
			array.DetachFrom(this);
			if (autoPaintOnAttachArray) Paint();
		}
		public void DetachAllArrays()
		{
			foreach (Array array in new List<Array>(arrays))
				DetachArray(array);
		}
		public new void Paint()
		{
			Graphics g = this.CreateGraphics();
			Paint(g);
			g.Dispose();
		}
		private new void Paint(Graphics g)
		{
			//Graphics g2 = new Graphics();
			bool needClearBackGround = false; // to clear by arrays
			if (arrays.Count != 0 && arrays[0].CanClearBackground)
				needClearBackGround = true;

			if (!needClearBackGround)
				g.Clear(backgroundColor);

			Rectangle area = this.ClientRectangle;
			area = zoomScale.GetFreeArea(area);
			foreach (Array array in arrays)
			{
				array.Paint(g, area, ref needClearBackGround);
				area = array.GetFreeArea(area);
			}
			zoomScale.Paint(g, area);
			zoomScale.PaintCursor(g, area);
			lastPaintArea = area;
		}

		#region GUI events handlers
		private void ArrayViewer_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (!isCreated) return;
				zoomScale.KeyDown(e);
				foreach (IUserInputProcessor uip in new List<Array>(arrays)) uip.KeyDown(e);
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		private void ArrayViewer_MouseCaptureChanged(object sender, EventArgs e)
		{
			try
			{
				if (!isCreated) return;
				zoomScale.MouseCaptureChanged(e);
				foreach (IUserInputProcessor uip in new List<Array>(arrays)) uip.MouseCaptureChanged(e);
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		private void ArrayViewer_MouseDown(object sender, MouseEventArgs e)
		{
			try
			{
				if (!isCreated) return;
				this.Focus();
				foreach (IUserInputProcessor uip in new List<Array>(arrays)) if (uip.MouseDown(e)) return;
				zoomScale.MouseDown(e);
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		private void ArrayViewer_MouseUp(object sender, MouseEventArgs e)
		{
			try
			{
				if (!isCreated) return;
				zoomScale.MouseUp(e);
				foreach (IUserInputProcessor uip in new List<Array>(arrays)) uip.MouseUp(e);
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		private void ArrayViewer_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				if (!isCreated) return;
				if (zoomScale.MouseMove(e)) return;
				foreach (IUserInputProcessor uip in new List<Array>(arrays)) if (uip.MouseMove(e)) return;
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		private void ArrayViewer_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			try
			{
				if (!isCreated) return;
				zoomScale.MouseDoubleClick(e);
				foreach (IUserInputProcessor uip in new List<Array>(arrays)) uip.MouseDoubleClick(e);
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		private void ArrayViewer_MouseLeave(object sender, EventArgs e)
		{
			try
			{
				if (!isCreated) return;
				zoomScale.MouseLeave(e);
				foreach (IUserInputProcessor uip in new List<Array>(arrays)) uip.MouseLeave(e);
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		private void ArrayViewer_MouseWheel(object sender, MouseEventArgs e)
		{
			ArrayViewer_KeyDown(sender, new KeyEventArgs(e.Delta > 0 ? Keys.Up : Keys.Down));
		}
		private void ArrayViewer_Paint(object sender, PaintEventArgs e)
		{
			try
			{
				if (!isCreated) return;
				Paint(e.Graphics);
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		private void ArrayViewer_Resize(object sender, EventArgs e)
		{
			try
			{
				if (!isCreated) return;
				if (zoomScale == null) return;
				Rectangle area = this.ClientRectangle;
				zoomScale.Resize(area);
				area = zoomScale.GetFreeArea(area);
				foreach (Array array in arrays)
				{
					array.Resize(area);
					area = array.GetFreeArea(area);
				}
				Paint();
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		#endregion
		private void cursorPaintTimer_Tick(object sender, EventArgs e)
		{
			try
			{
				if (!isCreated) return;
				Graphics g = this.CreateGraphics();
				zoomScale.PaintCursor(g, lastPaintArea);
				g.Dispose();
			}
			catch (Exception exc)
			{
				Debug.WriteLine(String.Format("exception in {0}.{1}: {2}", GetType().FullName, System.Reflection.MethodInfo.GetCurrentMethod().Name, exc));
			}
		}
		protected override bool IsInputKey(Keys keyData)
		{
			return true;
		}

		public ArrayViewerContinousArray1D AttachArray1d<T>(T[] a, double? transformScaleUpdateLimit)
		{
			ScalarValues<T> values = new ScalarValues<T>(a);
			LinearScalarTransformScale ts = new LinearScalarTransformScale(values);
			ArrayViewerContinousArray1D arr = new ArrayViewerContinousArray1D(ts, values, transformScaleUpdateLimit);
			AttachArray(arr);
			return arr;
		}
		public void AttachArray2d<T>(T[,] a)
		{
			ScalarValues2D<T> values = new ScalarValues2D<T>(a);
			LinearScalarTransformScale ts = new LinearScalarTransformScale(values);
			ArrayViewerArray2D arr = new ArrayViewerArray2D(ts, values);
			AttachArray(arr);
		}
	}

}
