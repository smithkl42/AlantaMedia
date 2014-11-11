using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace ASV
{
	public class ArrayViewerGenericZoomScale : ArrayViewer.IUserInputProcessor, ArrayViewer.ICursorPainter
	{
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
		public virtual bool MouseMove(MouseEventArgs e)
		{
			return false;
		}
		public virtual void MouseDoubleClick(MouseEventArgs e)
		{
		}
		public virtual void MouseLeave(EventArgs e)
		{
		}
		public virtual void PaintCursor(Graphics g, Rectangle paintArea)
		{
		}


		/// <summary>
		/// returned value [0;1] point to bounds of displayed region
		/// </summary>
		public virtual double Left
		{
			get
			{
				return 0;
			}
		}
		/// <summary>
		/// returned value [0;1] point to bounds of displayed region
		/// </summary>
		public virtual double Right
		{
			get
			{
				return 0;
			}
		}
		public double Width
		{
			get
			{
				return Right - Left;
			}
		}
		public virtual double Top
		{
			get
			{
				return 0;
			}
		}
		public virtual double Bottom
		{
			get
			{
				return 1;
			}
		}
		public virtual void SetZoom(double start, double end)
		{
		}

		protected ArrayViewer owner;
		public virtual void AttachTo(ArrayViewer owner) // create your navigation controls here
		{
			if (this.owner != null) throw new Exception();
			this.owner = owner;
		}
		public virtual void DetachFrom(ArrayViewer owner) // destroy your navigation controls here
		{
			if (this.owner != owner) throw new Exception();
			this.owner = null;
		}
		public virtual void Paint(Graphics g, Rectangle freeArea)
		{
		}
		public virtual void Resize(Rectangle freeArea)
		{
		}
		public virtual Rectangle GetFreeArea(Rectangle initialFreeArea)
		{
			return initialFreeArea;
		}
	}
	public class ArrayViewerHorizontalZoomScale : ArrayViewerGenericZoomScale
	{
		#region variables
		private double left, right;
		private int changeRegionWidth = 30;
		private int changeRegionHeight = 20;
		private Rectangle changeRegion;
		private Point? lastMousePoint;
		private SolidBrush changeRegionBrush;
		private bool changing = false;
		private Point cursorPositionBeforeChanging;
		private Timer changeTimer;
		private double cursor;
		private Rectangle lastPaintArea;
		public bool alignZoomToRightOfCenter = false;
		public enum MouseInputMode // flags
		{
			/// <summary>
			/// uses only limited rectangle at right bottom for input
			/// </summary>
			limitedRegion = 1,
			/// <summary>
			/// vertical mouse move causes zoom
			/// </summary>
			zoomAndMove = 2,
			syncMoveEntireArea = 0,			
		}
		private MouseInputMode mouseInputMode; // no change region if true
		#endregion
		public ArrayViewerHorizontalZoomScale(MouseInputMode mouseInputMode)
		{
			this.mouseInputMode = mouseInputMode;
			left = 0;
			right = 1;
			cursor = 0.5;
			changeRegionBrush = new SolidBrush(Color.Yellow);
		}
		public override void SetZoom(double left, double right)
		{
			if (left >= right) throw new ArgumentException("invalid zoom bounds");
			if (left < 0 || left > 1) throw new ArgumentException();
			if (right < 0 || right > 1) throw new ArgumentException();
			this.left = left;
			this.right = right;
			owner.Paint();
		}
		private void BeforeChangingProcedure()
		{
			owner.Capture = true;
			changing = true;
			cursorPositionBeforeChanging = Cursor.Position;
			changeTimer = new Timer();
			changeTimer.Tick += new EventHandler(changeTimer_Tick);
			changeTimer.Interval = 50;
			changeTimer.Start();
		}
		private void AfterChangingProcedure()
		{
			changing = false;
			changeTimer.Stop();
			changeTimer.Dispose();
		}
		#region change
		private void changeTimer_Tick(object sender, EventArgs e)
		{
			if (Cursor.Position != cursorPositionBeforeChanging)
			{
				Point difference = new Point(Cursor.Position.X - cursorPositionBeforeChanging.X, Cursor.Position.Y - cursorPositionBeforeChanging.Y);
								
				if ((mouseInputMode & MouseInputMode.zoomAndMove) != 0)
				{
					Cursor.Position = cursorPositionBeforeChanging;
					const double mouseZoomCoefficient = 0.02;
					const double mouseMoveCoefficient = 0.01;

					ProcessChange(
						(double)difference.X * mouseMoveCoefficient,
						(double)difference.Y * mouseZoomCoefficient
						);
				}
				else
				{
					cursorPositionBeforeChanging = Cursor.Position;
					double mouseMoveCoefficient = 1.0 / lastPaintArea.Width;
					ProcessChange(-(double)difference.X * mouseMoveCoefficient, 0);
				}				
			}
		}
		private void ProcessChange(double dx, double dy)
		{
			double zoom = Convert.ToDouble(-dy) + 1;
			if (zoom < 0.5) zoom = 0.5;
			double diff = right - left;
			diff /= zoom;
			if (diff > 1) diff = 1;

			if ((mouseInputMode & MouseInputMode.zoomAndMove) != 0 || dy != 0)
			{
				if (alignZoomToRightOfCenter)
				{
					left = right - diff;
				}
				else
				{
					right = cursor + diff / 2;
					left = cursor - diff / 2;
				}
			}

			double move = dx;
			if (move < -0.5) move = -0.5; if (move > 0.5) move = 0.5;
			move *= diff;
			
			left += move; if (left < 0) left = 0;
			right += move; if (right > 1) right = 1;

			// limit
			if (left < 0)
			{
				right -= left; if (right > 1) right = 1;
				left = 0;
			}
			if (right > 1)
			{
				left -= right - 1; if (left < 0) left = 0;
				right = 1;
			}

			// update cursor position
			cursor = (left + right) / 2; if (OnChangedCursor != null) OnChangedCursor(cursor);
			if (left > right) throw new Exception();
			owner.Paint();

			if (OnChangedZoom != null) OnChangedZoom();
		}
		#endregion
		#region GUI handlers
		public override bool MouseMove(MouseEventArgs e)
		{
			if (!changing)
			{
				if ((mouseInputMode & MouseInputMode.limitedRegion) != 0)
				{
					Point mousePoint = e.Location;
					bool changedRegionVisibility;
					if (lastMousePoint == null) changedRegionVisibility = true;
					else changedRegionVisibility = (changeRegion.Contains(mousePoint) ^ changeRegion.Contains(lastMousePoint.Value));
					lastMousePoint = mousePoint;
					if (changedRegionVisibility) owner.Paint();
					return changeRegion.Contains(mousePoint);						
				}
			}
			return false;
		}
		public override void MouseLeave(EventArgs e)
		{
			if (changing) return;
			if ((mouseInputMode & MouseInputMode.limitedRegion) != 0)
				if (lastMousePoint != null && changeRegion.Contains(lastMousePoint.Value))
				{
					lastMousePoint = null;
					owner.Paint();
				}
		}
		public override bool MouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if ((mouseInputMode & MouseInputMode.limitedRegion) == 0)
				{
					BeforeChangingProcedure();
					ChangeCursorPosition(e);
				}
				else if (changeRegion.Contains(e.Location)) BeforeChangingProcedure();
				else ChangeCursorPosition(e);
				return true;
			}
			return false;
		}
		private void ChangeCursorPosition(MouseEventArgs e)
		{
			CursorPosition = (left + (right - left) * Convert.ToDouble(e.X - lastPaintArea.Left) / lastPaintArea.Width);
		}
		public double CursorPosition
		{
			set
			{
				cursor = value;
				if (OnChangedCursor != null) OnChangedCursor(cursor);
				owner.Paint();
			}
			get
			{
				return cursor;
			}
		}
		public override void MouseCaptureChanged(EventArgs e)
		{
			if (changing) AfterChangingProcedure();
		}
		public override void MouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && changing)
			{
				owner.Capture = false;
				AfterChangingProcedure();
			}
		}
		public override void KeyDown(KeyEventArgs e)
		{
			// these coefficients are multiplied by mouse coefficients in ProcesssChange()
			double keyboardZoomCoefficient = 0.3;
			double keyboardMoveCoefficient = 0.05;

			switch (e.KeyCode)
			{
				case Keys.Up:
				case Keys.W:
					ProcessChange(0, -keyboardZoomCoefficient);
					break;
				case Keys.Down:
				case Keys.S:
					ProcessChange(0, keyboardZoomCoefficient);
					break;
				case Keys.Left:
				case Keys.A:
					ProcessChange(-keyboardMoveCoefficient, 0);
					break;
				case Keys.Right:
				case Keys.D:
					ProcessChange(keyboardMoveCoefficient, 0);
					break;
				case Keys.Z: ProcessChange((double)-3 / lastPaintArea.Width, 0); break;
				case Keys.X: ProcessChange((double)3 / lastPaintArea.Width, 0); break;
			}
		}
		public override void Paint(Graphics g, Rectangle freeArea)
		{
			if ((mouseInputMode & MouseInputMode.limitedRegion) != 0)
			{
				changeRegion = new Rectangle(freeArea.Right - changeRegionWidth, freeArea.Bottom - changeRegionHeight, changeRegionWidth, changeRegionHeight);
				if (lastMousePoint != null)
					if (changeRegion.Contains(lastMousePoint.Value))
						g.FillRectangle(changeRegionBrush, changeRegion);
			}
			lastPaintArea = freeArea;
		}
		#endregion

		public override double Left
		{
			get { return left; }
		}
		public override double Right
		{
			get { return right; }
		}		
		public override void PaintCursor(Graphics g, Rectangle paintArea)
		{
			double d = (cursor - left) / (right - left);
			int x = Convert.ToInt32(d * (paintArea.Right - paintArea.Left)) + paintArea.Left;
			//Rectangle rc = new Rectangle(x, paintArea.Top, 0, paintArea.Height);
			RECT rc = new RECT();
			rc.left = x;
			rc.right = x + 1;
			rc.bottom = paintArea.Bottom;
			rc.top = paintArea.Top;
			IntPtr hdc = g.GetHdc();
			InvertRect(hdc, ref rc);
			g.ReleaseHdc(hdc);

			//g.DrawRectangle(new Pen(Color.Black, 1), rc);
		}
		

		private struct RECT
		{
			public int left, top, right, bottom;
		}
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern int InvertRect(IntPtr hdc, ref RECT rect);

		public delegate void ChangedZoomHandler();
		public event ChangedZoomHandler OnChangedZoom = null;
		public delegate void ChangedCursorHandler(double cursorPosition);
		public event ChangedCursorHandler OnChangedCursor = null;
	}
}
