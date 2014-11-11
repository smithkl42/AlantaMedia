using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace ASV
{
	public interface IArrayViewerRegions
	{
		int Count
		{
			get;
		}
		/// <summary>adds new region to list</summary>
		/// <returns>new region index; null if no region was added</returns>
		int? Add(double start, double end);
		void Get(int index, out double start, out double end);
		void Set(int index, double start, double end);
	}
	public class ArrayViewerRegionsDisplay: ArrayViewer.Array
	{
		private const int boundsWidth = 6;
		private readonly IArrayViewerRegions regions;
		private System.Drawing.Rectangle freeArea;
		private bool setResizingCursor = false;
		private int resizingRegionIndex;
		private bool resizingEndOrStart;
		private bool resizing = false;
		private Point cursorPositionOnCapture;
		private Timer timer;

		/// <summary>
		/// occurs when mouse hovers region
		/// no visualization now (todo)
		/// </summary>
		public event EventHandler HoveredRegionChanged = null;
		private int? hoveredRegionIndex = null;
		public int? HoveredRegionIndex
		{
			get
			{
				return hoveredRegionIndex;
			}
		}
		public ArrayViewerRegionsDisplay(IArrayViewerRegions regions)
		{
			this.regions = regions;
		}
		
		private bool GetRegionDisplayPosition(int index, out int startX, out int endX)
		{
			double start, end; regions.Get(index, out start, out end);
			start -= arrayViewer.ZoomScale.Left; end -= arrayViewer.ZoomScale.Left;
			start /= arrayViewer.ZoomScale.Width; end /= arrayViewer.ZoomScale.Width;
			if (start < 0) start = 0; else if (start > 1) start = 1;
			if (end < 0) end = 0; else if (end > 1) end = 1;
			if (start != end)
			{
				startX = Convert.ToInt32(freeArea.Left + start * freeArea.Width);
				endX = Convert.ToInt32(freeArea.Left + end * freeArea.Width);
				return true;
			}
			else
			{
				startX = endX = 0;
				return false;
			}
		}
		public override void Paint(System.Drawing.Graphics g, System.Drawing.Rectangle freeArea, ref bool needClearBackground)
		{
			this.freeArea = freeArea;
			for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
				PaintRegion(g, regionIndex);
		}
		private void PaintRegion(System.Drawing.Graphics g, int regionIndex)
		{
			int startX, endX;
			if (GetRegionDisplayPosition(regionIndex, out startX, out endX))
			{
				RECT rect; rect.left = startX; rect.right = endX;
				rect.top = freeArea.Top; rect.bottom = freeArea.Bottom;
				IntPtr hdc = g.GetHdc();
				InvertRect(hdc, ref rect);
				g.ReleaseHdc(hdc);
			}
		}
		private void OnMouseMoveInsideResizeArea()
		{
			if (!setResizingCursor)
			{
				arrayViewer.Cursor = Cursors.SizeWE;
				setResizingCursor = true;
			}
		}
		private void OnMouseMoveOutsideResizeArea()
		{
			if (setResizingCursor)
			{
				arrayViewer.Cursor = Cursors.Default;
				setResizingCursor = false;
			}
		}
		/// <summary>
		/// makes region be noticed by user
		/// </summary>
		public void HiliteRegion(int regionIndex)
		{
			MoveZoomToRegionIfInvisible(regionIndex);
			using (Graphics g = arrayViewer.CreateGraphics())
			{
				PaintRegion(g, regionIndex);
				System.Threading.Thread.Sleep(50);
				PaintRegion(g, regionIndex);
			}
		}
		private void MoveZoomToRegionIfInvisible(int regionIndex)
		{
			double start, end;
			regions.Get(regionIndex, out start, out end);
			if (start < ZoomScale.Left || end > ZoomScale.Right)
			{
				double w = Math.Abs(end - start) * 2;
				double c = (start + end) / 2;
				ZoomScale.SetZoom(Math.Max(c - w, 0), Math.Min(c + w, 1));
			}
		}
		public override bool MouseMove(System.Windows.Forms.MouseEventArgs e)
		{
			if (!resizing)
			{
				bool insideResizingArea = false;
				bool hoveredRegion = false;
				for (int i = 0; i < regions.Count; i++)
				{
					int startX, endX;
					if (GetRegionDisplayPosition(i, out startX, out endX))
					{
						if (Math.Abs(e.X - startX) < boundsWidth / 2 || Math.Abs(e.X - startX) < boundsWidth / 2)
						{
							resizingEndOrStart = false;
							resizingRegionIndex = i;
							insideResizingArea = true;
							break;
						}
						if (Math.Abs(e.X - endX) < boundsWidth / 2 || Math.Abs(e.X - endX) < boundsWidth / 2)
						{
							resizingEndOrStart = true;
							resizingRegionIndex = i;
							insideResizingArea = true;
							break;
						}
						if (e.X <= endX && e.X >= startX)
						{
							hoveredRegion = true;
							if (hoveredRegionIndex == null)
							{
								hoveredRegionIndex = i;
								HiliteRegion(i);								
								if (HoveredRegionChanged != null) HoveredRegionChanged(this, null);
							}
						}
					}
				}
				if (insideResizingArea) OnMouseMoveInsideResizeArea();
				else OnMouseMoveOutsideResizeArea();
				if (!hoveredRegion && hoveredRegionIndex != null)
				{
					hoveredRegionIndex = null;
					if (HoveredRegionChanged != null) HoveredRegionChanged(this, null);
				}
					
			}
			return false;
		}
		private void BeforeResizingProcedure()
		{
			resizing = true;
			arrayViewer.Capture = true;
			cursorPositionOnCapture = Cursor.Position;
			timer = new Timer();
			timer.Tick += new EventHandler(timer_Tick);
			timer.Interval = 50;
			timer.Start();
		}

		private void AfterResizingProcedure()
		{
			if (resizing)
			{
				resizing = false;
				timer.Stop();
				timer.Dispose();
				timer = null;

				double start, end; regions.Get(resizingRegionIndex, out start, out end);
				if (start > end)
				{
					double t = start;
					start = end;
					end = t;
					regions.Set(resizingRegionIndex, start, end);
				}
			}
		}
		public override bool MouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (setResizingCursor)
				{
					BeforeResizingProcedure();
					return true;
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				double createdRegionStart = ZoomScale.Left + (double)(e.X - freeArea.Left) / freeArea.Width * ZoomScale.Width;
				int? r = regions.Add(createdRegionStart, createdRegionStart);
				if (r == null) return false;
				OnMouseMoveInsideResizeArea();
				resizingEndOrStart = true;
				resizingRegionIndex = r.Value;
				BeforeResizingProcedure();
				return true;
			}

			return false;
		}
		public override void MouseUp(MouseEventArgs e)
		{
			if (resizing)
			{
				arrayViewer.Capture = false;
				AfterResizingProcedure();
			}
		}
		public override void MouseCaptureChanged(EventArgs e)
		{
			if (resizing) AfterResizingProcedure();
		}
		private void timer_Tick(object sender, EventArgs e)
		{
			if (resizing)
			{
				if (Cursor.Position != cursorPositionOnCapture)
				{
					Point difference = new Point(Cursor.Position.X - cursorPositionOnCapture.X, Cursor.Position.Y - cursorPositionOnCapture.Y);
					
					cursorPositionOnCapture = Cursor.Position;
					double change = (double)difference.X / freeArea.Width * ZoomScale.Width;

					double start, end;
					regions.Get(resizingRegionIndex, out start, out end);
					if (resizingEndOrStart) end += change; else start += change;
					regions.Set(resizingRegionIndex, start, end);
					arrayViewer.Paint();
				}
			}
		}


		private struct RECT
		{
			public int left, top, right, bottom;
		}
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern int InvertRect(IntPtr hdc, ref RECT rect);
	}
}
