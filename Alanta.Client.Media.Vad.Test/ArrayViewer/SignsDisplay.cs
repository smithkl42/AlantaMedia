using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ASV
{
	public interface IArrayViewerAbstractSigns1D
	{
		/// <summary>
		/// inclusive;
		/// min sign value must be zero
		/// </summary>
		int MaxSignValue
		{
			get;
		}
		int GetSignValue(int index);
		int ValuesCount
		{
			get;
		}
	}


	public class ArrayViewerSigns1D<T> : IArrayViewerAbstractSigns1D
	{
		private readonly T[] array;
		private readonly int maxSignValue;
		public ArrayViewerSigns1D(T[] array)
		{
			this.array = array;
			maxSignValue = 0;
			for (int i = 0; i < array.Length; i++)
				if (GetSignValue(i) > maxSignValue)
					maxSignValue = GetSignValue(i);
		}
		public int MaxSignValue
		{
			get
			{
				return maxSignValue;
			}
		}
		public int GetSignValue(int index)
		{
			return Convert.ToInt32(array[index]);
		}
		public int ValuesCount
		{
			get
			{
				return array.Length;
			}
		}
	}



	public class ArrayViewerSignsDisplay: ArrayViewer.Array
	{
		private readonly IArrayViewerAbstractSigns1D signs;
		private readonly List<Pen> pens;
		private readonly int height;

		public ArrayViewerSignsDisplay(IArrayViewerAbstractSigns1D signs, int height)
		{
			this.height = height;
			this.signs = signs;
			this.pens = new List<Pen>((int)signs.MaxSignValue + 1);
			for (int sign = 0; sign <= signs.MaxSignValue; sign++)
				pens.Add(new Pen(GetColor(sign)));
		}
		private static readonly Color[] colors = new Color[]
			{
				Color.FromArgb(255, 0, 0),
				Color.FromArgb(0, 255, 0),
				Color.FromArgb(0, 0, 255),
				Color.FromArgb(255, 255, 0),
				Color.FromArgb(0, 255, 255),
				Color.FromArgb(255, 0, 255),
				Color.FromArgb(128, 0, 0),
				Color.FromArgb(0, 128, 0),
				Color.FromArgb(0, 0, 128),
				Color.FromArgb(128, 128, 0),
				Color.FromArgb(0, 128, 128),
				Color.FromArgb(128, 0, 128),
				Color.FromArgb(128, 255, 255),
				Color.FromArgb(255, 128, 255),
				Color.FromArgb(255, 255, 128),
				Color.FromArgb(128, 128, 255),
				Color.FromArgb(255, 128, 128),
				Color.FromArgb(128, 255, 128),
				Color.FromArgb(0, 0, 0),
			};
		public static Color GetColor(int index)
		{		

			if (index < colors.Length) return colors[index];
			else
			{
				Random rnd = new Random();
				return Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
			}
		}

        public override void Paint(Graphics g, Rectangle freeArea, ref bool needClearBackground)
        {
			if (needClearBackground)
			{
				g.FillRectangle(new SolidBrush(arrayViewer.BackColor), 
					freeArea.Left, freeArea.Top + height, freeArea.Width, freeArea.Height - height);
				needClearBackground = false;
			}

			for (int x = freeArea.Left; x < freeArea.Right; x++)
			{
				double normalized_index = arrayViewer.ZoomScale.Left + (arrayViewer.ZoomScale.Right - arrayViewer.ZoomScale.Left) *
					(double)(x - freeArea.Left) / (freeArea.Right - freeArea.Left);
				int index = Convert.ToInt32(normalized_index * signs.ValuesCount);
				int sign = signs.GetSignValue(index);
				if (sign >= pens.Count) throw new Exception("invalid sign value received from ArrayViewerAbstractSigns1D");
				g.DrawLine(pens[sign], x, freeArea.Top, x, freeArea.Top + height);
			}
        }
    }
}
